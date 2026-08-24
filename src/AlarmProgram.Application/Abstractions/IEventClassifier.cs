using AlarmProgram.Application.Contracts;
using AlarmProgram.Domain;

namespace AlarmProgram.Application.Abstractions;

public interface IEventClassifier
{
    MachineEvent? Classify(RawSystemEvent rawEvent);
}
