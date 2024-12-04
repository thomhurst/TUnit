﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Tests.Assertions.Delegates;

namespace TUnit.Assertions.Tests;

[SuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
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
}