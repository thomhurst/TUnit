using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7106;

// Edge cases for method-level [Arguments] rows that infer a generic class's type arguments (#7106).

// A test method with an overload must resolve to its own counterpart on the constructed type
// rather than failing with an ambiguous name lookup.
[EngineTest(ExpectedResult.Pass)]
public class OverloadedGenericClassMethodRows<T>
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Run(T value)
    {
        await Assert.That(value).IsTypeOf<int>();
    }

    public Task Run(T value, int other) => Task.CompletedTask;
}
