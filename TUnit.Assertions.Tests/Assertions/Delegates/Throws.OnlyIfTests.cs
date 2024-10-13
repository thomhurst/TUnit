namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class OnlyIfTests
    {
        [Test]
        public async Task Fails_When_False_And_Exception_Is_Thrown()
        {
            string expectedMessage = $$"""
                Expected action to throw nothing, but a CustomException was thrown:
                {{nameof(Fails_When_False_And_Exception_Is_Thrown)}}.
                At Assert.That(action).ThrowsExactly<CustomException>().OnlyIf(false)
                """;
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>().OnlyIf(false);

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_When_True_And_Exception_Is_Not_Thrown()
        {
            string expectedMessage = """
                Expected action to throw a CustomException, but none was thrown.
                At Assert.That(action).Throws<CustomException>().OnlyIf(true)
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>().OnlyIf(true);

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Succeeds_When_False_And_Exception_Is_Not_Thrown()
        {
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>().OnlyIf(false);

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Succeeds_When_True_And_Exception_Is_Thrown()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>().OnlyIf(true);

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
