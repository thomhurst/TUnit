using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ArgumentDisplayFormatterTests
{
    [Test]
    [MethodDataSource(nameof(Data1))]
    [ArgumentDisplayFormatter<FooFormatter>]
    public async Task FormatterShouldBeAppliedToMethodDataSource(Foo foo)
    {
        // Verify the formatter was applied by checking the display name
        var displayName = TestContext.Current!.GetDisplayName();
        await Assert.That(displayName).IsEqualTo("FormatterShouldBeAppliedToMethodDataSource(FooFormatterValue)");
    }

    [Test]
    [Arguments(1, 2, 3)]
    [ArgumentDisplayFormatter<IntFormatter>]
    public async Task FormatterShouldBeAppliedToArguments(int a, int b, int c)
    {
        // Verify the formatter was applied by checking the display name
        var displayName = TestContext.Current!.GetDisplayName();
        await Assert.That(displayName).IsEqualTo("FormatterShouldBeAppliedToArguments(INT:1, INT:2, INT:3)");
    }

    [Test]
    [MethodDataSource(nameof(DataWithException))]
    [ArgumentDisplayFormatter<BarFormatter>]
    public async Task FormatterShouldPreventExceptionInToString(Bar bar)
    {
        // The Bar.ToString() throws, but the formatter should prevent that
        var displayName = TestContext.Current!.GetDisplayName();
        await Assert.That(displayName).IsEqualTo("FormatterShouldPreventExceptionInToString(BarFormatterValue)");
    }

    public static IEnumerable<Foo> Data1() => [new Foo()];

    public static IEnumerable<Bar> DataWithException() => [new Bar()];
}

public class Foo
{
    public override string ToString() => throw new Exception("Foo.ToString should not be called");
}

public class Bar
{
    public override string ToString() => throw new Exception("Bar.ToString should not be called");
}

public class FooFormatter : ArgumentDisplayFormatter
{
    public override bool CanHandle(object? value) => value is Foo;

    public override string FormatValue(object? value) => "FooFormatterValue";
}

public class BarFormatter : ArgumentDisplayFormatter
{
    public override bool CanHandle(object? value) => value is Bar;

    public override string FormatValue(object? value) => "BarFormatterValue";
}

public class IntFormatter : ArgumentDisplayFormatter
{
    public override bool CanHandle(object? value) => value is int;

    public override string FormatValue(object? value) => $"INT:{value}";
}
