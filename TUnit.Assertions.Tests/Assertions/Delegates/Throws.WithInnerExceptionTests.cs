using System.Diagnostics.CodeAnalysis;
namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithInnerExceptionTests
    {
        [Test]
        public async Task Fails_For_Different_Messages_In_Inner_Exception()
        {
            var outerMessage = "foo";
            var expectedInnerMessage = "bar";
            var expectedMessage = """
                                  Expected exception message to equal "bar"
                                  but exception message was "some different inner message"

                                  at Assert.That(action).ThrowsException().WithInnerException().WithMessage("bar")
                                  """;
            Exception exception = CreateCustomException(outerMessage,
                CreateCustomException("some different inner message"));
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .WithInnerException().WithMessage(expectedInnerMessage);

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException(
                innerException: CreateCustomException());
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws<CustomException>().WithInnerException();

            await Assert.That(result).IsSameReferenceAs(exception.InnerException);
        }

        [Test]
        public async Task Succeed_For_Matching_Message()
        {
            var outerMessage = "foo";
            var innerMessage = "bar";
            Exception exception = CreateCustomException(outerMessage, CreateCustomException(innerMessage));
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .WithInnerException().WithMessage(innerMessage);

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
