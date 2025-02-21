using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests1877
{
    [Test]
    public async Task Test()
    {
        IEnumerable<MyRecord> x = [new MyRecord("A"), new MyRecord("B"), new MyRecord("C")];
        IEnumerable<MyRecord> y = [new MyRecord("C"), new MyRecord("B"), new MyRecord("A")];

        await Assert.That(x).IsEquivalentTo(y, CollectionOrdering.Any);
    }

    [Test]
    public async Task Test2()
    {
        IEnumerable<string> x = ["A", "B", "C"];
        IEnumerable<string> y = ["C", "B", "A"];

        await Assert.That(x).IsEquivalentTo(y, CollectionOrdering.Any);
    }
    
    public record MyRecord(string Value);
}