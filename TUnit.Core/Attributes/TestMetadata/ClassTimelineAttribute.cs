using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Opts a test class (or every class in an assembly) into a specific HTML-report
/// class-timeline rendering mode. Class-level usage wins over assembly-level usage.
/// </summary>
/// <remarks>
/// <para>
/// Useful for BDD-style chains of <see cref="DependsOnAttribute"/> tests where seeing the
/// whole flow on one timeline matters, without making every class noisier in the report.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ClassTimeline(TimelineMode.FullExecution)]
/// public class OrderProcessingFlow
/// {
///     [Test] public Task SetupWiremock() => Task.CompletedTask;
///
///     [Test, DependsOn(nameof(SetupWiremock))]
///     public Task SendMessage() => Task.CompletedTask;
///
///     [Test, DependsOn(nameof(SendMessage))]
///     public Task VerifyWiremockWasCalled() => Task.CompletedTask;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
public sealed class ClassTimelineAttribute(TimelineMode mode) : TUnitAttribute, ITestDiscoveryEventReceiver, IScopedAttribute
{
    /// <summary>The timeline rendering mode to apply.</summary>
    public TimelineMode Mode { get; } = mode;

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public Type ScopeType => typeof(ClassTimelineAttribute);

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        context.AddProperty(ClassTimelinePropertyKey, Mode.ToString());
        return default;
    }

    /// <summary>
    /// Custom-property key used to round-trip the chosen <see cref="TimelineMode"/> into
    /// <c>TestDetails.CustomProperties</c> so the HTML reporter can read it back per class.
    /// </summary>
    internal const string ClassTimelinePropertyKey = "tunit.report.timeline";
}
