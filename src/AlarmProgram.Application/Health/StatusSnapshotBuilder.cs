using AlarmProgram.Domain;

namespace AlarmProgram.Application.Health;

public static class StatusSnapshotBuilder
{
    public static MachineEvent Build(HostStatusFacts facts, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var timestamp = now ?? DateTimeOffset.UtcNow;
        var muteText = facts.IsMuted
            ? facts.MutedUntil is { } until
                ? $"тишина до {until.ToLocalTime():HH:mm}"
                : "тишина включена"
            : "нет";
        var networkText = facts.NetworkAvailable ? "доступна" : "нет";
        var ip = string.IsNullOrWhiteSpace(facts.PrimaryIp) ? "-" : facts.PrimaryIp.Trim();
        var disk = string.IsNullOrWhiteSpace(facts.DiskSummary) ? "н/д" : facts.DiskSummary.Trim();

        var message =
            $"Uptime: {HostUptimeFormatter.Format(facts.Uptime)}" + Environment.NewLine +
            $"IP: {ip}" + Environment.NewLine +
            $"Сеть: {networkText}" + Environment.NewLine +
            $"Диск: {disk}" + Environment.NewLine +
            $"Перезагрузка ожидается: {(facts.RebootPending ? "да" : "нет")}" + Environment.NewLine +
            $"Тишина: {muteText}" + Environment.NewLine +
            $"Мониторинг: {(facts.MonitoringPaused ? "пауза" : "включён")}";

        return new MachineEvent
        {
            Type = MachineEventType.StatusSnapshot,
            OccurredAt = timestamp,
            Source = "StatusSnapshot",
            HostName = Environment.MachineName,
            Message = message
        };
    }

    public static string DescribeSystemDrive()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory);
            if (string.IsNullOrWhiteSpace(root))
            {
                return "н/д";
            }

            var drive = new DriveInfo(root);
            if (!drive.IsReady || drive.TotalSize <= 0)
            {
                return "диск недоступен";
            }

            var freePercent = (int)Math.Floor(drive.AvailableFreeSpace * 100d / drive.TotalSize);
            return $"{drive.Name} свободно {freePercent}% ({FormatBytes(drive.AvailableFreeSpace)} из {FormatBytes(drive.TotalSize)})";
        }
        catch
        {
            return "н/д";
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double mega = 1024d * 1024d;
        if (bytes >= mega * 1024d)
        {
            return $"{bytes / (mega * 1024d):0.0} ГБ";
        }

        return $"{bytes / mega:0.0} МБ";
    }
}
