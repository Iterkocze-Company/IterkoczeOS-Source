using Iterkocze;

public static class Test {
    public static void Run() {
        Log.Info("Running test 'CreateAllPossibleFormulaFiles'");
        //string[] allFormulaFiles = Directory.GetFiles(Globals.PAKA_FORMULADIR);
        Log.Info("Running test 'LibiterkoczeOS'");
        Console.WriteLine(LibiterkoczeOS.GetSystemUser());
    }
}