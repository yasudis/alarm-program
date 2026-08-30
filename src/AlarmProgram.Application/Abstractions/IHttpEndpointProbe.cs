namespace AlarmProgram.Application.Abstractions;

public interface IHttpEndpointProbe
{
    Task<bool> IsHealthyAsync(string url, CancellationToken cancellationToken = default);
}
