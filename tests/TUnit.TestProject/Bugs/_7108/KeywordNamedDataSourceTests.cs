using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7108;

/// <summary>
/// Data source members named after C# keywords must be accessed as <c>@name</c> in generated code, which
/// would otherwise not compile.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class KeywordNamedDataSourceTests
{
    public static readonly object @event = new[] { 61, 62 };

    public static int[] @class => [63, 64];

    public static IEnumerable<int> @return() => [65, 66];

    [Test]
    [MethodDataSource(nameof(@event))]
    public async Task Keyword_Named_Field(int value)
    {
        await Assert.That(value).IsBetween(61, 62);
    }

    [Test]
    [MethodDataSource(nameof(@class))]
    public async Task Keyword_Named_Property(int value)
    {
        await Assert.That(value).IsBetween(63, 64);
    }

    [Test]
    [MethodDataSource(nameof(@return))]
    public async Task Keyword_Named_Method(int value)
    {
        await Assert.That(value).IsBetween(65, 66);
    }
}
