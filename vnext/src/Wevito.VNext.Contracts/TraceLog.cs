using System.Diagnostics;
using System.Text;

namespace Wevito.VNext.Contracts;

public static class TraceLog
{
    private static readonly object Sync = new();
    private static readonly string? TraceDirectory = ResolveTraceDirectory();
    private static readonly string ProcessName = ResolveProcessName();

    public static bool IsEnabled => !string.IsNullOrWhiteSpace(TraceDirectory);

    public static void Write(string component, string message)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(TraceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(TraceDirectory);
        var path = Path.Combine(TraceDirectory, $"{ProcessName}.trace.log");
        var line = $"{DateTimeOffset.UtcNow:O} | {component} | pid={Environment.ProcessId} | {message}{Environment.NewLine}";

        lock (Sync)
        {
            File.AppendAllText(path, line, Encoding.UTF8);
        }
    }

    private static string? ResolveTraceDirectory()
    {
        var value = Environment.GetEnvironmentVariable("WEVITO_VNEXT_TRACE_DIR");
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Path.GetFullPath(value);
    }

    private static string ResolveProcessName()
    {
        try
        {
            return Process.GetCurrentProcess().ProcessName;
        }
        catch
        {
            return "process";
        }
    }
}
