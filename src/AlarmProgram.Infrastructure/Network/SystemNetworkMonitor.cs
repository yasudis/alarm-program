using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using AlarmProgram.Application.Abstractions;
using AlarmProgram.Domain;
using Microsoft.Extensions.Logging;

namespace AlarmProgram.Infrastructure.Network;

public sealed class SystemNetworkMonitor : INetworkMonitor
{
    private readonly ILogger<SystemNetworkMonitor> _logger;
    private readonly object _sync = new();
    private bool _started;
    private bool _disposed;
    private string? _lastIp;
    private bool _lastAvailable;

    public SystemNetworkMonitor(ILogger<SystemNetworkMonitor> logger)
    {
        _logger = logger;
        _lastAvailable = NetworkInterface.GetIsNetworkAvailable();
        _lastIp = ResolvePrimaryIpv4();
        CurrentPrimaryIp = _lastIp;
        IsNetworkAvailable = _lastAvailable;
    }

    public event EventHandler<MachineEvent>? NetworkEventDetected;

    public string? CurrentPrimaryIp { get; private set; }

    public bool IsNetworkAvailable { get; private set; }

    public void Start()
    {
        lock (_sync)
        {
            if (_started || _disposed)
            {
                return;
            }

            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            _started = true;
        }

        _logger.LogInformation(
            "Монитор сети запущен. IP={Ip}, Available={Available}",
            CurrentPrimaryIp ?? "-",
            IsNetworkAvailable);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (_started)
            {
                NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            }

            _disposed = true;
            _started = false;
        }
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        try
        {
            var newIp = ResolvePrimaryIpv4();
            string? previousIp;
            lock (_sync)
            {
                previousIp = _lastIp;
                if (string.Equals(previousIp, newIp, StringComparison.Ordinal))
                {
                    return;
                }

                _lastIp = newIp;
                CurrentPrimaryIp = newIp;
            }

            if (string.IsNullOrWhiteSpace(newIp) && string.IsNullOrWhiteSpace(previousIp))
            {
                return;
            }

            var machineEvent = new MachineEvent
            {
                Type = MachineEventType.IpChanged,
                OccurredAt = DateTimeOffset.UtcNow,
                Source = "NetworkMonitor",
                HostName = Environment.MachineName,
                Message = $"IP изменился: {previousIp ?? "-"} -> {newIp ?? "-"}"
            };

            NetworkEventDetected?.Invoke(this, machineEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка обработки смены IP");
        }
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        try
        {
            bool previous;
            lock (_sync)
            {
                previous = _lastAvailable;
                if (previous == e.IsAvailable)
                {
                    return;
                }

                _lastAvailable = e.IsAvailable;
                IsNetworkAvailable = e.IsAvailable;
                if (e.IsAvailable)
                {
                    _lastIp = ResolvePrimaryIpv4();
                    CurrentPrimaryIp = _lastIp;
                }
            }

            var machineEvent = new MachineEvent
            {
                Type = e.IsAvailable ? MachineEventType.NetworkOnline : MachineEventType.NetworkOffline,
                OccurredAt = DateTimeOffset.UtcNow,
                Source = "NetworkMonitor",
                HostName = Environment.MachineName,
                Message = e.IsAvailable
                    ? $"Сеть восстановлена. IP={CurrentPrimaryIp ?? "-"}"
                    : "Сетевая доступность потеряна"
            };

            NetworkEventDetected?.Invoke(this, machineEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка обработки доступности сети");
        }
    }

    internal static string? ResolvePrimaryIpv4()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                var props = nic.GetIPProperties();
                foreach (var address in props.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    if (IPAddress.IsLoopback(address.Address))
                    {
                        continue;
                    }

                    return address.Address.ToString();
                }
            }
        }
        catch
        {
            // ignore and return null
        }

        return null;
    }
}
