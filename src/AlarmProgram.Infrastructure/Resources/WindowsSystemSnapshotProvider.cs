using System.Runtime.InteropServices;
using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Infrastructure.Resources;

public sealed class WindowsSystemSnapshotProvider : ISystemSnapshotProvider
{
    public SystemSnapshot Capture(string? primaryIp)
    {
        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        return new SystemSnapshot
        {
            PrimaryIp = primaryIp,
            Uptime = uptime < TimeSpan.Zero ? TimeSpan.Zero : uptime,
            SystemDriveFreePercent = TryReadSystemDriveFreePercent(),
            MemoryUsedPercent = TryReadMemoryUsedPercent()
        };
    }

    private static int? TryReadSystemDriveFreePercent()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory)
                       ?? Path.GetPathRoot(Environment.CurrentDirectory);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            var drive = new DriveInfo(root);
            if (!drive.IsReady || drive.TotalSize <= 0)
            {
                return null;
            }

            return (int)Math.Clamp(Math.Floor(drive.AvailableFreeSpace * 100d / drive.TotalSize), 0, 100);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static int? TryReadMemoryUsedPercent()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var status = new NativeMethods.MemoryStatusEx { Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>() };
        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return null;
        }

        return (int)Math.Clamp((int)status.MemoryLoad, 0, 100);
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
    }
}
