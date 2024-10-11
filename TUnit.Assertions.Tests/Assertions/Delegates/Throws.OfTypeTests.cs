namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class OfTypeTests
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw a CustomException, but an OtherException was thrown.
                At Assert.That(action).Throws().OfType<CustomException>()
                """;
            Exception exception = OtherException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().OfType<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessageMatching(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Supertype_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw a SubCustomException, but a CustomException was thrown.
                At Assert.That(action).Throws().OfType<SubCustomException>()
                """;
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().OfType<SubCustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessageMatching(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw a CustomException, but none was thrown.
                At Assert.That(action).Throws().OfType<CustomException>()
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws().OfType<CustomException>();

            await Assert.That(sut).Throws().Exception()
                .WithMessageMatching(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Subtype_Exceptions()
        {
            Exception exception = SubCustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().OfType<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Succeeds_For_Code_With_Exact_Exceptions()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().OfType<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
