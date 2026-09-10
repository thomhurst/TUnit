namespace TUnit.Assertions.Tests.Bugs;

public class Issue6581Tests
{
    [Test]
    public async Task Member_After_HasSingleItem_Item_Succeeds()
    {
        var items = new List<TestObject>
        {
            new() { Value = 4 }
        };

        await Assert
            .That(items).HasSingleItem()
            .Item.Member(item => item.Value, value => value.IsEqualTo(4));
    }

    [Test]
    public async Task Member_After_HasSingleItem_With_Predicate_Item_Succeeds()
    {
        var items = new List<TestObject>
        {
            new() { Value = 3 },
            new() { Value = 4 }
        };

        await Assert
            .That(items).HasSingleItem(item => item.Value == 4)
            .Item.Member(item => item.Value, value => value.IsEqualTo(4));
    }

    private sealed class TestObject
    {
        public int Value { get; init; }
    }
}
