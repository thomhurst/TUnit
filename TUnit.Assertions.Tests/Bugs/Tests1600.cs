using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests1600
{
    [Test]
    public async Task Default_Comparer()
    {
        MyModel[] array1 = [ new(), new(), new() ];
        MyModel[] array2 = [ new(), new(), new() ];

        await Assert.That(async () =>
            await Assert.That(array1).IsEquivalentTo(array2)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Custom_Comparer()
    {
        MyModel[] array1 = [ new(), new(), new() ];
        MyModel[] array2 = [ new(), new(), new() ];

        await Assert.That(array1).IsEquivalentTo(array2, new MyModelComparer());
    }

    public class MyModel
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }

    public class MyModelComparer : IEqualityComparer<MyModel>
    {
        public bool Equals(MyModel? x, MyModel? y)
        {
            return true;
        }

        public int GetHashCode(MyModel obj)
        {
            return 1;
        }
    }
}