namespace TUnit.TestProject.Bugs._7624;

// The string row must fail constraint validation before invocation.
public class ConcreteParamsConstraintTests
{
    [Test]
    [Arguments(1, 7)]
    [Arguments("invalid", 7)]
    public async Task Run<T>(T first, params int[] extra) where T : struct
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(extra.SequenceEqual(new[] { 7 })).IsTrue();
    }
}
