using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class Program
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private const int WatchSleepMs = 40;
    private const int LeftMouseVk = 0x01;
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const int SwShow = 5;
    private const int SwRestore = 9;

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Missing command.");
            return 1;
        }

        var command = args[0].Trim().ToLowerInvariant();
        var options = ParseOptions(args);

        try
        {
            return command switch
            {
                "watch-focus" => RunWatchFocus(options),
                "activate-window" => RunActivateWindow(options),
                "left-click" => RunLeftClick(options),
                _ => Fail("Unknown command: " + command)
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < args.Length; i++)
        {
            var key = args[i];
            if (!key.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var value = "true";
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[i + 1];
                i++;
            }

            options[key] = value;
        }

        return options;
    }

    private static int RunWatchFocus(Dictionary<string, string> options)
    {
        var parentPid = GetRequiredInt(options, "--parent-pid");
        var statePath = GetRequiredString(options, "--state-path");
        var stopPath = GetRequiredString(options, "--stop-path");
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);

        var lastJson = string.Empty;
        var lastLeftDown = false;
        long lastLeftPressMs = 0;
        long lastLeftReleaseMs = 0;
        NativePoint lastLeftPressPoint = default;
        NativePoint lastLeftReleasePoint = default;

        while (true)
        {
            if (File.Exists(stopPath) || !ProcessExists(parentPid))
            {
                return 0;
            }

            var hwnd = NativeMethods.GetForegroundWindow();
            var foregroundPid = 0;
            var foregroundHwnd = hwnd.ToInt64();
            var windowTitle = string.Empty;
            var windowClass = string.Empty;

            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
                foregroundPid = unchecked((int)pid);
                windowTitle = GetWindowText(hwnd);
                windowClass = GetClassName(hwnd);
            }

            var processName = string.Empty;
            if (foregroundPid > 0)
            {
                try
                {
                    processName = Process.GetProcessById(foregroundPid).ProcessName;
                }
                catch
                {
                    processName = string.Empty;
                }
            }

            NativeMethods.GetCursorPos(out var cursorPoint);
            var leftButtonDown = (NativeMethods.GetAsyncKeyState(LeftMouseVk) & 0x8000) != 0;
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (leftButtonDown && !lastLeftDown)
            {
                lastLeftPressMs = nowMs;
                lastLeftPressPoint = cursorPoint;
            }
            else if (!leftButtonDown && lastLeftDown)
            {
                lastLeftReleaseMs = nowMs;
                lastLeftReleasePoint = cursorPoint;
            }

            lastLeftDown = leftButtonDown;

            var payload = new FocusStatePayload
            {
                ForegroundPid = foregroundPid,
                ForegroundHwnd = foregroundHwnd,
                ProcessName = processName,
                WindowClass = windowClass,
                WindowTitle = windowTitle,
                IsShellSurface = windowClass is "Progman" or "WorkerW" or "Shell_TrayWnd",
                LeftButtonDown = leftButtonDown,
                LastLeftPressMs = lastLeftPressMs,
                LastLeftReleaseMs = lastLeftReleaseMs,
                CursorX = cursorPoint.X,
                CursorY = cursorPoint.Y,
                LastLeftPressX = lastLeftPressPoint.X,
                LastLeftPressY = lastLeftPressPoint.Y,
                LastLeftReleaseX = lastLeftReleasePoint.X,
                LastLeftReleaseY = lastLeftReleasePoint.Y,
                UpdatedAtUnixMs = nowMs
            };

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            if (!string.Equals(json, lastJson, StringComparison.Ordinal))
            {
                WriteTextAtomic(statePath, json);
                lastJson = json;
            }

            Thread.Sleep(WatchSleepMs);
        }
    }

    private static int RunActivateWindow(Dictionary<string, string> options)
    {
        var hwnd = new IntPtr(GetRequiredLong(options, "--hwnd"));
        var delayMs = GetOptionalInt(options, "--delay-ms", 0);
        if (delayMs > 0)
        {
            Thread.Sleep(delayMs);
        }

        return ActivateWindow(hwnd) ? 0 : 1;
    }

    private static int RunLeftClick(Dictionary<string, string> options)
    {
        var delayMs = GetOptionalInt(options, "--delay-ms", 0);
        var holdMs = GetOptionalInt(options, "--hold-ms", 18);
        if (delayMs > 0)
        {
            Thread.Sleep(delayMs);
        }

        NativeMethods.mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        if (holdMs > 0)
        {
            Thread.Sleep(holdMs);
        }
        NativeMethods.mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        return 0;
    }

    private static bool ActivateWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var foreground = NativeMethods.GetForegroundWindow();
        var currentThread = NativeMethods.GetCurrentThreadId();
        var foregroundThread = foreground == IntPtr.Zero ? 0U : NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var targetThread = NativeMethods.GetWindowThreadProcessId(hwnd, out _);

        var attachedForeground = false;
        var attachedTarget = false;

        try
        {
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attachedForeground = NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
            }

            if (targetThread != 0 && targetThread != currentThread && targetThread != foregroundThread)
            {
                attachedTarget = NativeMethods.AttachThreadInput(currentThread, targetThread, true);
            }

            if (NativeMethods.IsIconic(hwnd))
            {
                NativeMethods.ShowWindow(hwnd, SwRestore);
            }

            NativeMethods.ShowWindow(hwnd, SwShow);
            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.SetForegroundWindow(hwnd);
            NativeMethods.SetActiveWindow(hwnd);
            NativeMethods.SetFocus(hwnd);

            return NativeMethods.GetForegroundWindow() == hwnd;
        }
        finally
        {
            if (attachedTarget)
            {
                NativeMethods.AttachThreadInput(currentThread, targetThread, false);
            }

            if (attachedForeground)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }

    private static string GetWindowText(IntPtr hwnd)
    {
        var builder = new StringBuilder(512);
        _ = NativeMethods.GetWindowTextW(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr hwnd)
    {
        var builder = new StringBuilder(128);
        _ = NativeMethods.GetClassNameW(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static bool ProcessExists(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteTextAtomic(string path, string content)
    {
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, content, Utf8NoBom);
        if (File.Exists(path))
        {
            File.Replace(tempPath, path, null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }

    private static string GetRequiredString(Dictionary<string, string> options, string key)
    {
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException("Missing required option: " + key);
    }

    private static int GetRequiredInt(Dictionary<string, string> options, string key)
    {
        if (options.TryGetValue(key, out var raw) && int.TryParse(raw, out var value))
        {
            return value;
        }

        throw new InvalidOperationException("Missing or invalid integer option: " + key);
    }

    private static long GetRequiredLong(Dictionary<string, string> options, string key)
    {
        if (options.TryGetValue(key, out var raw) && long.TryParse(raw, out var value))
        {
            return value;
        }

        throw new InvalidOperationException("Missing or invalid integer option: " + key);
    }

    private static int GetOptionalInt(Dictionary<string, string> options, string key, int fallback)
    {
        if (options.TryGetValue(key, out var raw) && int.TryParse(raw, out var value))
        {
            return value;
        }

        return fallback;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    private sealed class FocusStatePayload
    {
        [JsonPropertyName("foreground_pid")]
        public int ForegroundPid { get; init; }

        [JsonPropertyName("foreground_hwnd")]
        public long ForegroundHwnd { get; init; }

        [JsonPropertyName("process_name")]
        public string ProcessName { get; init; } = string.Empty;

        [JsonPropertyName("window_class")]
        public string WindowClass { get; init; } = string.Empty;

        [JsonPropertyName("window_title")]
        public string WindowTitle { get; init; } = string.Empty;

        [JsonPropertyName("is_shell_surface")]
        public bool IsShellSurface { get; init; }

        [JsonPropertyName("left_button_down")]
        public bool LeftButtonDown { get; init; }

        [JsonPropertyName("last_left_press_ms")]
        public long LastLeftPressMs { get; init; }

        [JsonPropertyName("last_left_release_ms")]
        public long LastLeftReleaseMs { get; init; }

        [JsonPropertyName("cursor_x")]
        public int CursorX { get; init; }

        [JsonPropertyName("cursor_y")]
        public int CursorY { get; init; }

        [JsonPropertyName("last_left_press_x")]
        public int LastLeftPressX { get; init; }

        [JsonPropertyName("last_left_press_y")]
        public int LastLeftPressY { get; init; }

        [JsonPropertyName("last_left_release_x")]
        public int LastLeftReleaseX { get; init; }

        [JsonPropertyName("last_left_release_y")]
        public int LastLeftReleaseY { get; init; }

        [JsonPropertyName("updated_at_unix_ms")]
        public long UpdatedAtUnixMs { get; init; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetActiveWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();
    }
}
