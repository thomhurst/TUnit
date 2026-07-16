namespace TUnit.Assertions.Tests.Bugs;

public class Tests1600
{
    [Test]
    public async Task Default_Comparer()
    {
        MyModel[] array1 = [new(), new(), new()];
        MyModel[] array2 = [new(), new(), new()];

        await Assert.That(async () =>
            await Assert.That(array1).IsEquivalentTo(array2)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task Custom_Comparer()
    {
        MyModel[] array1 = [new(), new(), new()];
        MyModel[] array2 = [new(), new(), new()];

        await Assert.That(array1).IsEquivalentTo(array2).Using(new MyModelComparer());
    }

    [Test]
    public async Task Custom_Predicate()
    {
        MyModel[] array1 = [new(), new(), new()];
        MyModel[] array2 = [new(), new(), new()];

        // Using a lambda predicate instead of implementing IEqualityComparer
        await Assert.That(array1).IsEquivalentTo(array2).Using((x, y) => true);
    }

    [Test]
    public async Task Custom_Predicate_With_Property_Comparison()
    {
        var users1 = new[] { new User("Alice", 30), new User("Bob", 25) };
        var users2 = new[] { new User("Bob", 25), new User("Alice", 30) };

        // Elements have different order but are equivalent by name and age
        await Assert.That(users1)
            .IsEquivalentTo(users2)
            .Using((u1, u2) => u1?.Name == u2?.Name && u1?.Age == u2?.Age);
    }

    [Test]
    public async Task Custom_Predicate_Not_Equivalent()
    {
        var users1 = new[] { new User("Alice", 30), new User("Bob", 25) };
        var users2 = new[] { new User("Charlie", 35), new User("Diana", 28) };

        await Assert.That(users1)
            .IsNotEquivalentTo(users2)
            .Using((u1, u2) => u1?.Name == u2?.Name && u1?.Age == u2?.Age);
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

    public record User(string Name, int Age);
}
