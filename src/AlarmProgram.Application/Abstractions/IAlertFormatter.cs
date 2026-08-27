using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IAlertFormatter
{
    AlertMessage Format(MachineEvent machineEvent, UserSettings? settings = null);
}
