using System.Runtime.InteropServices;

namespace SershoCode.FileUpdater.Utils;

public static partial class ConsoleModeManager
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial void SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private const int StdOutputHandle = -11;
    private const uint VirtualTerminalProcessing = 4;

    public static void EnableVirtualTerminalProcessing()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = GetStdHandle(StdOutputHandle);

        GetConsoleMode(handle, out var mode);

        mode |= VirtualTerminalProcessing;

        SetConsoleMode(handle, mode);
    }
}