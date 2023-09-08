namespace Iterkocze.LibiterkoczeOS;

using System.Runtime.InteropServices;

public class LibiterkoczeOS {
    [DllImport("libiterkoczeos.so", EntryPoint = "GetSystemUser")]
    private static extern IntPtr _GetSystemUser();

    public static string? GetSystemUser() {
        IntPtr ptr = _GetSystemUser();
        return Marshal.PtrToStringAnsi(ptr);
    }
}
