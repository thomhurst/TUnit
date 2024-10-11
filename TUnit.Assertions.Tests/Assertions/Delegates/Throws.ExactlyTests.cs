using TUnit.Assertions.Extensions.Generic;

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
            Exception exception = OtherException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessageMatching(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Subtype_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but a SubCustomException was thrown.
                At Assert.That(action).Throws().Exactly<CustomException>()
                """;
            Exception exception = SubCustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessageMatching(expectedMessage);
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
                .WithMessageMatching(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Correct_Exception()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var customException = await Assert.That(action).Throws().Exactly<CustomException>();

            await Assert.That((object)customException).IsSameReference(exception);
        }
    }
}
