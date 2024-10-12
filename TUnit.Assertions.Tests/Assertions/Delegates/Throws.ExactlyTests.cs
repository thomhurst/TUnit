namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class ExactlyTests
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but an OtherException was thrown.
                At Assert.That(action).Throws().Exactly<CustomException>()
                """;
            Exception exception = CreateOtherException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Subtype_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but a SubCustomException was thrown.
                At Assert.That(action).Throws().Exactly<CustomException>()
                """;
            Exception exception = CreateSubCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but none was thrown.
                At Assert.That(action).Throws().Exactly<CustomException>()
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Correct_Exception()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
