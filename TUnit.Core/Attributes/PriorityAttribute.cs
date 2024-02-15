using TUnit.Core.Enums;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PriorityAttribute : PropertyAttribute
{
    public PriorityLevel PriorityLevel { get; }

    public PriorityAttribute(PriorityLevel priorityLevel) : base("Priority", priorityLevel.ToString())
    {
        PriorityLevel = priorityLevel;
    }
}