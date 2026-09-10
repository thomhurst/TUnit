namespace TUnit.TestProject.Bugs.Issue2993;

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
    
    // Also test with non-nullable int to verify both work
    private record TestItem2(int Value)
    {
        public static implicit operator TestItem2(int value) => new(value);
    }
    
    [Test]
    public async Task AssertEmptyIsEmpty_NonNullable()
    {
        var items = Enumerable.Empty<TestItem2>();
        await Assert.That(items).IsEmpty();
    }
}