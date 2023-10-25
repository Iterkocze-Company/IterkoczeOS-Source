public static class Log {
    public static void Error(string msg) {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[ERROR] " + msg);
        Console.ForegroundColor = oldColor;
    }
    public static void Info(string msg) {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[INFO] " + msg);
        Console.ForegroundColor = oldColor;
    }
    public static void Success(string msg) {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("[SUCCESS] " + msg);
        Console.ForegroundColor = oldColor;
    }
    public static void Warning(string msg) {
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("[WARNING] " + msg);
        Console.ForegroundColor = oldColor;
    }

    /// <summary>
    /// Will output only if Globals.IsDebugMode is set
    /// </summary>
    /// <param name="msg"></param>
    public static void Debug(string msg) {
        if (!Globals.IsDebugMode) return;
        ConsoleColor oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("[DEBUG] " + msg);
        Console.ForegroundColor = oldColor;
    }
}