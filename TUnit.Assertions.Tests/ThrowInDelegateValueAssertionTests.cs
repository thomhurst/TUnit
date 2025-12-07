namespace TUnit.Assertions.Tests;

public class ThrowInDelegateValueAssertionTests
{
    [Test]
    public async Task ThrowInDelegateValueAssertion_ReturnsExpectedErrorMessage()
    {
        var expectedContains = """
                         Expected to be equal to True
                         but threw System.Exception
                         """.NormalizeLineEndings();
        var assertion = async () => await Assert.That(() =>
        {
            throw new Exception("No");
            return true;
        }).IsEqualTo(true);

        var exception = await Assert.That(assertion)
            .Throws<AssertionException>();
        await Assert.That(exception.Message.NormalizeLineEndings()).Contains(expectedContains);
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
            .WithMessageContaining("should not contain \"error message\"");
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
            .WithMessageContaining("should not contain \"error message\"");
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
