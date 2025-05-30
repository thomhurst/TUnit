using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests;

[UnconditionalSuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
public class AssertMultipleTests
{
    [Test]
    public async Task Exception_In_Scope_Is_Captured()
    {
        await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(1).IsEqualTo(2);
                await Assert.That(2).IsEqualTo(4);

                if (1.ToString() == "1")
                {
                    throw new Exception("Hello World");
                }

                await Assert.That(3).IsEqualTo(6);
            }
        }).Throws<Exception>().And.HasMessageContaining("Hello World");
    }
    
    [Test]
    public async Task Caught_Exception_In_Scope_Is_Not_Captured()
    {
        var exception = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(1).IsEqualTo(2);
                await Assert.That(2).IsEqualTo(4);

                if (1.ToString() == "1")
                {
                    try
                    {
                        throw new Exception("Hello World");
                    }
                    catch
                    {
                        // Ignored
                    }
                }

                await Assert.That(3).IsEqualTo(6);
            }
        }).Throws<Exception>();

        await Assert.That(exception!.Message)
            .Contains("(This exception may or may not have been caught) System.Exception: Hello World");
    }

    [Test]
    public async Task CanAssertThrowsException_Within_AssertMultiple()
    {
        using (Assert.Multiple())
        {
            await Assert.That(
                static () => throw new InvalidOperationException()
            ).ThrowsExactly<InvalidOperationException>();
            
            await Assert.That(
                static () => throw new InvalidOperationException()
            ).ThrowsExactly<InvalidOperationException>();
        }
    }
    
    [Test]
    public async Task Assert_Fail_Doesnt_Throw_Immediately_Within_AssertMultiple()
    {
        var endReached = false;

        await Assert.That(() =>
            {
                using (Assert.Multiple())
                {
                    Assert.Fail("Error 1");
                    Assert.Fail("Error 2");

                    endReached = true;
                }
            })
            .ThrowsException()
            .WithMessageContaining("Error 1")
            .And
            .HasMessageContaining("Error 2");
        
        await Assert.That(endReached).IsTrue();
    }
}