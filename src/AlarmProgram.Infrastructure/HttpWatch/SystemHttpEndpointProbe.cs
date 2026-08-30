using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Infrastructure.HttpWatch;

public sealed class SystemHttpEndpointProbe : IHttpEndpointProbe
{
    private readonly HttpClient _httpClient;

    public SystemHttpEndpointProbe(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsHealthyAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            using var response = await _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);
            var code = (int)response.StatusCode;
            return code is >= 200 and < 300;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
