using System.Text;
using System.Linq;
using System.Diagnostics;

class Formula {
    public static Formula? ToplevelPackage = null;
    public string Name {get; set;}
    public string InstallProcedure {get; private set;}
    public string RemoveProcedure {get; private set;}
    public bool IsDirectlyInstalled {get; private set;}
    public bool IsInstalled {get; private set;}

    public Dictionary<string, string> Properties {get; set;} = new Dictionary<string, string>() {
        {"DESC", string.Empty},
        {"SRC_URL", string.Empty},
    };
    public Dictionary<string, bool> Switches {get; set;} = new Dictionary<string, bool>() {
        {"HOLD", false},
        {"AUTO_REMOVE", false},
        {"NO_DOWNLOAD", false},
    };

    public List<Formula> Dependencies {get; set;} = new();

    public Formula(string formulaFileName) {
        Name = formulaFileName;
        string formulaFilePath = Globals.PAKA_FORMULADIR + formulaFileName + ".formula";
        
        // Procedures
        StringBuilder installProcedure = new();
        StringBuilder removeProcedure = new();
        bool insideProcedure = false;

        foreach (string line in File.ReadLines(formulaFilePath)) {
            if (line.StartsWith("BEGIN INSTALL")) {
                insideProcedure = true;
                continue;
            }

            if (insideProcedure) {
                if (line.StartsWith("END INSTALL")) {
                    insideProcedure = false;
                    break;
                }

                installProcedure.AppendLine(line);
            }
        }

        foreach (string line in File.ReadLines(formulaFilePath)) {
            if (line.StartsWith("BEGIN REMOVE")) {
                insideProcedure = true;
                continue;
            }

            if (insideProcedure) {
                if (line.StartsWith("END REMOVE")) {
                    insideProcedure = false;
                    break;
                }

                removeProcedure.AppendLine(line);
            }
        }
        InstallProcedure = installProcedure.ToString();
        RemoveProcedure = removeProcedure.ToString();

        // Properties
        foreach (var line in File.ReadLines(formulaFilePath)) {
            if (line.StartsWith("DESC"))
                Properties["DESC"] = line.Replace("\"", "").Replace("DESC=", "");
            if (line.StartsWith("SRC_URL"))
                Properties["SRC_URL"] = line.Replace("\"", "").Replace("SRC_URL=", "");
        }

        // Switches
        foreach (var key in Switches.Keys) {
            foreach (string line in File.ReadLines(formulaFilePath)) {
                if (line.StartsWith(key)) {
                    Switches[key] = true;
                }
            }
        }

        // Dependencies
        // NOTE: I think it might cause a crash due to circular dependencies
        foreach (string line in File.ReadLines(formulaFilePath)) {
            if (line.StartsWith("DEPENDSON")) {
                var v = line.Replace("\"", "").Replace("DEPENDSON=", "").Trim().Split(',');
                foreach (var val in v) {
                    if (!string.IsNullOrEmpty(val))
                        Dependencies.Add(new Formula(val.Trim()));
                }
            }
        }

        // IsDirectlyInstalled
        IsDirectlyInstalled = LocalDatabase.IsDirectlyInstalled(Name);

        //IsInstalled
        IsInstalled = LocalDatabase.IsInstalled(Name);
    }

    public static bool Exists(string formulaFileName) {
        if (!File.Exists(Globals.PAKA_FORMULADIR + formulaFileName + ".formula")) {
            return false;
        }
        return true;
    }

