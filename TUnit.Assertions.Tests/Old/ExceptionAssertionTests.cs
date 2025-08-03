using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Old;

public class ExceptionAssertionTests
{
    [Test]
    public async Task Assertion_Message_Has_Correct_doNotPopulateThisValue2()
    {
        await TUnitAssert.That(InnerExceptionThrower.Throw)
            .ThrowsException()
            .WithInnerException()
            .WithInnerException()
            .WithInnerException()
            .WithInnerException()
            .WithInnerException()
            .WithMessage("Message 6");
    }
}
