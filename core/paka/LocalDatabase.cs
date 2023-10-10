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

        File.WriteAllLines(Globals.PAKA_DBDIR + "direct", new []{packageName});
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
}