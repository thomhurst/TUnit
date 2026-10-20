using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7141;

// [Arguments(null)] passes a null params array, which ArgumentsAttribute stores as one null value. On a
// generic class with no [GenerateGenericTest] or class-level data source, that value infers object for
// the class's type argument, like any other null value, so the row runs instead of silently producing
// no test. This matches a generic method with [Arguments(null)] (#7120). Same in both modes.

[EngineTest(ExpectedResult.Pass)]
public class NullRowOnGenericClass<T>
{
    [Test]
    [Arguments(null)]
    public async Task Run(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(object));
        await Assert.That(value).IsNull();
    }
}

// Beside a row that infers int, the null row runs on its own C<object> instantiation.
[EngineTest(ExpectedResult.Pass)]
public class NullAndValueRowsOnGenericClass<T>
{
    [Test]
    [Arguments(1)]
    [Arguments(null)]
    public async Task Run(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(value is null ? typeof(object) : typeof(int));
        await Assert.That((object?)value is null or 1).IsTrue();
    }
}

// object breaks the struct constraint, so the null row is reported as a failed test, like any other
// method row that infers a type the class's constraints reject (#7107), rather than being dropped.
// No [EngineTest] attribute: the failing row must not be picked up by the `/**[EngineTest=Pass]` run.
public class NullRowOnConstrainedGenericClass<T> where T : struct
{
    [Test]
    [Arguments(1)]
    [Arguments(null)]
    public async Task Run(T value)
    {
        await Assert.That(value).IsEqualTo((T)(object)1);
    }
}

// A [GenerateGenericTest] supplies the instantiation, so the null row infers nothing and runs on it,
// as before (#7106).
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(string))]
public class GenerateGenericTestWithNullRow<T>
{
    [Test]
    [Arguments(null)]
    public async Task Run(T? value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That(value).IsNull();
    }
}

// A by-reference parameter cannot take the row's value, so the null row infers nothing for it in
// either mode, and the generated code must still compile. No test is registered.
public class NullRowOnGenericClassByRef<T>
{
    [Test]
    [Arguments(null)]
    public void Run(ref T value)
    {
    }
}
