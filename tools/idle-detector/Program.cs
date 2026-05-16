using System.Runtime.InteropServices;

var idle = IdleDetector.GetIdleTime();
Console.WriteLine($$"""{"idleMilliseconds":{{(long)idle.TotalMilliseconds}},"idleSeconds":{{idle.TotalSeconds:0}}}""");

internal static class IdleDetector
{
    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo
        {
            CbSize = (uint)Marshal.SizeOf<LastInputInfo>()
        };
        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var tickCount = Environment.TickCount64;
        var idleMilliseconds = Math.Max(0, tickCount - info.DwTime);
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint CbSize;
        public uint DwTime;
    }
}
