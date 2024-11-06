using System.Runtime.InteropServices;

namespace ResoniteWikiMine.Utility;

/// <summary>
/// Helper classes for dealing with console output.
/// </summary>
public static unsafe partial class ConsoleHelper
{
    public static void SetInputEchoEnabled(bool enabled)
    {
        if (OperatingSystem.IsWindows())
            SetInputEchoEnabledWindows(enabled);

        // TODO: Unix platforms.
    }

    private static void SetInputEchoEnabledWindows(bool enabled)
    {
        var handle = GetStdHandle(unchecked((uint) -10));
        uint mode;
        if (GetConsoleMode(handle, &mode) == 0)
            return;

        if (enabled)
            mode |= ENABLE_ECHO_INPUT;
        else
            mode &= ~ENABLE_ECHO_INPUT;

        SetConsoleMode(handle, mode);
    }

    [LibraryImport("KERNEL32.dll")]
    private static partial void* GetStdHandle(uint nStdHandle);

    [LibraryImport("KERNEL32.dll")]
    private static partial int GetConsoleMode(void* hConsoleHandle, uint* lpMode);

    [LibraryImport("KERNEL32.dll")]
    private static partial int SetConsoleMode(void* hConsoleHandle, uint dwMode);

    // ReSharper disable once InconsistentNaming
    private const uint ENABLE_ECHO_INPUT = 0x0004;
}
