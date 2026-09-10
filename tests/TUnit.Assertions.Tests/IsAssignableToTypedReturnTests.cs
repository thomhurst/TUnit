using System.Text;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests for GitHub issue #6184: <c>IsAssignableTo&lt;T&gt;()</c> should return the value
/// cast to <c>T</c> (parity with <c>IsTypeOf&lt;T&gt;()</c>), so the caller doesn't need a
/// second manual cast.
/// </summary>
public class IsAssignableToTypedReturnTests
{
    private interface IElement { }
    private class Element : IElement { }
    private class DerivedElement : Element { }

    [Test]
    public async Task IsAssignableTo_ReturnsValueCastToInterface()
    {
        // The exact scenario from the issue: an object/base-typed value that we want back
        // as the interface it implements.
        object obj = new List<string> { "a", "b", "c" };

        IEnumerable<string> result = await Assert.That(obj).IsAssignableTo<IEnumerable<string>>();

        await Assert.That(result.Count()).IsEqualTo(3);
        await Assert.That(result.First()).IsEqualTo("a");
    }

    [Test]
    public async Task IsAssignableTo_ReturnsValueCastToBaseType()
    {
        DerivedElement derived = new DerivedElement();

        Element result = (await Assert.That(derived).IsAssignableTo<Element>())!;

        await Assert.That(result).IsSameReferenceAs(derived);
    }

    [Test]
    public async Task IsAssignableTo_ExactType_ReturnsValue()
    {
        var sb = new StringBuilder("Test");
        object obj = sb;

        StringBuilder result = (await Assert.That(obj).IsAssignableTo<StringBuilder>())!;

        await Assert.That(result.ToString()).IsEqualTo("Test");
    }

    [Test]
    public async Task IsAssignableTo_NotAssignable_StillFails()
    {
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            Element element = new Element();
            await Assert.That(element).IsAssignableTo<DerivedElement>();
        });
    }

    [Test]
    public async Task IsAssignableTo_Null_StillFails()
    {
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            object? obj = null;
            await Assert.That(obj).IsAssignableTo<IElement>();
        });
    }

    [Test]
    public async Task IsAssignableTo_ChainedAfterThrows_ChecksAndReturnsExceptionType()
    {
        // Dual-mode: when chained after Throws, the asserted "value" is the thrown
        // exception, so IsAssignableTo validates the exception's type and returns it cast.
        var result = await Assert.That(async () =>
            {
                throw new InvalidOperationException("boom");
            })
            .Throws<InvalidOperationException>()
            .And
            .IsAssignableTo<Exception>();

        await Assert.That(result!.Message).IsEqualTo("boom");
    }

    [Test]
    public async Task IsAssignableTo_ChainedAfterThrows_Fails_WhenExceptionNotAssignable()
    {
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(async () =>
                {
                    throw new InvalidOperationException("boom");
                })
                .Throws<InvalidOperationException>()
                .And
                .IsAssignableTo<ArgumentException>();
        });
    }

    [Test]
    public async Task IsAssignableTo_EvaluatesSourceOnlyOnce()
    {
        // Regression guard for the dual-context design: CheckAsync reads the original
        // context while the base reads the mapped context; both must resolve to a single
        // cached evaluation of the source delegate.
        var invocations = 0;

        await Assert.That(() =>
        {
            invocations++;
            return (object)new List<string> { "a" };
        }).IsAssignableTo<IEnumerable<string>>();

        await Assert.That(invocations).IsEqualTo(1);
    }
}
