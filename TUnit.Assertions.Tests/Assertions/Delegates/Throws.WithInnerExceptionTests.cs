namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithInnerExceptionTests
    {
        [Test]
        public async Task Fails_For_Different_Messages_In_Inner_Exception()
        {
            string outerMessage = "foo";
            string expectedInnerMessage = "bar";
            string expectedMessage = """
                Expected action to throw an Exception which message equals "bar", but it differs at index 0:
                    ↓
                   "some different inner message"
                   "bar"
                    ↑.
                At Assert.That(action).Throws().Exception().WithInnerException().WithMessage(expectedInnerMessage)
                """;
            Exception exception = CreateCustomException(outerMessage,
                CreateCustomException("some different inner message"));
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exception()
                .WithInnerException().WithMessage(expectedInnerMessage);

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException(
                innerException: CreateCustomException());
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws().OfType<CustomException>().WithInnerException();

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeed_For_Matching_Message()
        {
            string outerMessage = "foo";
            string innerMessage = "bar";
            Exception exception = CreateCustomException(outerMessage, CreateCustomException(innerMessage));
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exception()
                .WithInnerException().WithMessage(innerMessage);

            await Assert.That(sut).Throws().Nothing();
        }
    }
}
