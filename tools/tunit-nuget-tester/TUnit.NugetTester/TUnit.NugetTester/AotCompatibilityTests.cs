namespace TUnit.NugetTester;

/// <summary>
/// Tests for issue #3321 - AOT analysis warnings when using enums in Arguments attribute
/// This test verifies that no IL3050 warnings are emitted when PublishAot is enabled
///
/// Per the issue report, code like this was triggering warnings:
/// - [Arguments(Enum1.Value1)] with enums
///
/// The warning was:
/// - IL3050: Using Array.CreateInstance which requires dynamic code
///
/// This has been suppressed in CastHelper.cs with an UnconditionalSuppressMessage
/// since the array creation happens at test discovery time, not during AOT-compiled execution.
///
/// Note: MatrixDataSource is intentionally NOT AOT-compatible and is marked with
/// RequiresUnreferencedCode/RequiresDynamicCode attributes.
/// </summary>
public class AotCompatibilityTests
{
    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    // Test from issue #3321 - enum parameters in Arguments attribute
    [Test]
    [Arguments(TestEnum.Value1)]
    [Arguments(TestEnum.Value2)]
    [Arguments(TestEnum.Value3)]
    public async Task EnumArguments_ShouldNotTriggerAotWarnings(TestEnum enumValue)
    {
        // This test verifies that using enum values in Arguments doesn't trigger IL3050
        await Assert.That(enumValue).IsDefined();
    }

    // Test property injection with AOT - verifies no warnings when using property injection in source-gen mode
    [Arguments("test value")]
    public required string? InjectedProperty { get; set; }

    [Test]
    public async Task PropertyInjection_ShouldNotTriggerAotWarnings()
    {
        // This test verifies that property injection works with AOT via source generation
        // The source generator creates PropertyInjectionData at compile time without using reflection
        await Assert.That(InjectedProperty).IsNotNull();
        await Assert.That(InjectedProperty).IsEqualTo("test value");
    }
}
