namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class ExceptionTests
    {
        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw an exception, but none was thrown.
                At Assert.That(action).ThrowsException()
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).ThrowsException();

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Exceptions()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException();

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
