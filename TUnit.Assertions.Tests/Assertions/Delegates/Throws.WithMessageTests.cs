using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithMessageTests
    {
        [Test]
        public async Task Fails_For_Different_Messages()
        {
            var message1 = "foo";
            var message2 = "bar";
            var expectedMessage = """
                                  Expected action to throw a CustomException which message equals "bar"
                                  
                                  but it differs at index 0:
                                      ↓
                                     "foo"
                                     "bar"
                                      ↑
                                  
                                  at Assert.That(action).ThrowsExactly<CustomException>().WithMessage(message2)
                                  """;
            Exception exception = CreateCustomException(message1);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>().WithMessage(message2);

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public async Task Returns_Exception_When_Awaited()
        {
            var matchingMessage = "foo";
            Exception exception = CreateCustomException(matchingMessage);
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws<CustomException>().WithMessage(matchingMessage);

            await Assert.That(result).IsSameReferenceAs(exception);
        }

        [Test]
        public async Task Succeed_For_Matching_Message()
        {
            var matchingMessage = "foo";
            Exception exception = CreateCustomException(matchingMessage);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>().WithMessage(matchingMessage);

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
