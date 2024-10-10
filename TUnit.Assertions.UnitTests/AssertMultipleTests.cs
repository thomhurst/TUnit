using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

[SuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
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
                                                                Expected 1 to be equal to 2, but the received value 1 is different.
                                                                At Assert.That(1).IsEqualTo(2)
                                                                
                                                                Expected 2 to be equal to 3, but the received value 2 is different.
                                                                At Assert.That(2).IsEqualTo(3)
                                                                
                                                                Expected 3 to be equal to 4, but the received value 3 is different.
                                                                At Assert.That(3).IsEqualTo(4)
                                                                
                                                                Expected 4 to be equal to 5, but the received value 4 is different.
                                                                At Assert.That(4).IsEqualTo(5)
                                                                
                                                                Expected 5 to be equal to 6, but the received value 5 is different.
                                                                At Assert.That(5).IsEqualTo(6)
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
                                                                Expected 1 to be equal to 2
                                                                 or
                                                                to be equal to 3, but the received value 1 is different and the received value 1 is different.
                                                                At Assert.That(1).IsEqualTo(2).Or.IsEqualTo(3)
                                                                
                                                                Expected 2 to be equal to 3
                                                                 and
                                                                to be equal to 4, but the received value 2 is different and the received value 2 is different.
                                                                At Assert.That(2).IsEqualTo(3).And.IsEqualTo(4)
                                                                
                                                                Expected 3 to be equal to 4
                                                                 or
                                                                to be equal to 5, but the received value 3 is different and the received value 3 is different.
                                                                At Assert.That(3).IsEqualTo(4).Or.IsEqualTo(5)
                                                                
                                                                Expected 4 to be equal to 5
                                                                 and
                                                                to be equal to 6, but the received value 4 is different and the received value 4 is different.
                                                                At Assert.That(4).IsEqualTo(5).And.IsEqualTo(6)
                                                                
                                                                Expected 5 to be equal to 6
                                                                 or
                                                                to be equal to 7, but the received value 5 is different and the received value 5 is different.
                                                                At Assert.That(5).IsEqualTo(6).Or.IsEqualTo(7)
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
                                                                 Expected 1 to be equal to 2, but the received value 1 is different.
                                                                 At Assert.That(1).IsEqualTo(2)
                                                                 
                                                                 Expected 2 to be equal to 3, but the received value 2 is different.
                                                                 At Assert.That(2).IsEqualTo(3)
                                                                 
                                                                 Expected 3 to be equal to 4, but the received value 3 is different.
                                                                 At Assert.That(3).IsEqualTo(4)
                                                                 """));
        
        NUnitAssert.That(assertionException2.Message, Is.EqualTo("""
                                                                 Expected 4 to be equal to 5, but the received value 4 is different.
                                                                 At Assert.That(4).IsEqualTo(5)
                                                                 
                                                                 Expected 5 to be equal to 6, but the received value 5 is different.
                                                                 At Assert.That(5).IsEqualTo(6)
                                                                 """));
        
        NUnitAssert.That(assertionException3.Message, Is.EqualTo("""
                                                                 Expected 6 to be equal to 7, but the received value 6 is different.
                                                                 At Assert.That(6).IsEqualTo(7)
                                                                 
                                                                 Expected 7 to be equal to 8, but the received value 7 is different.
                                                                 At Assert.That(7).IsEqualTo(8)
                                                                 """));
    }
}