    public void DoInstallProcedure() {
        Log.Info($"Resolving install procedure for {Name}...");
        Thread.Sleep(1000);

        Directory.CreateDirectory($"/tmp/{Name}");
        Directory.SetCurrentDirectory($"/tmp/{Name}");

        if (!Switches["NO_DOWNLOAD"]) {
            Log.Info("Downloading source...");

            Process wget = new();
            wget.StartInfo.FileName = "wget";
            wget.StartInfo.RedirectStandardOutput = true;
            wget.StartInfo.RedirectStandardInput = true;
            wget.StartInfo.Arguments = Properties["SRC_URL"] + " -q --show-progress";
            wget.Start();
            wget.WaitForExit();
            if (wget.ExitCode != 0) {
                Log.Error($"wget failed! Is SRC_URL for {Name}.formula valid?");
                Environment.Exit(1);
            }
        }

        List<string> AllFilesBefore = Directory.GetFiles("/usr/local", "*.*", SearchOption.AllDirectories).ToList();
        AllFilesBefore.AddRange(Directory.GetFiles("/programs", "*.*", SearchOption.AllDirectories).ToList());


        Log.Info($"Executing BEGIN INSTALL for {Name}...");

        _RunProcess(InstallProcedure);

        List<string> AllFilesAfter = Directory.GetFiles("/usr/local", "*.*", SearchOption.AllDirectories).ToList();
        AllFilesAfter.AddRange(Directory.GetFiles("/programs", "*.*", SearchOption.AllDirectories).ToList());

        List<string> AllFilesInstalled = AllFilesBefore.Except(AllFilesAfter).Concat(AllFilesAfter.Except(AllFilesBefore)).ToList();

        double installedSizeMB = 0;
        foreach (string file in AllFilesInstalled) {
            FileInfo fi = new(file);
            installedSizeMB += fi.Length / (1024 * 1024); 
        }

        Log.Debug($"Registering {Name} in the database...");
        LocalDatabase.RegisterPackage(Name);
        if (ToplevelPackage != null && ToplevelPackage.Name == Name) {
            LocalDatabase.MarkAsDirectlyInstalled(Name);
        }
        
        LocalDatabase.WriteDBFile(Name, "remove.db", AllFilesInstalled, false);
        LocalDatabase.WriteDBFile(Name, "size.db", new string[]{installedSizeMB.ToString()}, false);

        if (!Switches["HOLD"]) {
            Log.Info("Deleting temporary data...");
            Directory.Delete($"/tmp/{Name}", true);
        }
    }

    public void DoRemoveProcedure() {
        Log.Info($"Resolving remove procedure for {Name}...");
        Thread.Sleep(1000);

        if (Switches["AUTO_REMOVE"]) {
            Log.Info($"Executing AUTO_REMOVE for {Name}...");
            var filesToRemove = LocalDatabase.ReadDBFile(Name, "remove.db");
            foreach (var file in filesToRemove) {
                Console.WriteLine($"Deleting {file}...");
                File.Delete(file);
            }
        } else {
            _RunProcess(RemoveProcedure);
        }
        LocalDatabase.UnregisterPackage(Name);
        if (ToplevelPackage != null && ToplevelPackage.Name == Name) {
            LocalDatabase.UnmarkAsDirectlyInstalled(Name);
        }
    }

    public List<Formula> ResolveDependencies() {
        List<Formula> dependencies = new();
        _ResolveDependenciesRecursive(this, dependencies);
        dependencies.RemoveAt(0);
        return dependencies;
    }

    public static void ValidateAll() {
        string[] allFormulaFiles = Directory.GetFiles(Globals.PAKA_FORMULADIR);
        List<string> names = new();
        foreach (var file in allFormulaFiles) {
            if (file.EndsWith("example.formula"))
                continue;
            var parts = file.Split('/');
            names.Add(parts[parts.Length - 1]);
        }
        List<Formula> formulas = new();
        foreach (var name in names) {
            formulas.Add(new(name.Replace(".formula", "")));
        }
        foreach (var formula in formulas) {
            formula.Validate();
        }
    }

    public void Validate() {
        if (string.IsNullOrEmpty(InstallProcedure)) {
            Log.Error($"{Name} does not contain the INSTALL procedure");
        }
        if (Switches["HOLD"]) {
            Log.Warning($"{Name} has HOLD enabled");
        }
    }

    private void _ResolveDependenciesRecursive(Formula package, List<Formula> resolvedDependencies) {
        if (!resolvedDependencies.Contains(package)) {
            resolvedDependencies.Add(package);
            foreach (Formula dep in package.Dependencies) {
                _ResolveDependenciesRecursive(dep, resolvedDependencies);
            }
        }
    }

    private void _RunProcess(string procedure) {
        Process p = new();
        p.StartInfo.FileName = "bash";
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardInput = true;
        p.OutputDataReceived += (sender, e) => {
            Console.WriteLine(e.Data);
        };
        p.Start();
        p.BeginOutputReadLine();
        p.StandardInput.WriteLine(@$"
        export CORES={Environment.ProcessorCount} 
        export LOCAL=""/usr/local/""
        export USER_HOME=""/home/{Iterkocze.LibiterkoczeOS.GetSystemUser()}""
        ");
        p.StandardInput.Write(procedure);
        p.StandardInput.Close();
        p.WaitForExit();
    }
}