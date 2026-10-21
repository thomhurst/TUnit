using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
[Arguments(1)]
public class GenerateGenericTestGenericClassArgumentsRowsTests<T>(T classValue)
{
    // Source-gen mode closes the class and the method together from each class row and method row, then runs
    // every method [Arguments] row for that instantiation: <int, int> runs (1) and (2), and the
    // [GenerateGenericTest] pair adds nothing. Reflection mode dropped the second row because it infers the same
    // <int> as the first.
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Repeated_Rows_Run_On_A_Generic_Class<TValue>(TValue value)
    {
        await Assert.That(classValue).IsEqualTo((T)(object)1);
        await Assert.That(value!.GetType()).IsEqualTo(typeof(TValue));
    }
}
