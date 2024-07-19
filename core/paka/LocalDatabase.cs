public static class LocalDatabase {
    /// <summary>
    /// Register the package in the database located at Globals.PAKA_DBDIR
    /// </summary>
    /// <param name="packageName"></param>
    public static void RegisterPackage(string packageName) {
        Directory.CreateDirectory(Globals.PAKA_DBDIR + packageName);
    }

    /// <summary>
    /// Unregsiter package from the local database and remove any data it had
    /// </summary>
    /// <param name="packageName"></param>
    public static void UnregisterPackage(string packageName) {
        Directory.Delete(Globals.PAKA_DBDIR + packageName, true);
    }

    /// <summary>
    /// Determine if the package was directly installed by the OS user.
    /// It's preffered to check on the Formula object directly instead of calling this method
    /// Formula class constructor uses it to set it's property
    /// </summary>
    /// <param name="packageName"></param>
    /// <returns></returns>
    public static bool IsDirectlyInstalled(string packageName) {
        return File.ReadAllLines(Globals.PAKA_DBDIR + "direct").Any(pkg => pkg == packageName);
    }

    public static bool IsInstalled(string packageName) {
        return Directory.Exists(Globals.PAKA_DBDIR + packageName);
    }

    public static void MarkAsDirectlyInstalled(string packageName) {
        if (File.ReadAllLines(Globals.PAKA_DBDIR + "direct").Any(pkg => pkg == packageName)) {
            Log.Debug($"{packageName} is already marked as directly installed. Ignoring");
            return;
        }

        File.AppendAllLines(Globals.PAKA_DBDIR + "direct", new []{packageName});
    }

    public static void UnmarkAsDirectlyInstalled(string packageName) {
        var text = File.ReadAllLines(Globals.PAKA_DBDIR + "direct").Where(line => line != packageName).ToArray();
        File.WriteAllLines(Globals.PAKA_DBDIR + "direct", text);
    }

    public static void WriteDBFile(string forPackage, string dbFilename, IEnumerable<string> content, bool overwrite) {
        if (File.Exists(Globals.PAKA_DBDIR + forPackage + "/" + dbFilename) && !overwrite) 
            return;
        
        File.WriteAllLines(Globals.PAKA_DBDIR + forPackage + "/" + dbFilename, content);
    }

    public static string[] ReadDBFile(string forPackage, string dbFilename) {
        return File.ReadAllLines(Globals.PAKA_DBDIR + forPackage + "/" + dbFilename);
    }

    public static string[] GetAllInstalledPackages() {
        var dirs = Directory.GetDirectories(Globals.PAKA_DBDIR);
        List<string> ret = new();
        foreach (var dir in dirs) {
            var parts = dir.Split('/');
            ret.Add(parts[parts.Length - 1]);
        }

        return ret.ToArray();
    }

    // NOTE: Isn't this kinda dangerous?
    // What if I have hundrets of formula files in the future and it's going to use 69420 GB of RAM?
    // This is silly. This is silly and danger
    public static Formula[] GetAllFormulas(string onlyInDir = "") {
        string searchDir = onlyInDir == "" ? Globals.PAKA_FORMULADIR : Globals.PAKA_FORMULADIR + onlyInDir;
        List<Formula> ret = new();
        foreach (string name in Directory.GetFiles(searchDir, "*.*", SearchOption.AllDirectories)) {
            ret.Add(new Formula(Path.GetRelativePath(Globals.PAKA_FORMULADIR, name)));
        }

        return ret.ToArray();
    }

    public static List<uint> GetInstalledUpdateIDs() {
        return File.ReadAllLines(Globals.DB_LOCAL_UPDATE_FILE).Select(uint.Parse).ToList();
    }

    public static void MarkUpdateIDAsInstalled(uint id) {
        File.AppendAllText(Globals.DB_LOCAL_UPDATE_FILE, $"{id.ToString()}\n");
    }

    // TODO: This can crash if the file has invalid data
    public static uint GetUpdateVersion() {
        uint ret = 0;

        foreach (var line in File.ReadAllLines(Globals.DB_LOCAL_UPDATE_FILE)) {
            ret += uint.Parse(line);
        }

        return ret;
    }
}