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

    /// <summary>
    /// Tests for issue #3851 - IsEquivalentTo with custom comparer should be AOT-compatible
    /// When a custom comparer is provided, no reflection is used, so the method should not
    /// have RequiresUnreferencedCode attribute and should be safe for AOT.
    /// </summary>
    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_ShouldNotTriggerAotWarnings()
    {
        // This test verifies that using IsEquivalentTo with a custom comparer doesn't trigger IL2026/IL3050
        // The custom comparer path doesn't use StructuralEqualityComparer which requires reflection
        var list1 = new List<int> { 1, 2, 3 };
        var list2 = new List<int> { 3, 2, 1 };

        // Using explicit comparer - should be AOT-safe
        await Assert.That(list1).IsEquivalentTo(list2, EqualityComparer<int>.Default);
    }

    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_OrderMatching_ShouldNotTriggerAotWarnings()
    {
        // Verify that custom comparer works with both ordering modes
        var list1 = new List<string> { "a", "b", "c" };
        var list2 = new List<string> { "a", "b", "c" };

        // Using custom comparer with order matching - should be AOT-safe
        await Assert.That(list1).IsEquivalentTo(list2, StringComparer.OrdinalIgnoreCase, CollectionOrdering.Matching);
    }
}
