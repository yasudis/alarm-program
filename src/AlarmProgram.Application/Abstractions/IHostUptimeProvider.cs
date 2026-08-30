namespace AlarmProgram.Application.Abstractions;

public interface IHostUptimeProvider
{
    TimeSpan GetUptime();
}
