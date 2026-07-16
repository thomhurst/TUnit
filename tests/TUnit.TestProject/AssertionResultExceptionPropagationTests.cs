using TUnit.Assertions.Exceptions;

namespace TUnit.TestProject;

public class AssertionResultExceptionPropagationTests
{
    [Test]
    public async Task Nested_Collection_Assertion_Failure_Preserves_InnerException()
    {
        var items = new[] { 1, 2, 3 };

        AssertionException? caught = null;
        try
        {
            await Assert.That(items).All().Satisfy(x => x.IsEqualTo(99));
        }
        catch (AssertionException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.InnerException).IsNotNull();
        await Assert.That(caught.InnerException).IsTypeOf<AssertionException>();
    }

    [Test]
    public async Task Mapped_Assertion_Failure_Preserves_InnerException()
    {
        var value = "hello";

        AssertionException? caught = null;
        try
        {
            await Assert.That(value).Satisfies(v => v.Length, length => length.IsEqualTo(99));
        }
        catch (AssertionException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.InnerException).IsNotNull();
        await Assert.That(caught.InnerException).IsTypeOf<AssertionException>();
    }
}
