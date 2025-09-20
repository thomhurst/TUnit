using System.Collections.Generic;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class SimpleTupleTest
{
    [Test]
    [MethodDataSource(nameof(TupleData))]
    public async Task Test_SingleTupleParam((int, int) value)
    {
        await Assert.That(value.Item1).IsEqualTo(1);
        await Assert.That(value.Item2).IsEqualTo(2);
    }

    public static IEnumerable<Func<(int, int)>> TupleData()
    {
        return [() => (1, 2)];
    }
}