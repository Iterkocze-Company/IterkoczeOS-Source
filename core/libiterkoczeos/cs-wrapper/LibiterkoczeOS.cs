namespace Iterkocze;

using System.Runtime.InteropServices;

public class LibiterkoczeOS {
    [DllImport("libiterkoczeos.so", EntryPoint = "GetSystemUser")]
    private static extern IntPtr _GetSystemUser();

    [DllImport("libiterkoczeos.so", EntryPoint = "GetSystemVersion")]
    private static extern IntPtr _GetSystemVersion();
    public static string? GetSystemUser() {
        IntPtr ptr = _GetSystemUser();
        return Marshal.PtrToStringAnsi(ptr);
    }

    public static string? GetSystemVersion() {
        return Marshal.PtrToStringAnsi(_GetSystemVersion());
    }
}
