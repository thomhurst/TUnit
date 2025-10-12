namespace TUnit.Assertions.Tests;

public class AssertionGroupTests
{
    [Test]
    public async Task Or_Conditions_With_Delegates()
    {
        // Test: "CD" should contain (C AND D) OR (A AND B)
        // This passes because it contains C AND D
        var value = "CD";

        // Try first assertion, if it fails try second
        try
        {
            await Assert.That(value).Contains('C').And.Contains('D');
        }
        catch (AssertionException)
        {
            await Assert.That(value).Contains('A').And.Contains('B');
        }
    }

    [Test]
    public async Task Simple_And_Chaining()
    {
        var value = "Foo";

        await Assert.That(value)
            .IsNotNull()
            .And
            .IsEqualTo("Foo");
    }

    // IsNullOrEmpty is not available in current API
    // These tests are commented out as they test deprecated functionality

    //[Test]
    //public async Task Complex_Or_With_Delegates()
    //{
    //    // Test: "Foo" should match (IsNullOrEmpty AND EqualTo("Foo")) OR (IsNullOrEmpty OR EqualTo("Foo"))
    //    // Second condition passes because EqualTo("Foo") is true
    //    var value = "Foo";

    //    // Try first assertion, if it fails try second
    //    try
    //    {
    //        await Assert.That(value).IsNullOrEmpty().And.IsEqualTo("Foo");
    //    }
    //    catch (AssertionException)
    //    {
    //        await Assert.That(value).IsNullOrEmpty().Or.IsEqualTo("Foo");
    //    }
    //}

    //[Test]
    //public async Task And_Condition_Throws_As_Expected()
    //{
    //    var value = "Foo";

    //    await Assert.That(async () =>
    //            await Assert.That(value).IsNullOrEmpty().And.IsEqualTo("Foo")
    //        ).Throws<AssertionException>()
    //        .And
    //        .HasMessageContaining("to be null or empty")
    //        .And
    //        .HasMessageContaining("Foo");
    //}
}
