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
        Log.Info($"Resolving {Name}...");
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
    }

    public List<Formula> ResolveDependencies() {
        List<Formula> dependencies = new();
        _ResolveDependenciesRecursive(this, dependencies);
        return dependencies;
    }

    private void _ResolveDependenciesRecursive(Formula package, List<Formula> resolvedDependencies) {
        if (!resolvedDependencies.Contains(package) && ToplevelPackage != null && package.Name != ToplevelPackage.Name) {
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
        export LOCAL=""/usr/local""
        export HOME=""/home/{Iterkocze.LibiterkoczeOS.GetSystemUser()}""
        ");
        p.StandardInput.Write(procedure);
        p.StandardInput.Close();
        p.WaitForExit();
    }
}