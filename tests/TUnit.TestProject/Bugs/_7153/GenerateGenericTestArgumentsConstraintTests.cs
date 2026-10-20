#pragma warning disable TUnit0001 // The invalid [Arguments] rows are the point of these tests

namespace TUnit.TestProject.Bugs._7153;

// A generic method with a method-level [GenerateGenericTest] and an [Arguments] row whose inferred
// type argument breaks the method's constraints: the row is reported as a failed test in both modes,
// not silently dropped in reflection mode (#7153). Valid rows keep running.
//
// These classes carry no [EngineTest] attribute: TUnit.Engine.Tests targets them by name.
public class GenerateGenericTestArgumentsConstraintTests
{
    // "x" infers string, which breaks `where T : struct`
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments("x")]
    [Arguments(5)]
    public async Task Run<T>(T value) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
    }

    // Every invalid row is its own failed test
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(5)]
    [Arguments("x")]
    [Arguments("y")]
    public async Task TwoInvalidRows<T>(T value) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
    }

    // The [GenerateGenericTest] type also breaks the constraint: the row and the type each fail once
    [Test]
    [GenerateGenericTest(typeof(string))]
    [Arguments("x")]
    public async Task InvalidRowAndInvalidType<T>(T value) where T : struct
    {
        await Task.CompletedTask;
    }
}
