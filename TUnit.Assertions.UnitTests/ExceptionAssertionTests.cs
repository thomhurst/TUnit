using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class ExceptionAssertionTests
{
    [Test]
    public async Task Assertion_Message_Has_Correct_doNotPopulateThisValue2()
    {
        await TUnitAssert.That(InnerExceptionThrower.Throw)
            .ThrowsException()
            .With.InnerException
            .With.InnerException
            .With.InnerException
            .With.InnerException
            .With.InnerException
            .With.Message.EqualTo("Message 6");
    }
}