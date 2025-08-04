using TUnit.Core;
using TUnit.Core.Enums;

namespace TUnit.Engine.Scheduling;

internal class TestExecutionData
{
    public required AbstractExecutableTest Test { get; init; }
    public required ExecutionContext? ExecutionContext { get; init; }
    public required List<string> Constraints { get; init; }
    public required Priority Priority { get; init; }
    public required TestExecutionState State { get; init; }
}