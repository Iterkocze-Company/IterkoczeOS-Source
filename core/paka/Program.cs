using System;
using System.CommandLine;
using System.Diagnostics;

class Program {
    private static void CheckAndInit() {
        var requiredFiles = new string[] { 
            Globals.PAKA_DBDIR + "direct",
            Globals.DB_LOCAL_UPDATE_FILE 
        };

        foreach (var requiredFile in requiredFiles) {
            if (!File.Exists(requiredFile)) {
                Log.Info($"{requiredFile} doesn't exist. Creating");
                File.Create(requiredFile).Close();
            }
        }
    }
    private static void ProcessExit(object? sender, EventArgs e) {
        DeleteLockfile();
        Log.Info("Process exited");
    }
    static void Main(string[] args) {
        CheckAndInit();
        Console.CancelKeyPress += delegate {
            Log.Info("User requested an immediate stop. Cleaning up...");
            DeleteLockfile();
        };
        AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        var rootCommand = new RootCommand($"Iterkocze Paka {Globals.VERSION}");
        var utilCommand = new Command("util");
        var packageCommand = new Command("package");
        var updateCommand = new Command("update");

        var downloadOption = new Option<string>(name: "--download", description: "Installs a package");
        downloadOption.AddAlias("-D");

        var cleanOption = new Option<string>(name: "--clean", description: "Safely uninstalls a package and its dependencies");
        cleanOption.AddAlias("-C");

        var uninstallOption = new Option<string>(name: "--uninstall", description: "Uninstalls only the package specified and leaves any dependencies");
        uninstallOption.AddAlias("-U");

        var testOption = new Option<bool>(name: "--test", description: "Runs unit tests");

        var unlockOption = new Option<bool>(name: "--force-unlock", description: "Deletes the lockfile (unsafe)");
        
        var debugOption = new Option<bool>(name: "--debug", description: "Enables debug logging");

        var sandboxOption = new Option<bool>(name: "--sandbox", description: "Enables sandboxing (see docs)");

        var listInstalledOption = new Option<bool>(name: "--list-installed", description: "Lists all installed packages and their size in MB");

        var packFormulaOption = new Option<bool>(name: "--pack-formula", description: "Packs formula files into .tar");

        var findFormulaOption = new Option<string>(name: "--search", description: "Finds a formula file for specified package");

        var validateFormulaOption = new Option<bool>(name: "--validate-formula-files", description: "Validates all formula files");

        var formulaInfoOption = new Option<string>(name: "--info", description: "Displays information about a formula");

        var updateFormulaOption = new Option<bool>(name: "--formula", description: "Updates formula files");

        var updateSystemOption = new Option<bool>(name: "--system", description: "Installs all pending system updates");

        var updateCheckOption = new Option<bool>(name: "--check", description: "Checks if there are any pending updates. Remember to update formula files before checking for new updates");


        rootCommand.Add(unlockOption);
        rootCommand.AddGlobalOption(debugOption);
        rootCommand.AddGlobalOption(sandboxOption);
        // NOTE: This is an awful hack but I don't care. I blame the library
        if (args.Contains("--debug")) {
            Globals.IsDebugMode = true;
        }
        if (args.Contains("--sandbox")) {
            Globals.IsSandboxMode = true;
        }

        utilCommand.Add(testOption);
        utilCommand.Add(packFormulaOption);
        utilCommand.Add(validateFormulaOption);

        packageCommand.Add(downloadOption);
        packageCommand.Add(cleanOption);
        packageCommand.Add(uninstallOption);
        packageCommand.Add(listInstalledOption);
        packageCommand.Add(findFormulaOption);
        packageCommand.Add(formulaInfoOption);

        updateCommand.Add(updateFormulaOption);
        updateCommand.Add(updateSystemOption);
        updateCommand.Add(updateCheckOption);

        rootCommand.AddCommand(utilCommand);
        rootCommand.AddCommand(packageCommand);
        rootCommand.AddCommand(updateCommand);

        rootCommand.SetHandler((unlockOptionValue, debugOptionValue) => {
            if (File.Exists(Globals.PAKA_BASEDIR + ".lock")) {
                Log.Error("a lockfile is blocking paka execution\nIs another instance running?");
                Environment.Exit(1);
            }
            File.Create(Globals.PAKA_BASEDIR+".lock");

            if (debugOptionValue) {
                Globals.IsDebugMode = true;
            }
            if (unlockOptionValue) {
                DeleteLockfile();
            }
        }, unlockOption, debugOption);

        utilCommand.SetHandler((testOptionValue, packFormulaValue, validateFormulaValue) => {
            if (testOptionValue) {
                Test.Run();
            }
            if (packFormulaValue) {
                PackFormulaFiles();
            }
            if (validateFormulaValue) {
                Formula.ValidateAll();
            }
        }, testOption, packFormulaOption, validateFormulaOption);

        packageCommand.SetHandler((downloadOptionValue, cleanOptionValue, listInstalledValue, findFormulaValue, uninstallOptionValue, formulaInfoOptionValue) => {
            if (downloadOptionValue != null) {
                DoPackageInstall(downloadOptionValue);
            }
            if (cleanOptionValue != null) {
                DoPackageClean(cleanOptionValue);
            }
            if (listInstalledValue) {
                ListInstalledPackages();
            }
            if (findFormulaValue != null) {
                FindFormula(findFormulaValue);
            }
            if (uninstallOptionValue != null) {
                DoPackageUninstall(uninstallOptionValue);
            }
            if (formulaInfoOptionValue != null) {
                FormulaInfo(formulaInfoOptionValue);
            }
        }, downloadOption, cleanOption, listInstalledOption, findFormulaOption, uninstallOption, formulaInfoOption);

        updateCommand.SetHandler((updateFormulaOptionValue, updateSystemOptionValue, updateCheckOptionValue) => {
            if (updateFormulaOptionValue) {
                UpdateFormula();
            }
            if (updateSystemOptionValue) {
                UpdateSystem();
            }
            if (updateCheckOptionValue) {
                GetAllPendingUpdates();
            }
        }, updateFormulaOption, updateSystemOption, updateCheckOption);

        rootCommand.Invoke(args);
        if (args.Length == 0) {
            Console.WriteLine($"Iterkocze Paka {Globals.VERSION}");
            Console.WriteLine("--h or --help for list of switches");
        }
    }

