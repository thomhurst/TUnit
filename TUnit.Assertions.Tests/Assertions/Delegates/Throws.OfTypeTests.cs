using System.Diagnostics.CodeAnalysis;
namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class OfTypeTests
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            var expectedMessage = """
                                  Expected to throw CustomException
                                  but threw TUnit.Assertions.Tests.Assertions.Delegates.Throws+OtherException

                                  at Assert.That(action).Throws<CustomException>()
                                  """.NormalizeLineEndings();
            Exception exception = CreateOtherException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Supertype_Exceptions()
        {
            var expectedMessage = """
                                  Expected to throw SubCustomException
                                  but threw TUnit.Assertions.Tests.Assertions.Delegates.Throws+CustomException

                                  at Assert.That(action).Throws<SubCustomException>()
                                  """.NormalizeLineEndings();
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<SubCustomException>();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            var expectedMessage = """
                                  Expected to throw CustomException
                                  but no exception was thrown

                                  at Assert.That(action).Throws<CustomException>()
                                  """.NormalizeLineEndings();
            var action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws<CustomException>();

            await Assert.That(result).IsSameReferenceAs(exception);
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
