using TUnit.Core.Enums;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Represents the priority of a test for ordering purposes
/// </summary>
internal readonly struct TestPriority : IComparable<TestPriority>
{
    public Priority Priority { get; }
    public int Order { get; }

    public TestPriority(Priority priority, int order)
    {
        Priority = priority;
        Order = order;
    }

    public int CompareTo(TestPriority other)
    {
        var priorityComparison = ((int)other.Priority).CompareTo((int)Priority);
        if (priorityComparison != 0)
            return priorityComparison;

        return Order.CompareTo(other.Order);
    }
}