    private static void UpdateFormula() {
        Log.Info("Updating formula files...");
        Directory.SetCurrentDirectory(Globals.PAKA_BASEDIR);
        Log.Debug("Checking for formula.old");
        if (Directory.Exists("formula.old")) {
            Directory.Delete("formula.old", true);
            Log.Debug("formula.old found and removed");
        }
        Log.Debug("Creating backup of current formulas");
        Directory.Move("formula", "formula.old");
        Log.Debug($"Renamed formula -> formula.old inside {Globals.PAKA_BASEDIR}");
        Log.Debug("Downloading new formula.tar");
        DownloadFile("https://github.com/Iterkocze-Company/IterkoczeOS-Packages-Main/raw/main/paka/formula.tar");
        RunProcessAndWait("tar", "xf formula.tar");
        Log.Debug("Removing formula.tar");
        File.Delete("formula.tar");
    }

    public static void UpdateSystem() {
        var pendingFormulas = GetAllPendingUpdates();

        if (pendingFormulas.Length > 0 && !AskToProceed("Would you like to download and install them now?")) {
            return;
        }

        foreach (var f in pendingFormulas) {
            f.DoInstallProcedure();
        }
    }

    private static Formula[] GetAllPendingUpdates() {
        Log.Info("Scanning for pending updates...");
        var installedIDs = LocalDatabase.GetInstalledUpdateIDs();
        var allUpdateIDs = new List<uint>();

        if (Globals.IsDebugMode) {
            Log.Debug($"installedIDs:");
            foreach (var installedID in installedIDs) {
                Log.Debug(installedID.ToString());
            }
        }

        foreach (var file in Directory.GetFiles(Globals.PAKA_FORMULADIR + "updates/")) {
            allUpdateIDs.Add(uint.Parse(new Formula("updates/" + Path.GetFileName(file)).Properties["UPDATE_ID"].Value));
        }

        var pendingIDs = allUpdateIDs.Except(installedIDs);
        var pendingFormulas = new List<Formula>();

        Log.Info($"Found {pendingIDs.ToArray().Length} pending updates");

        foreach (var f in LocalDatabase.GetAllFormulas("updates")) {
            if (pendingIDs.Any(id => id == uint.Parse(f.Properties["UPDATE_ID"].Value)) ) {
                pendingFormulas.Add(f);
                System.Console.WriteLine(f.Name);
            }
        }

        return pendingFormulas.ToArray();
    }

