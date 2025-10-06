using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests1877
{
    [Test]
    public async Task Test()
    {
        IEnumerable<MyRecord> x = [new("A"), new("B"), new("C")];
        IEnumerable<MyRecord> y = [new("C"), new("B"), new("A")];

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
