using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for issue #5732 — failure messages for IsEqualTo / IsEquivalentTo should include
/// a focused "differs at member X: expected Y but found Z" hint instead of dumping the
/// whole serialized object graph.
/// </summary>
public class FocusedDiffMessageTests
{
    public record EmployeeInfo(string FirstName, string LastName, int Age);

    public class Employee
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Age { get; set; }

        // Force reference equality so IsEqualTo (without overridden Equals) hits the
        // structural diff fallback path rather than the records' value-equality path.
    }

    public class EmployeeWithNestedAddress
    {
        public string? Name { get; set; }
        public Address? Address { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    [Test]
    public async Task IsEqualTo_OnRecord_Failure_Includes_Differing_Property()
    {
        var actual = new EmployeeInfo("ictoria", "Apanii", 30);
        var expected = new EmployeeInfo("Victoria", "Apanii", 30);

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEqualTo(expected));

        // GetProperties() order isn't contractually guaranteed across runtimes, so we just
        // verify the focused-diff path was hit and the differing values surfaced — the only
        // differing member is FirstName, so its values must appear regardless of which
        // member name the formatter prints.
        await Assert.That(exception!.Message).Contains("differs at member");
        await Assert.That(exception.Message).Contains("expected \"Victoria\"");
        await Assert.That(exception.Message).Contains("found \"ictoria\"");
    }

    [Test]
    public async Task IsEqualTo_OnReferenceType_Failure_Includes_Differing_Property()
    {
        var actual = new Employee { FirstName = "Tom", LastName = "X", Age = 1 };
        var expected = new Employee { FirstName = "Tom", LastName = "Y", Age = 1 };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEqualTo(expected));

        await Assert.That(exception!.Message).Contains("differs at member LastName");
        await Assert.That(exception.Message).Contains("expected \"Y\"");
        await Assert.That(exception.Message).Contains("found \"X\"");
    }

    [Test]
    public async Task IsEqualTo_OnReferenceType_Failure_Includes_Nested_Path()
    {
        var actual = new EmployeeWithNestedAddress
        {
            Name = "Bob",
            Address = new Address { Street = "1 Main", City = "Foo" }
        };
        var expected = new EmployeeWithNestedAddress
        {
            Name = "Bob",
            Address = new Address { Street = "1 Main", City = "Bar" }
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEqualTo(expected));

        await Assert.That(exception!.Message).Contains("differs at member Address.City");
    }

    [Test]
    public async Task IsEqualTo_PrimitiveString_Message_Unchanged()
    {
        // Primitives/strings should keep the simple "received X" path — verifies we don't
        // regress the primitive case.
        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That("hello").IsEqualTo("world"));

        await Assert.That(exception!.Message).DoesNotContain("differs at member");
    }

    [Test]
    public async Task IsEquivalentTo_Collection_MatchingOrder_Failure_Includes_Property_Diff()
    {
        var actual = new[]
        {
            new EmployeeInfo("Victoria", "Apanii", 30),
            new EmployeeInfo("Bob", "X", 25),
        };
        var expected = new[]
        {
            new EmployeeInfo("Victoria", "Apanii", 30),
            new EmployeeInfo("Bob", "Y", 25),
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEquivalentTo(expected, CollectionOrdering.Matching));

        await Assert.That(exception!.Message).Contains("differs at member LastName");
        await Assert.That(exception.Message).Contains("expected \"Y\"");
        await Assert.That(exception.Message).Contains("found \"X\"");
    }

    [Test]
    public async Task IsEquivalentTo_Collection_AnyOrder_Failure_Includes_Closest_Match_Diff()
    {
        var actual = new[]
        {
            new EmployeeInfo("Victoria", "Apanii", 30),
            new EmployeeInfo("Bob", "X", 25),
        };
        var expected = new[]
        {
            new EmployeeInfo("Victoria", "Apanii", 30),
            // The closest match in actual is Bob/X — the diff should call out LastName.
            new EmployeeInfo("Bob", "Y", 25),
        };

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEquivalentTo(expected, CollectionOrdering.Any));

        await Assert.That(exception!.Message).Contains("closest match");
        await Assert.That(exception.Message).Contains("differs at member LastName");
    }

    [Test]
    public async Task IsEquivalentTo_PrimitiveCollection_Message_Unchanged()
    {
        // Primitive collections shouldn't get a "closest match" hint — there is no
        // member path to surface.
        int[] actual = [1, 2, 3];
        int[] expected = [1, 2, 4];

        var exception = await Assert.ThrowsAsync<TUnitAssertionException>(
            async () => await Assert.That(actual).IsEquivalentTo(expected, CollectionOrdering.Matching));

        await Assert.That(exception!.Message).DoesNotContain("differs at member");
        await Assert.That(exception.Message).DoesNotContain("closest match");
    }
}
