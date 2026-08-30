using AlarmProgram.Application.Abstractions;

namespace AlarmProgram.Application.Health;

public sealed class TickCountHostUptimeProvider : IHostUptimeProvider
{
    public TimeSpan GetUptime() => TimeSpan.FromMilliseconds(Environment.TickCount64);
}
