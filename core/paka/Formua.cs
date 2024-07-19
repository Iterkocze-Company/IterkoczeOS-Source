using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Reflection;

public class Formula {
    public static Formula? ToplevelPackage = null;
    public string Name {get; set;}
    //public string InstallProcedure {get; private set;}
    //public string RemoveProcedure {get; private set;}
    public bool IsDirectlyInstalled {get; private set;}
    public bool IsInstalled {get; private set;}

    public bool IsUpdate {get; private set;}

    public readonly Dictionary<string, Property> Properties = new() {
        { "DESC", new() {IsRequired = false} },
        { "SRC_URL", new() {IsRequired = true} },
        { "DEPENDSON", new() {IsRequired = false} },
        { "UPDATE_ID", new() {IsRequired = false} },
    };
    // NOTE: Maybe Switches and Procedures should have enums as keys instead of strings
    public Dictionary<string, bool> Switches {get; set;} = new Dictionary<string, bool>() {
        {"HOLD", false},
        {"AUTO_REMOVE", false},
        {"NO_DOWNLOAD", false},
        {"BIN", false},
        {"SRC", false},
    };

    public Dictionary<string, Procedure> Procedures = new() {
        { "INFO", new() {Name = "INFO", IsRequired = true} },
        { "INSTALL", new() {Name = "INSTALL", IsRequired = true} },
        { "REMOVE", new() {Name = "REMOVE", IsRequired = false} },
    };

    public List<Formula> Dependencies {get; set;} = new();

    public Formula(string formulaRelativePath) {
        Name = Path.GetFileName(formulaRelativePath);
        string formulaFilePath = Globals.PAKA_FORMULADIR + formulaRelativePath;

        if (!formulaFilePath.EndsWith(".formula")) {
            formulaFilePath += ".formula";
        }

        if (!File.Exists(formulaFilePath)) {
            throw new FileNotFoundException($"This formula file can't be found: {formulaFilePath}");
        }
        
        var fileContent = File.ReadLines(formulaFilePath);

        _ReadProcedure(Procedures["INFO"], fileContent, formulaRelativePath);
        _ReadProcedure(Procedures["INSTALL"], fileContent, formulaRelativePath);
        _ReadProcedure(Procedures["REMOVE"], fileContent, formulaRelativePath);

        _EvalInfoProcedure(Procedures["INFO"].Value);

        IsDirectlyInstalled = LocalDatabase.IsDirectlyInstalled(Name);
        IsInstalled = LocalDatabase.IsInstalled(Name);
        IsUpdate = Properties["UPDATE_ID"].Value == "" ? false : true;

        // NOTE: Shoudn't this file be closed by now?
    }

    public static Formula[] FormulasFromNames(string[] names) {
        List<Formula> ret = new();

        foreach (var name in names) {
            ret.Add(new Formula(name));
        }
        return ret.ToArray();
    }

