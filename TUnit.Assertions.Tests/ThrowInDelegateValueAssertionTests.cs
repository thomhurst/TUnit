namespace TUnit.Assertions.Tests;

public class ThrowInDelegateValueAssertionTests
{
    [Test]
    public async Task ThrowInDelegateValueAssertion_ReturnsExpectedErrorMessage()
    {
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("No");
            return true;
        }).IsEqualTo(true);

        await Assert.That(assertion)
            .Throws<AssertionException>()
            .WithMessageContaining("""
                         Expected () =>
                                 {
                                     throw new Exception("No");
                                     return true;
                                 } to be equal to True

                         but An exception was thrown during the assertion: System.Exception: No
                         """);
    }

    [Test]
    public async Task ThrowInDelegateValueAssertion_RespectsCaseInsensitiveMessage()
    {
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("No");
            return true;
        }).IsEqualTo(true);

        await Assert.That(assertion)
            .Throws<AssertionException>()
            .WithMessageContaining("SYSTEM.EXCEPTION", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task ThrowInDelegateValueAssertion_WithMessageNotContaining_Passes_WhenMessageDoesNotContainText()
    {
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("This is an error message");
            return true;
        }).IsEqualTo(true);

        await Assert.That(assertion)
            .Throws<AssertionException>()
            .WithMessageNotContaining("different text");
    }

    [Test]
    public async Task ThrowInDelegateValueAssertion_WithMessageNotContaining_Fails_WhenMessageContainsText()
    {
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("This is an error message");
            return true;
        }).IsEqualTo(true);

        var finalAssertion = async () => await Assert.That(assertion)
            .Throws<AssertionException>()
            .WithMessageNotContaining("error message");

        await Assert.That(finalAssertion)
            .Throws<AssertionException>()
            .WithMessageContaining("which message does not contain \"error message\"");
    }

    [Test]
    public async Task ThrowInDelegateValueAssertion_WithMessageNotContaining_RespectsCaseInsensitive()
    {
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("This is an ERROR message");
            return true;
        }).IsEqualTo(true);

        var finalAssertion = async () => await Assert.That(assertion)
            .Throws<AssertionException>()
            .WithMessageNotContaining("error message", StringComparison.OrdinalIgnoreCase);

        await Assert.That(finalAssertion)
            .Throws<AssertionException>()
            .WithMessageContaining("which message does not contain \"error message\"");
    }

    [Test]
    public async Task ThrowInDelegateValueAssertion_WithMessageNotContaining_RespectsCaseSensitive()
    {
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("This is an ERROR message");
            return true;
        }).IsEqualTo(true);

        await Assert.That(assertion)
            .Throws<AssertionException>()
            .WithMessageNotContaining("error message", StringComparison.Ordinal);
    }
}
