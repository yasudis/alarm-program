using System.Runtime.InteropServices;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Application.Health;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Resources;

public sealed class WindowsResourceMonitor : IResourceMonitor
{
    private readonly ILogger<WindowsResourceMonitor> _logger;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;
    private bool _cpuWasHigh;
    private bool _memoryWasHigh;
    private ulong? _previousIdle;
    private ulong? _previousKernel;
    private ulong? _previousUser;

    public WindowsResourceMonitor(ILogger<WindowsResourceMonitor> logger)
    {
        _logger = logger;
    }

    public event EventHandler<MachineEvent>? ResourceEventDetected;

    public void Start()
    {
        lock (_sync)
        {
            if (_started || _disposed)
            {
                return;
            }

            if (!OperatingSystem.IsWindows())
            {
                _logger.LogInformation("Монитор CPU/памяти пропущен: не Windows");
                return;
            }

            _started = true;
        }

        _logger.LogInformation("Монитор CPU и памяти запущен");
    }

    public void Poll(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!_started || _disposed || !OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            if (settings.NotifyOnHighCpu)
            {
                TryRaiseCpu(settings.HighCpuThresholdPercent);
            }

            if (settings.NotifyOnHighMemory)
            {
                TryRaiseMemory(settings.HighMemoryThresholdPercent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка проверки CPU/памяти");
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            _started = false;
        }
    }

    private void TryRaiseCpu(int thresholdPercent)
    {
        if (!TryReadCpuPercent(out var percent))
        {
            return;
        }

        var alert = SystemHealthRules.HighCpu(percent, thresholdPercent);
        lock (_sync)
        {
            if (alert is null)
            {
                _cpuWasHigh = false;
                return;
            }

            if (_cpuWasHigh)
            {
                return;
            }

            _cpuWasHigh = true;
        }

        ResourceEventDetected?.Invoke(this, alert);
    }

    private void TryRaiseMemory(int thresholdPercent)
    {
        if (!TryReadMemoryLoad(out var percent))
        {
            return;
        }

        var alert = SystemHealthRules.HighMemory(percent, thresholdPercent);
        lock (_sync)
        {
            if (alert is null)
            {
                _memoryWasHigh = false;
                return;
            }

            if (_memoryWasHigh)
            {
                return;
            }

            _memoryWasHigh = true;
        }

        ResourceEventDetected?.Invoke(this, alert);
    }

    private bool TryReadCpuPercent(out int percent)
    {
        percent = 0;
        if (!NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return false;
        }

        var idle = ToUInt64(idleTime);
        var kernel = ToUInt64(kernelTime);
        var user = ToUInt64(userTime);

        ulong? previousIdle;
        ulong? previousKernel;
        ulong? previousUser;
        lock (_sync)
        {
            previousIdle = _previousIdle;
            previousKernel = _previousKernel;
            previousUser = _previousUser;
            _previousIdle = idle;
            _previousKernel = kernel;
            _previousUser = user;
        }

        if (previousIdle is null || previousKernel is null || previousUser is null)
        {
            return false;
        }

        var idleDelta = idle - previousIdle.Value;
        var kernelDelta = kernel - previousKernel.Value;
        var userDelta = user - previousUser.Value;
        var total = kernelDelta + userDelta;
        if (total == 0)
        {
            return false;
        }

        var busy = total - idleDelta;
        percent = (int)Math.Clamp(Math.Round(busy * 100d / total), 0, 100);
        return true;
    }

    private static bool TryReadMemoryLoad(out int percent)
    {
        percent = 0;
        var status = new NativeMethods.MemoryStatusEx { Length = (uint)Marshal.SizeOf<NativeMethods.MemoryStatusEx>() };
        if (!NativeMethods.GlobalMemoryStatusEx(ref status))
        {
            return false;
        }

        percent = (int)Math.Clamp((int)status.MemoryLoad, 0, 100);
        return true;
    }

    private static ulong ToUInt64(NativeMethods.FileTime fileTime) =>
        ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct FileTime
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

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
        internal static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
    }
}
