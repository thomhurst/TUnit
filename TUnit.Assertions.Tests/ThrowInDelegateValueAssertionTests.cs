using TUnit.Assertions.AssertConditions.Throws;

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
}
