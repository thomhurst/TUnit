using TUnit.Assertions.Extensions.Generic;

namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithMessageTests
    {
        [Test]
        public async Task Fails_For_Different_Messages()
        {
            string message1 = "foo";
            string message2 = "bar";
            string expectedMessage = """
                Expected action to throw a CustomException which message equals "bar", but it differs at index 0:
                    ↓
                   "foo"
                   "bar"
                    ↑.
                At Assert.That(action).Throws().Exactly<CustomException>().WithMessage(message2)
                """;
            Exception exception = CustomException.Create(message1);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>().WithMessage(message2);

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            string matchingMessage = "foo";
            Exception exception = CustomException.Create(matchingMessage);
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws().OfType<CustomException>().WithMessage(matchingMessage);

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeed_For_Matching_Message()
        {
            string matchingMessage = "foo";
            Exception exception = CustomException.Create(matchingMessage);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().OfType<CustomException>().WithMessage(matchingMessage);

            await Assert.That(sut).Throws().Nothing();
        }
    }
}
