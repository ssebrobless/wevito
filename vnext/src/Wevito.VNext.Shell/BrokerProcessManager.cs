using System.IO;
using System.Diagnostics;

namespace Wevito.VNext.Shell;

internal static class BrokerProcessManager
{
    public static Process Start(string pipeName)
    {
        var (fileName, arguments) = ResolveBrokerLaunch(pipeName);
        return Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = AppContext.BaseDirectory
        }) ?? throw new InvalidOperationException("Failed to launch Wevito broker.");
    }

    public static string ResolveContentRoot()
    {
        var nextToExe = Path.Combine(AppContext.BaseDirectory, "content");
        if (Directory.Exists(nextToExe))
        {
            return nextToExe;
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        if (repoRoot is not null)
        {
            var candidate = Path.Combine(repoRoot, "vnext", "content");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not resolve vNext content directory.");
    }

    private static (string FileName, string Arguments) ResolveBrokerLaunch(string pipeName)
    {
        var exeNextToShell = Path.Combine(AppContext.BaseDirectory, "Wevito.VNext.Broker.exe");
        if (File.Exists(exeNextToShell))
        {
            return (exeNextToShell, $"--pipe-name {pipeName}");
        }

        var dllNextToShell = Path.Combine(AppContext.BaseDirectory, "Wevito.VNext.Broker.dll");
        if (File.Exists(dllNextToShell))
        {
            return ("dotnet", $"\"{dllNextToShell}\" --pipe-name {pipeName}");
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        if (repoRoot is not null)
        {
            var configuration = AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                ? "Debug"
                : "Release";
            var sourceExe = Path.Combine(repoRoot, "vnext", "src", "Wevito.VNext.Broker", "bin", configuration, "net8.0-windows", "Wevito.VNext.Broker.exe");
            if (File.Exists(sourceExe))
            {
                return (sourceExe, $"--pipe-name {pipeName}");
            }
        }

        throw new FileNotFoundException("Could not find Wevito.VNext.Broker executable.");
    }

    private static string? FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "vnext")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
