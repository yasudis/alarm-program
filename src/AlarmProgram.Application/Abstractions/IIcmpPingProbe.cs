namespace AlarmProgram.Application.Abstractions;

public interface IIcmpPingProbe
{
    Task<bool> IsReachableAsync(string host, CancellationToken cancellationToken = default);
}