    private static void DownloadFile(string fileURL) {
        // https://github.com/Iterkocze-Company/IterkoczeOS-Packages-Main/raw/main/paka/formula.tar
        Process wget = new();
        wget.StartInfo.FileName = "wget";
        wget.StartInfo.RedirectStandardOutput = true;
        wget.StartInfo.RedirectStandardInput = true;
        wget.StartInfo.Arguments = fileURL + " -q --show-progress";
        wget.Start();
        wget.WaitForExit();
        if (wget.ExitCode != 0) {
            Log.Error($"wget failed! Do you have internet connection?");
            Environment.Exit(1);
        }
    }

    private static void RunProcessAndWait(string name, string args = "") {
        Process wget = new();
        wget.StartInfo.FileName = name;
        wget.StartInfo.RedirectStandardOutput = true;
        wget.StartInfo.RedirectStandardInput = true;
        wget.StartInfo.Arguments = args;
        wget.Start();
        wget.WaitForExit();
        if (wget.ExitCode != 0) {
            Log.Error($"{name} failed!");
            Environment.Exit(1);
        }

    }

    private static void FindFormula(string name) {
        Log.Info($"Searching for {name}...");
        var files = Directory.GetFiles(Globals.PAKA_FORMULADIR);
        List<string> names = new(); 
        int hits = 0;
    
        foreach (var dir in files) {
            if (dir.Contains(name)) {
                names.Add(Formula.FormulaFileToName(dir));
                hits++;
            }
        }

        foreach (var n in names) {
            var f = new Formula(n);
            char sep = '\\';
            Console.WriteLine($"\t{n} => [{f.GetFormulaType()}] {f.Properties["DESC"].Value}");
            foreach(var dep in f.Dependencies) {
                Console.WriteLine($"\t\t{sep}-> {dep.Name} {(dep.IsInstalled ? "[INSTALLED]" : "")}");
                sep = '|';
            }
        }
        Log.Info($"Found {hits} matches");
    }

    private static void FormulaInfo(string name) {
        Formula f = new(name);

        Console.WriteLine($"Formula information for: {name}");
        Console.WriteLine($"Formula type: {f.GetFormulaType()}");
        Console.WriteLine($"Description: {f.Properties["DESC"].Value}");
    }

    private static void PackFormulaFiles() {
        if (!Directory.Exists(Globals.PAKA_LOCAL_REPO_MAIN)) {
            Log.Error($"Directory not found: {Globals.PAKA_LOCAL_REPO_MAIN}");
            return;
        }

        Log.Info("Packaging all formula files...");
        Directory.SetCurrentDirectory(Globals.PAKA_BASEDIR);
        RunProcessAndWait("tar", "cf formula.tar formula");
        File.Move("formula.tar", Globals.PAKA_LOCAL_REPO_MAIN + "paka/formula.tar");
        Log.Info($"tar archive created and moved to {Globals.PAKA_LOCAL_REPO_MAIN + "paka/formula.tar"}");
    }

    private static void ListInstalledPackages() {
        foreach(var package in LocalDatabase.GetAllInstalledPackages()) {
            Console.Write(package + " => ");
            Console.Write(LocalDatabase.ReadDBFile(package, "size.db")[0] + " MB\n");
        }
    }

    private static void DeleteLockfile() {
        // If the lockfile doesn't exist, then don't panik. Doesn't matter
        try {
            Log.Debug("Trying to delete lockfile...");
            File.Delete(Globals.PAKA_BASEDIR + ".lock");
        } catch(Exception) {
            Log.Debug("Lockfile can't be deleted. Ignoring");
        }
    }

    private static void DoPackageUninstall(string packageName) {
        if (!Formula.Exists(packageName)) {
            Log.Error($"{packageName}.formula can't be found");
            Environment.Exit(1);
        }
        if (!LocalDatabase.IsInstalled(packageName)) {
            Log.Error($"{packageName} is not installed");
            Environment.Exit(1);
        }

        if (!AskToProceed($"To uninstall a package and it's dependencies use -C\nThis action will uninstall {packageName}, but leave any dependencies. Do you want to preceed?")) {
            Environment.Exit(0);
        }

        Formula packageToUninstall = new(packageName);
        packageToUninstall.DoRemoveProcedure();
    }

