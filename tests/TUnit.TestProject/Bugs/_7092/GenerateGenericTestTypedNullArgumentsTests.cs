using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestTypedNullArgumentsTests
{
    // Source-gen mode runs <string>("a") and <object>(null): a null [Arguments] value, typed or not, is an
    // element of the params object?[] argument, so the generator infers object for it.
    // [GenerateGenericTest(typeof(string))] adds nothing because the "a" row produced <string>. Reflection mode
    // inferred nothing from the null row, so it never ran.
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("a")]
    [Arguments((string?)null)]
    public async Task Null_Row_Runs_As_Object<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(value is null ? typeof(object) : typeof(string));
    }
}
