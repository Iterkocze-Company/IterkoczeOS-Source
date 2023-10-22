using System;
using System.CommandLine;
using System.Runtime.CompilerServices;

class Program {
    private static void ProcessExit(object? sender, EventArgs e) {
        DeleteLockfile();
        Log.Info("Process exited");
    }
    static void Main(string[] args) {
        Console.CancelKeyPress += delegate {
            Log.Info("User requested an immediate stop. Cleaning up...");
            DeleteLockfile();
        };
        AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        var rootCommand = new RootCommand($"Iterkocze Paka {Globals.VERSION}");

        var downloadOption = new Option<string>(name: "--download", description: "Installs a package");
        downloadOption.AddAlias("-D");

        var cleanOption = new Option<string>(name: "--clean", description: "Safely uninstalls a package and its dependencies");
        cleanOption.AddAlias("-C");

        var testOption = new Option<bool>(name: "--test", description: "Runs unit tests");

        var unlockOption = new Option<bool>(name: "--force-unlock", description: "Deletes the lockfile (unsafe)");
        
        var debugOption = new Option<bool>(name: "--debug", description: "Enables debug logging");

        var listInstalledOption = new Option<bool>(name: "--list-installed", description: "Lists all installed packages and their size in MB");

        var packFormulaOption = new Option<bool>(name: "--pack-formula", description: "Packs formula files into .tar");

        rootCommand.Add(downloadOption);
        rootCommand.Add(testOption);
        rootCommand.Add(unlockOption);
        rootCommand.Add(debugOption);
        rootCommand.Add(cleanOption);
        rootCommand.Add(listInstalledOption);
        rootCommand.Add(packFormulaOption);

        /*if (Environment.UserName != "root") {
            Log.Error("Run as root");
            Environment.Exit(1);
        }*/

        rootCommand.SetHandler((downloadOptionValue, testOptionValue,
        unlockOptionValue, debugOptionValue, cleanOptionValue, listInstalledValue, packFormulaValue) => {
            if (File.Exists(Globals.PAKA_BASEDIR + ".lock")) {
                Log.Error("a lockfile is blocking paka execution\nIs another instance running?");
                Environment.Exit(1);
            }
            File.Create(Globals.PAKA_BASEDIR+".lock");

            if (debugOptionValue) {
                Globals.IsDebugMode = true;
            }

            if (downloadOptionValue != null) {
                DoPackageDownload(downloadOptionValue);
            }

            if (cleanOptionValue != null) {
                DoPackageClean(cleanOptionValue);
            }

            if (testOptionValue) {
                Test.Run();
            }
            if (listInstalledValue) {
                ListInstalledPackages();
            }
            if (packFormulaValue) {
                PackFormulaFiles();
            }
            if (unlockOptionValue) {
                DeleteLockfile();
            }
        }, downloadOption, testOption, unlockOption, debugOption, cleanOption, listInstalledOption, packFormulaOption);

        rootCommand.Invoke(args);
        if (args.Length == 0) {
            Console.WriteLine($"Iterkocze Paka {Globals.VERSION}");
            Console.WriteLine("--h or --help for list of switches");
        }
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

    private static void DoPackageDownload(string packageName) {
        if (!Formula.Exists(packageName)) {
            Log.Error($"{packageName}.formula can't be found");
            Environment.Exit(1);
        }
        if (LocalDatabase.IsInstalled(packageName)) {
            Log.Error($"{packageName} is already installd. To reinstall, please uninstall it first");
            Environment.Exit(0);
        }
        Formula.ToplevelPackage = new(packageName);
        Log.Info("Calculating dependencies...");
        List<Formula> depsToinstall = Formula.ToplevelPackage.ResolveDependencies();

        if (depsToinstall.Count > 0) {
            Console.WriteLine("The following packages will be installed as dependencies");
            foreach (Formula dep in depsToinstall) {
                Console.WriteLine("\t" + dep.Name);
            }
            if (!AskToProceed("Do you want to continue?")) 
                Environment.Exit(0);

            foreach (Formula dep in depsToinstall) {
                dep.DoInstallProcedure();
            }
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