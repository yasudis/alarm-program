namespace AlarmProgram.Application.Abstractions;

public interface ISmtpMailSender
{
    Task SendAsync(SmtpMailRequest request, CancellationToken cancellationToken = default);
}

public sealed class SmtpMailRequest
{
    public required string Host { get; init; }

    public required int Port { get; init; }

    public required bool UseSsl { get; init; }

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public required string From { get; init; }

    public required IReadOnlyList<string> To { get; init; }

    public required string Subject { get; init; }

    public required string Body { get; init; }
}