    private static void DoPackageInstall(string packageName) {
        if (!Formula.Exists(packageName)) {
            Log.Error($"{packageName}.formula can't be found");
            Environment.Exit(1);
        }
        if (LocalDatabase.IsInstalled(packageName)) {
            Console.WriteLine($"{packageName} is already installed. If you want to update or reinstall this package, uninstall it first.\nIt's best to do a full removal of the package using -C");
            Environment.Exit(0);
        }
        Formula.ToplevelPackage = new(packageName);
        Log.Info("Calculating dependencies...");
        List<Formula> depsToinstall = Formula.ToplevelPackage.ResolveDependencies().Where(dep => !LocalDatabase.IsInstalled(dep.Name)).ToList();
        depsToinstall.Reverse();
        
        if (Globals.IsSandboxMode) {
            Log.Warning("[ SANDBOX MODE ENABLED ]");
        }

        Console.WriteLine("The following packages will be installed (in this order)");
        foreach (Formula dep in depsToinstall) {
            Console.WriteLine("\t" + dep.Name);
        }
        Console.WriteLine("\t" + Formula.ToplevelPackage.Name);
        if (!AskToProceed("Do you want to continue?")) 
            Environment.Exit(0);

        foreach (Formula dep in depsToinstall) {
            dep.DoInstallProcedure();
        }
         
        Log.Info($"installing {Formula.ToplevelPackage.Name}...");
        Formula.ToplevelPackage.DoInstallProcedure();
    }

    private static void DoPackageClean(string packageName) {
        if (!Formula.Exists(packageName)) {
            Log.Error($"{packageName}.formula can't be found");
            Environment.Exit(1);
        }
        if (!LocalDatabase.IsInstalled(packageName)) {
            Log.Error($"{packageName} is not installed");
            Environment.Exit(0);
        }

        Formula.ToplevelPackage = new(packageName);
        Log.Info("Calculating dependencies...");
        List<Formula> depsToRemove = Formula.ToplevelPackage.ResolveDependencies();
        List<Formula> directDeps = new();

        foreach (var dep in depsToRemove) {
            if (dep.IsDirectlyInstalled) {
                Log.Info($"Dependency '{dep.Name}' won't be uninstalled, because it was directly installed");
                directDeps.Add(dep);
            }
        }

        depsToRemove = depsToRemove.Except(directDeps).ToList();

        var rdeps = Formula.ToplevelPackage.GetReverseDependencies();
        var preventingFormulas = rdeps.Where(rdep => rdep.IsInstalled).ToArray();

        foreach (var p in preventingFormulas) {
            Log.Info($"{packageName} can't be uninstalled because {p.Name} depends on it");
        }
        if (preventingFormulas.Length > 0) {
            return;
        }

        // foreach (var dep in Formula.ToplevelPackage.Dependencies) {
        //     var rDeps = dep.GetReverseDependencies();
        //     var preventingFormulas = rDeps.Where(rdep => rdep.IsInstalled && rdep.Name != Formula.ToplevelPackage.Name).ToArray();

        //     if (preventingFormulas.Length > 0) {
        //         Log.Info($"Dependency '{dep.Name}' won't be uninstalled, because the following installed packages depend on it:");
        //         foreach (var p in preventingFormulas) {
        //             Console.WriteLine("\t"+p.Name);
        //         }
        //         depsToRemove.Remove(dep);
        //     }
        // }

        Console.WriteLine("The following packages will be removed");
        Console.WriteLine("\t" + packageName);
        foreach (Formula dep in depsToRemove) {
            Console.WriteLine("\t" + dep.Name);
        }
        if (!AskToProceed("Do you want to continue?")) 
            Environment.Exit(0);

        foreach (Formula dep in depsToRemove) {
            dep.DoRemoveProcedure();
        }

        Log.Info($"Removing {Formula.ToplevelPackage.Name}...");
        Formula.ToplevelPackage.DoRemoveProcedure();
    }

    public static bool AskToProceed(string msg) {
        Console.WriteLine(msg);
        Console.Write("y/n? ");
        bool choice = false;
        bool ret_ans = false;

        while (!choice) {
            string? ans = Console.ReadLine();
            if (ans != "y" && ans != "n") {
                Console.WriteLine("Be clear. 'y' or 'n'");
            } else {
                if (ans == "y")
                    ret_ans = true;
                choice = true;
            }
        }

        return ret_ans;
    }
}