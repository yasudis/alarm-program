using System.Net.NetworkInformation;
using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Infrastructure.HostWatch;

public sealed class SystemIcmpPingProbe : IIcmpPingProbe
{
    public async Task<bool> IsReachableAsync(string host, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 3000);
            return reply.Status == IPStatus.Success;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (PingException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
