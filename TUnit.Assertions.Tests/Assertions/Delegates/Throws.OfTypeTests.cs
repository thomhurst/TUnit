namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class OfTypeTests
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw a CustomException
                                  
                                  but an OtherException was thrown
                                  
                                  at Assert.That(action).Throws<CustomException>()
                                  """;
            Exception exception = CreateOtherException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Supertype_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw a SubCustomException
                                  
                                  but a CustomException was thrown
                                  
                                  at Assert.That(action).Throws<SubCustomException>()
                                  """;
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<SubCustomException>();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw a CustomException
                                  
                                  but none was thrown
                                  
                                  at Assert.That(action).Throws<CustomException>()
                                  """;
            var action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws<CustomException>();

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Subtype_Exceptions()
        {
            Exception exception = CreateSubCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Succeeds_For_Code_With_Exact_Exceptions()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