    public void DoInstallProcedure() {
        Log.Info($"Resolving install procedure for {Name}...");
        Thread.Sleep(1000);

        Directory.CreateDirectory($"/tmp/{Name}");
        Directory.SetCurrentDirectory($"/tmp/{Name}");
        if (Globals.IsSandboxMode) {
            Directory.CreateDirectory($"/tmp/paka-sandbox-{Name}");
        }

        if (!Switches["NO_DOWNLOAD"]) {
            Log.Info("Downloading source...");

            Process wget = new();
            wget.StartInfo.FileName = "wget";
            wget.StartInfo.RedirectStandardOutput = true;
            wget.StartInfo.RedirectStandardInput = true;
            wget.StartInfo.Arguments = Properties["SRC_URL"].Value + " -q --show-progress";
            wget.Start();
            wget.WaitForExit();
            if (wget.ExitCode != 0) {
                Log.Error($"wget failed! Is SRC_URL for {Name}.formula valid?");
                Environment.Exit(1);
            }
        }

        // NOTE: This should not count up everything in the /usr/local dir
        // Maybe use chroot?
        List<string> AllFilesBefore = Directory.GetFiles("/usr/local", "*.*", SearchOption.AllDirectories).ToList();
        AllFilesBefore.AddRange(Directory.GetFiles("/programs/local", "*.*", SearchOption.AllDirectories).ToList());

        Log.Info($"Executing BEGIN INSTALL for {Name}...");

        _RunProcess(string.Join(" && ", Procedures["INSTALL"].Value)[0..^3]);

        List<string> AllFilesAfter = Directory.GetFiles("/usr/local", "*.*", SearchOption.AllDirectories).ToList();
        AllFilesAfter.AddRange(Directory.GetFiles("/programs/local", "*.*", SearchOption.AllDirectories).ToList());

        List<string> AllFilesInstalled = AllFilesBefore.Except(AllFilesAfter).Concat(AllFilesAfter.Except(AllFilesBefore)).ToList();

        double installedSizeMB = 0;
        foreach (string file in AllFilesInstalled) {
            FileInfo fi = new(file);
            installedSizeMB += fi.Length / (1024 * 1024); 
        }


        if (!IsUpdate) {
            Log.Debug($"Registering {Name} in the database...");
            LocalDatabase.RegisterPackage(Name);      
            LocalDatabase.WriteDBFile(Name, "remove.db", AllFilesInstalled, false);
            LocalDatabase.WriteDBFile(Name, "size.db", new string[]{installedSizeMB.ToString()}, false);
        } else {
            Log.Debug($"Registering ID for {Name} as installed...");
            LocalDatabase.MarkUpdateIDAsInstalled(uint.Parse(Properties["UPDATE_ID"].Value));
        }

        // Toplevel package is never set for updates
        if (ToplevelPackage != null && ToplevelPackage.Name == Name) {
            LocalDatabase.MarkAsDirectlyInstalled(Name);
        }
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
            Log.Info($"Executing BEGIN REMOVE for {Name}...");
            _RunProcess(string.Join(" && ", Procedures["REMOVE"].Value)[0..^3]);
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

    public Formula[] GetReverseDependencies() {
        List<Formula> ret = new();

        foreach (var formula in LocalDatabase.GetAllFormulas()) {
            if (formula.Dependencies.Any(dep => dep.Name == this.Name)) {
                ret.Add(formula);
            }
        }
        
        return ret.ToArray();
    }

    public static void ValidateAll() {
        var allFormulas = LocalDatabase.GetAllFormulas();

        foreach (var formula in allFormulas) {
            formula.Validate();
        }
    }

    public string GetFormulaType() {
        if (Switches["BIN"])
            return "Binary";
        else if (Switches["SRC"])
            return "Source";
        else
            return "Unknown";
    }

    public uint Validate() {
        uint errors = 0;

        Log.Info($"validating {Name}...");

        foreach (var prop in Properties) {
            if (prop.Value.Value == "" && prop.Value.IsRequired) {
                Console.Write("\t");
                Log.Error($"{prop.Key} is required, but not set");
                errors++;
            }
        }
        foreach (var proc in Procedures) {
            if (proc.Value.State == Procedure.STATE.INVALID) {
                Console.Write("\t");
                Log.Error($"{proc.Value.Name} procedure is invalid");
                errors++;
            }
            if (proc.Value.State == Procedure.STATE.UNITITIALISED) {
                Console.Write("\t");
                Log.Error($"{proc.Value.Name} procedure is UNITITIALISED");
                errors++;
            }
        }

        if (Switches["HOLD"]) {
            Console.Write("\t");
            Log.Warning("has HOLD enabled");
        }
        if (Switches["SRC"] && Switches["BIN"]) {
            Console.Write("\t");
            Log.Error("is both SRC and BIN");
            errors++;
        }

        if (Properties["UPDATE_ID"].Value == "" && Name.StartsWith("update-")) {
            Console.Write("\t");
            Log.Error($"{Name} is considered to be an update because it starts with 'update-', but its UPDATE_ID is not set");
            errors++;
        }

        return errors;
    }

    /// <summary>
    /// <para> n => /some/directory/dummy.formula </para>
    /// n = dummy
    /// </summary>
    /// <returns></returns>
    public static string FormulaFileToName(string n) {
        var parts = n.Split('/');
        var name = parts[parts.Length - 1];
        name = name.Split('.')[0];

        return name;
    }

    // TODO: This should be moved to the Procedure class
    private void _ReadProcedure(Procedure proc, IEnumerable<string> fileContent, string fileName) {
        Log.Debug($"Parsing {proc.Name} for {fileName}...");

        bool insideProcedure = false;
        bool beginFound = false;
        bool endFound = false;
        StringBuilder sb = new();

        foreach (string line in fileContent) {
            if (line.StartsWith($"BEGIN {proc.Name}")) {
                insideProcedure = true;
                beginFound = true;
                continue;
            }

            if (insideProcedure) {
                if (line.StartsWith($"END {proc.Name}")) {
                    insideProcedure = false;
                    endFound = true;
                    sb.AppendLine(":");
                    break;
                }
                if (!string.IsNullOrWhiteSpace(line))
                    sb.AppendLine(line.Trim());
            }
        }

        if (!beginFound || !endFound) {
            proc.State = Procedure.STATE.INVALID;
            return;
        }

        proc.Value = sb.ToString().Split(Environment.NewLine.ToCharArray());
        proc.State = Procedure.STATE.VALID;
    }

    private void _EvalInfoProcedure(IEnumerable<string> content) {
        // NOTE: I don't like how Switches and properties are two different things
        // Because of this, I have to make this chain of foreaches
        // Maybe all properties inside the INFO proc should be in one variable?

        foreach (var line in content) {
            foreach (var prop in Properties) {
                if (line.StartsWith(prop.Key)) {
                    Properties[prop.Key].Value = line.Replace("\"", "").Replace($"{prop.Key}=", "");
                }
            }
        }

        foreach (var key in Switches.Keys) {
            foreach (string line in content) {
                // NOTE: The part after && is an AWFUL hack
                if (line.StartsWith(key) && !line.StartsWith("SRC_URL")) {
                    Switches[key] = true;
                }
            }
        }

        string[] dependsonValues = Properties["DEPENDSON"].Value.Trim().Replace("\"", "").Trim().Split(',');

        // NOTE: I don't know if I like the way unset properties are handled.
        // Maybe there should be a `bool isSet` inside the Property class?
        if (dependsonValues[0] != String.Empty) {
            foreach (string value in dependsonValues) {
                Dependencies.Add(new Formula(value.Trim()));
            }
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
        export ROOT=""{(Globals.IsSandboxMode ? $"/tmp/paka-sandbox-{Name}" : "/")}""
        ");
        p.StandardInput.Write(procedure);
        p.StandardInput.Close();
        p.WaitForExit();
    }
}