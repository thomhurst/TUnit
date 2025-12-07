using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Tests.Old;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
public class AssertMultipleTests
{
    [Test]
    public async Task MultipleFailures()
    {
        var assertionException = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            using (TUnitAssert.Multiple())
            {
                await TUnitAssert.That(1).IsEqualTo(2);
                await TUnitAssert.That(2).IsEqualTo(3);
                await TUnitAssert.That(3).IsEqualTo(4);
                await TUnitAssert.That(4).IsEqualTo(5);
                await TUnitAssert.That(5).IsEqualTo(6);
            }
        });

        var aggregateException = (AggregateException) assertionException!.InnerException!;

        var exception1 = (TUnitAssertionException) aggregateException.InnerExceptions[0];
        var exception2 = (TUnitAssertionException) aggregateException.InnerExceptions[1];
        var exception3 = (TUnitAssertionException) aggregateException.InnerExceptions[2];
        var exception4 = (TUnitAssertionException) aggregateException.InnerExceptions[3];
        var exception5 = (TUnitAssertionException) aggregateException.InnerExceptions[4];

        await TUnitAssert.That(exception1.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 2
                                                        but found 1

                                                        at Assert.That(1).IsEqualTo(2)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception2.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 3
                                                        but found 2

                                                        at Assert.That(2).IsEqualTo(3)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception3.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 4
                                                        but found 3

                                                        at Assert.That(3).IsEqualTo(4)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception4.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 5
                                                        but found 4

                                                        at Assert.That(4).IsEqualTo(5)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception5.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 6
                                                        but found 5

                                                        at Assert.That(5).IsEqualTo(6)
                                                        """.NormalizeLineEndings());
    }

    [Test]
    public async Task MultipleFailures_With_Connectors()
    {
        var assertionException = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            using (TUnitAssert.Multiple())
            {
                await TUnitAssert.That(1).IsEqualTo(2).Or.IsEqualTo(3);
                await TUnitAssert.That(2).IsEqualTo(3).And.IsEqualTo(4);
                await TUnitAssert.That(3).IsEqualTo(4).Or.IsEqualTo(5);
                await TUnitAssert.That(4).IsEqualTo(5).And.IsEqualTo(6);
                await TUnitAssert.That(5).IsEqualTo(6).Or.IsEqualTo(7);
            }
        });

        var aggregateException = (AggregateException) assertionException!.InnerException!;

        var exception1 = (TUnitAssertionException) aggregateException.InnerExceptions[0];
        var exception2 = (TUnitAssertionException) aggregateException.InnerExceptions[1];
        var exception3 = (TUnitAssertionException) aggregateException.InnerExceptions[2];
        var exception4 = (TUnitAssertionException) aggregateException.InnerExceptions[3];
        var exception5 = (TUnitAssertionException) aggregateException.InnerExceptions[4];

        await TUnitAssert.That(exception1.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 2
                                                        or to be 3
                                                        but found 1

                                                        at Assert.That(1).IsEqualTo(2).Or.IsEqualTo(3)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception2.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 3
                                                        and to be 4
                                                        but found 2

                                                        at Assert.That(2).IsEqualTo(3).And.IsEqualTo(4)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception3.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 4
                                                        or to be 5
                                                        but found 3

                                                        at Assert.That(3).IsEqualTo(4).Or.IsEqualTo(5)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception4.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 5
                                                        and to be 6
                                                        but found 4

                                                        at Assert.That(4).IsEqualTo(5).And.IsEqualTo(6)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(exception5.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 6
                                                        or to be 7
                                                        but found 5

                                                        at Assert.That(5).IsEqualTo(6).Or.IsEqualTo(7)
                                                        """.NormalizeLineEndings());
    }

    [Test]
    public async Task Nested_Multiples()
    {
        var assertionException = await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            using (TUnitAssert.Multiple())
            {
                await TUnitAssert.That(1).IsEqualTo(2);
                await TUnitAssert.That(2).IsEqualTo(3);
                await TUnitAssert.That(3).IsEqualTo(4);

                using (TUnitAssert.Multiple())
                {
                    await TUnitAssert.That(4).IsEqualTo(5);
                    await TUnitAssert.That(5).IsEqualTo(6);

                    using (TUnitAssert.Multiple())
                    {
                        await TUnitAssert.That(6).IsEqualTo(7);
                        await TUnitAssert.That(7).IsEqualTo(8);
                    }
                }
            }
        });

        var aggregateException = (AggregateException) assertionException!.InnerException!;

        await TUnitAssert.That((object)aggregateException.InnerExceptions[0]).IsTypeOf<TUnitAssertionException>();
        await TUnitAssert.That((object)aggregateException.InnerExceptions[1]).IsTypeOf<TUnitAssertionException>();
        await TUnitAssert.That((object)aggregateException.InnerExceptions[2]).IsTypeOf<TUnitAssertionException>();
        await TUnitAssert.That((object)aggregateException.InnerExceptions[3]).IsTypeOf<TUnitAssertionException>();
        await TUnitAssert.That((object)aggregateException.InnerExceptions[4]).IsTypeOf<TUnitAssertionException>();
        await TUnitAssert.That((object)aggregateException.InnerExceptions[5]).IsTypeOf<TUnitAssertionException>();
        await TUnitAssert.That((object)aggregateException.InnerExceptions[6]).IsTypeOf<TUnitAssertionException>();

        var assertionException1 = (TUnitAssertionException) aggregateException.InnerExceptions[0];
        var assertionException2 = (TUnitAssertionException) aggregateException.InnerExceptions[1];
        var assertionException3 = (TUnitAssertionException) aggregateException.InnerExceptions[2];
        var assertionException4 = (TUnitAssertionException) aggregateException.InnerExceptions[3];
        var assertionException5 = (TUnitAssertionException) aggregateException.InnerExceptions[4];
        var assertionException6 = (TUnitAssertionException) aggregateException.InnerExceptions[5];
        var assertionException7 = (TUnitAssertionException) aggregateException.InnerExceptions[6];

        await TUnitAssert.That(assertionException1.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 2
                                                        but found 1

                                                        at Assert.That(1).IsEqualTo(2)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(assertionException2.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 3
                                                        but found 2

                                                        at Assert.That(2).IsEqualTo(3)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(assertionException3.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 4
                                                        but found 3

                                                        at Assert.That(3).IsEqualTo(4)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(assertionException4.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 5
                                                        but found 4

                                                        at Assert.That(4).IsEqualTo(5)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(assertionException5.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 6
                                                        but found 5

                                                        at Assert.That(5).IsEqualTo(6)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(assertionException6.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 7
                                                        but found 6

                                                        at Assert.That(6).IsEqualTo(7)
                                                        """.NormalizeLineEndings());

        await TUnitAssert.That(assertionException7.Message.NormalizeLineEndings()).IsEqualTo("""
                                                        Expected to be 8
                                                        but found 7

                                                        at Assert.That(7).IsEqualTo(8)
                                                        """.NormalizeLineEndings());
    }
}
