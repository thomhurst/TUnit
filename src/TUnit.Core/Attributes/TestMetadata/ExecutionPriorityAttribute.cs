using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Sets the execution priority for a test, class, or assembly.
/// Higher priority tests are scheduled to execute before lower priority tests.
/// </summary>
/// <remarks>
/// Priority values range from <see cref="Enums.Priority.Low"/> (runs last) to
/// <see cref="Enums.Priority.Critical"/> (runs first). The default is <see cref="Enums.Priority.Normal"/>.
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [ExecutionPriority(Priority.High)]
/// public void ImportantTest() { }
///
/// [Test]
/// [ExecutionPriority(Priority.Low)]
/// public void LessImportantTest() { }
/// </code>
/// </example>
/// <param name="priority">The execution priority level.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class ExecutionPriorityAttribute : SingleTUnitAttribute, ITestDiscoveryEventReceiver, IScopedAttribute
{
    /// <summary>
    /// Gets the execution priority level for the test.
    /// </summary>
    public Priority Priority { get; }

    /// <inheritdoc />
    public int Order => 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionPriorityAttribute"/> class.
    /// </summary>
    /// <param name="priority">The execution priority level. Defaults to <see cref="Enums.Priority.Normal"/>.</param>
    public ExecutionPriorityAttribute(Priority priority = Priority.Normal)
    {
        Priority = priority;
    }

    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.SetPriority(Priority);
        return default(ValueTask);
    }

    public Type ScopeType => typeof(ExecutionPriorityAttribute);
}
