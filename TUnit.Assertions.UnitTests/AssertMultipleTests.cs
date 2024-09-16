using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

[SuppressMessage("Usage", "TUnitAssertions0005:AssertAsync.That(...) should not be used with a constant value")]
public class AssertMultipleTests
{
    [Test]
    public void MultipleFailures()
    {
        var assertionException = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            await using (TUnitAssert.Multiple())
            {
                await TUnitAssert.That(1).IsEqualTo(2);
                await TUnitAssert.That(2).IsEqualTo(3);
                await TUnitAssert.That(3).IsEqualTo(4);
                await TUnitAssert.That(4).IsEqualTo(5);
                await TUnitAssert.That(5).IsEqualTo(6);
            }
        });

        NUnitAssert.That(assertionException!.Message, Is.EqualTo("""
                                                                AssertAsync.That(1).IsEqualTo(2)
                                                                Expected: 2
                                                                Received: 1
                                                                
                                                                AssertAsync.That(2).IsEqualTo(3)
                                                                Expected: 3
                                                                Received: 2
                                                                
                                                                AssertAsync.That(3).IsEqualTo(4)
                                                                Expected: 4
                                                                Received: 3
                                                                
                                                                AssertAsync.That(4).IsEqualTo(5)
                                                                Expected: 5
                                                                Received: 4
                                                                
                                                                AssertAsync.That(5).IsEqualTo(6)
                                                                Expected: 6
                                                                Received: 5
                                                                """));
    }
    
    [Test]
    public void MultipleFailures_With_Connectors()
    {
        var assertionException = NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () =>
        {
            await using (TUnitAssert.Multiple())
            {
                await TUnitAssert.That(1).IsEqualTo(2).Or.IsEqualTo(3);
                await TUnitAssert.That(2).IsEqualTo(3).And.IsEqualTo(4);
                await TUnitAssert.That(3).IsEqualTo(4).Or.IsEqualTo(5);
                await TUnitAssert.That(4).IsEqualTo(5).And.IsEqualTo(6);
                await TUnitAssert.That(5).IsEqualTo(6).Or.IsEqualTo(7);
            }
        });

        NUnitAssert.That(assertionException!.Message, Is.EqualTo("""
                                                                AssertAsync.That(1).IsEqualTo(2).Or.IsEqualTo(3)
                                                                Expected: 2
                                                                Received: 1
                                                                 &
                                                                Expected: 3
                                                                Received: 1
                                                                
                                                                AssertAsync.That(2).IsEqualTo(3).And.IsEqualTo(4)
                                                                Expected: 3
                                                                Received: 2
                                                                
                                                                AssertAsync.That(2).IsEqualTo(3).And.IsEqualTo(4)
                                                                Expected: 4
                                                                Received: 2
                                                                
                                                                AssertAsync.That(3).IsEqualTo(4).Or.IsEqualTo(5)
                                                                Expected: 4
                                                                Received: 3
                                                                 &
                                                                Expected: 5
                                                                Received: 3
                                                                
                                                                AssertAsync.That(4).IsEqualTo(5).And.IsEqualTo(6)
                                                                Expected: 5
                                                                Received: 4
                                                                
                                                                AssertAsync.That(4).IsEqualTo(5).And.IsEqualTo(6)
                                                                Expected: 6
                                                                Received: 4
                                                                
                                                                AssertAsync.That(5).IsEqualTo(6).Or.IsEqualTo(7)
                                                                Expected: 6
                                                                Received: 5
                                                                 &
                                                                Expected: 7
                                                                Received: 5
                                                                """));
    }
    
      [Test]
    public void Nested_Multiples()
    {
        var aggregateException = NUnitAssert.ThrowsAsync<AggregateException>(async () =>
        {
            await using (TUnitAssert.Multiple())
            {
                await TUnitAssert.That(1).IsEqualTo(2);
                await TUnitAssert.That(2).IsEqualTo(3);
                await TUnitAssert.That(3).IsEqualTo(4);

                await using (TUnitAssert.Multiple())
                {
                    await TUnitAssert.That(4).IsEqualTo(5);
                    await TUnitAssert.That(5).IsEqualTo(6);
                    
                    await using (TUnitAssert.Multiple())
                    {
                        await TUnitAssert.That(6).IsEqualTo(7);
                        await TUnitAssert.That(7).IsEqualTo(8);
                    }
                }
            }
        });
        
        NUnitAssert.That(aggregateException!.InnerExceptions[0], Is.TypeOf<TUnitAssertionException>());
        NUnitAssert.That(aggregateException.InnerExceptions[1], Is.TypeOf<TUnitAssertionException>());
        NUnitAssert.That(aggregateException.InnerExceptions[2], Is.TypeOf<TUnitAssertionException>());

        var assertionException1 = (TUnitAssertionException)aggregateException.InnerExceptions[0];
        var assertionException2 = (TUnitAssertionException)aggregateException.InnerExceptions[1];
        var assertionException3 = (TUnitAssertionException)aggregateException.InnerExceptions[2];
        
        NUnitAssert.That(assertionException1.Message, Is.EqualTo("""
                                                                 AssertAsync.That(1).IsEqualTo(2)
                                                                 Expected: 2
                                                                 Received: 1

                                                                 AssertAsync.That(2).IsEqualTo(3)
                                                                 Expected: 3
                                                                 Received: 2

                                                                 AssertAsync.That(3).IsEqualTo(4)
                                                                 Expected: 4
                                                                 Received: 3
                                                                 """));
        
        NUnitAssert.That(assertionException2.Message, Is.EqualTo("""
                                                                 AssertAsync.That(4).IsEqualTo(5)
                                                                 Expected: 5
                                                                 Received: 4

                                                                 AssertAsync.That(5).IsEqualTo(6)
                                                                 Expected: 6
                                                                 Received: 5
                                                                 """));
        
        NUnitAssert.That(assertionException3.Message, Is.EqualTo("""
                                                                 AssertAsync.That(6).IsEqualTo(7)
                                                                 Expected: 7
                                                                 Received: 6

                                                                 AssertAsync.That(7).IsEqualTo(8)
                                                                 Expected: 8
                                                                 Received: 7
                                                                 """));
    }
}