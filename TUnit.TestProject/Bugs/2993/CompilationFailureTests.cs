using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._2993;

[EngineTest(ExpectedResult.Pass)]
internal class CompilationFailureTests
{
    private record TestItem(int? Value)
    {
        public static implicit operator TestItem(int? value) => new(value);
    }

    [Test]
    public async Task AssertEmptyIsEmpty()
    {
        var items = Enumerable.Empty<TestItem>();
        await Assert.That(items).IsEmpty();
    }

    [Test]
    public async Task TestImplicitConversion()
    {
        TestItem item1 = 42;
        TestItem item2 = null;
        
        await Assert.That(item1.Value).IsEqualTo(42);
        await Assert.That(item2.Value).IsNull();
    }
}