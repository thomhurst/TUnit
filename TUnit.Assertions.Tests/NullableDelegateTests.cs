using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class NullableDelegateTests
{
    [Test]
    public async Task IsNotNull_WithNonNullFunc_ShouldPass()
    {
        // Arrange
        Func<int>? func = () => 42;

        // Act & Assert
        await Assert.That(func).IsNotNull();
    }

    [Test]
    public async Task IsNotNull_WithNullFunc_ShouldFail()
    {
        // Arrange
        Func<int>? func = null;

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(func).IsNotNull();
        });
    }

    [Test]
    public async Task IsNotNull_WithNonNullAction_ShouldPass()
    {
        // Arrange
        Action? action = () => { };

        // Act & Assert
        await Assert.That(action).IsNotNull();
    }

    [Test]
    public async Task IsNotNull_WithNullAction_ShouldFail()
    {
        // Arrange
        Action? action = null;

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(action).IsNotNull();
        });
    }

    [Test]
    public async Task IsNotNull_WithFuncString_ShouldPass()
    {
        // Arrange
        Func<string>? func = () => "test";

        // Act & Assert
        await Assert.That(func).IsNotNull();
    }

    [Test]
    public async Task IsNotNull_WithNullFuncString_ShouldFail()
    {
        // Arrange
        Func<string>? func = null;

        // Act & Assert
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(func).IsNotNull();
        });
    }
}