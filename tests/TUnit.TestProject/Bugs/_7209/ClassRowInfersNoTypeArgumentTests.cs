using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7209;

// The class row's value goes to an int constructor parameter, not to T, so it supplies no class type
// argument. T comes from the method rows, as it does without a class row: C<int> for [Arguments(1)]
// and C<object> for [Arguments(null)] (#7141). Each instance is constructed from the class row
// (number = 42), never from a method row. Same in both modes.
[EngineTest(ExpectedResult.Pass)]
[Arguments(42)]
public class ClassRowInfersNoTypeArgument<T>(int number)
{
    [Test]
    [Arguments(null)]
    [Arguments(1)]
    public async Task Run(T value)
    {
        await Assert.That(number).IsEqualTo(42);
        await Assert.That(typeof(T)).IsEqualTo(value is null ? typeof(object) : typeof(int));
        await Assert.That((object?)value is null or 1).IsTrue();
    }
}

// A class row that does infer T keeps supplying it: the method rows run on C<string>, and the null row
// infers nothing of its own.
[EngineTest(ExpectedResult.Pass)]
[Arguments("class")]
public class ClassRowInfersTypeArgument<T>(T classValue)
{
    [Test]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)classValue).IsEqualTo("class");
        await Assert.That(value).IsNull();
    }
}
