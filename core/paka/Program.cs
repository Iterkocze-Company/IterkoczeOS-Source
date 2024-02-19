using System;
using System.CommandLine;

class Program {
    private static void CheckAndInit() {
        var direct_file = Globals.PAKA_DBDIR + "direct";
        if (!File.Exists(direct_file)) {
            Log.Info($"{Globals.PAKA_DBDIR + "direct"} doesn't exist. Creating");
            File.Create(direct_file).Close();
            Log.Info("Please restart paka");
            Environment.Exit(112);
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

        var downloadOption = new Option<string>(name: "--download", description: "Installs a package");
        downloadOption.AddAlias("-D");

        var cleanOption = new Option<string>(name: "--clean", description: "Safely uninstalls a package and its dependencies");
        cleanOption.AddAlias("-C");

        var uninstallOption = new Option<string>(name: "--uninstall", description: "Uninstalls only the package specified and leaves any dependencies");
        uninstallOption.AddAlias("-U");

        var testOption = new Option<bool>(name: "--test", description: "Runs unit tests");

        var unlockOption = new Option<bool>(name: "--force-unlock", description: "Deletes the lockfile (unsafe)");
        
        var debugOption = new Option<bool>(name: "--debug", description: "Enables debug logging");

        var listInstalledOption = new Option<bool>(name: "--list-installed", description: "Lists all installed packages and their size in MB");

        var packFormulaOption = new Option<bool>(name: "--pack-formula", description: "Packs formula files into .tar");

        var findFormulaOption = new Option<string>(name: "--search", description: "Finds a formula file for specified package");

        var validateFormulaOption = new Option<bool>(name: "--validate-formula-files", description: "Validates all formula files");

        var updatePakaOption = new Option<bool>(name: "--update-paka", description: "Updates paka");

        var formulaInfoOption = new Option<string>(name: "--info", description: "Displays information about a formula");

        rootCommand.Add(unlockOption);
        rootCommand.AddGlobalOption(debugOption); // this doesnt work

        utilCommand.Add(testOption);
        utilCommand.Add(packFormulaOption);
        utilCommand.Add(validateFormulaOption);
        utilCommand.Add(updatePakaOption);

        packageCommand.Add(downloadOption);
        packageCommand.Add(cleanOption);
        packageCommand.Add(uninstallOption);
        packageCommand.Add(listInstalledOption);
        packageCommand.Add(findFormulaOption);
        packageCommand.Add(formulaInfoOption);

        rootCommand.AddCommand(utilCommand);
        rootCommand.AddCommand(packageCommand);

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

        utilCommand.SetHandler((testOptionValue, packFormulaValue, validateFormulaValue, updatePakaValue) => {
            if (testOptionValue) {
                Test.Run();
            }
            if (packFormulaValue) {
                PackFormulaFiles();
            }
            if (validateFormulaValue) {
                Formula.ValidateAll();
            }
            if (updatePakaValue) {
                UpdatePaka();
            }
        }, testOption, packFormulaOption, validateFormulaOption, updatePakaOption);

        packageCommand.SetHandler((downloadOptionValue, cleanOptionValue, listInstalledValue, findFormulaValue, uninstallOptionValue, formulaInfoOptionValue) => {
            if (downloadOptionValue != null) {
                DoPackageDownload(downloadOptionValue);
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


        rootCommand.Invoke(args);
        if (args.Length == 0) {
            Console.WriteLine($"Iterkocze Paka {Globals.VERSION}");
            Console.WriteLine("--h or --help for list of switches");
        }
    }

    private static void UpdatePaka() {
        Console.WriteLine("Not implemnted");
    }

    private static void FindFormula(string name) {
        Log.Info($"Searching for {name}...");
        var files = Directory.GetFiles(Globals.PAKA_FORMULADIR);
        int hits = 0;
        foreach (var dir in files) {
            if (dir.Contains(name)) {
                var parts = dir.Split('/');
                Console.WriteLine(parts[parts.Length - 1]);
                hits++;
            }
        }
        Log.Info($"Found {hits} matches");
    }

    private static void FormulaInfo(string name) {
        Formula f = new(name);

        Console.WriteLine($"Formula information for: {name}");
        Console.WriteLine($"Formula type: {f.GetFormulaType()}");
        Console.WriteLine($"Description: {f.Properties["DESC"]}");
    }

    private static void PackFormulaFiles() {
        Log.Info("Packaging all formula files...");
        Directory.SetCurrentDirectory(Globals.PAKA_BASEDIR);
        var p = System.Diagnostics.Process.Start("tar", "cf formula.tar formula");
        p.WaitForExit();
        if (p.ExitCode != 0) {
            Log.Error("tar exited with non-zero exit code");
        } else {
            Log.Info("tar archive created");
        }
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

    private static void DoPackageDownload(string packageName) {
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

        if (depsToinstall.Count > 0) {
            Console.WriteLine("The following packages will be installed");
            Console.WriteLine("\t" + Formula.ToplevelPackage.Name);
            foreach (Formula dep in depsToinstall) {
                Console.WriteLine("\t" + dep.Name);
            }
            if (!AskToProceed("Do you want to continue?")) 
                Environment.Exit(0);

            foreach (Formula dep in depsToinstall) {
                dep.DoInstallProcedure();
            }
        } else {
            Log.Success("All dependencies satisfied");
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