namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Tests for issue #3489: IsEquivalentTo does not work properly with nested collections in records
/// </summary>
public class Tests3489
{
    [Test]
    public async Task IsEquivalentTo_WithNestedCollections_InRecords_ShouldSucceed()
    {
        // Arrange - Create identical hierarchical structures
        var actual = new List<Person>
        {
            new()
            {
                Name = "Parent1",
                Children =
                [
                    new Person { Name = "Child1", Children = [] },
                    new Person { Name = "Child2", Children = [] }
                ]
            },
            new()
            {
                Name = "Parent2",
                Children = [new Person { Name = "Child3", Children = [] }]
            }
        };

        var expected = new List<Person>
        {
            new()
            {
                Name = "Parent1",
                Children =
                [
                    new Person { Name = "Child1", Children = [] },
                    new Person { Name = "Child2", Children = [] }
                ]
            },
            new()
            {
                Name = "Parent2",
                Children = [new Person { Name = "Child3", Children = [] }]
            }
        };

        // Act & Assert - Should recognize structural equivalence despite different List instances
        await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task IsEquivalentTo_WithNestedCollections_InRecords_DifferentData_ShouldFail()
    {
        // Arrange
        var actual = new List<Person>
        {
            new()
            {
                Name = "Parent1",
                Children = [new Person { Name = "Child1", Children = [] }]
            }
        };

        var expected = new List<Person>
        {
            new()
            {
                Name = "Parent1",
                Children = [new Person { Name = "DifferentChild", Children = [] }]
            }
        };

        // Act & Assert - Should fail when actual data is different
        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEquivalentTo(expected));

        await Assert.That(exception).IsNotNull();
    }

    private sealed record Person
    {
        public required string Name { get; init; }
        public required List<Person> Children { get; init; }
    }
}
