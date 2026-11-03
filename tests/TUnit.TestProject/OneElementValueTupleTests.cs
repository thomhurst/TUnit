using System.Collections.Generic;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class OneElementValueTupleTests
{
    public static IEnumerable<ValueTuple<string?>> Rows() =>
        [new("a"), new((string?)null)];

    [Test]
    [MethodDataSource(nameof(Rows))]
    public async Task SingleElement(ValueTuple<string?> value)
    {
        await Assert.That(value.Item1 is null or "a").IsTrue();
    }
}
