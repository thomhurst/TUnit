namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class NothingTests
    {
        [Test]
        public async Task Fails_For_Code_With_Exceptions()
        {
            var expectedMessage = $"""
                                    Expected to not throw any exception
                                    but threw TUnit.Assertions.Tests.Assertions.Delegates.Throws+CustomException: {nameof(Fails_For_Code_With_Exceptions)}

                                    at Assert.That(action).ThrowsNothing()
                                    """.NormalizeLineEndings();
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsNothing();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_Without_Exceptions()
        {
            var action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsNothing();

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Returns_Awaited_Result()
        {
            var value = 42;
            var action = () => value;

            var result = await Assert.That(action).ThrowsNothing();

            await Assert.That(result).IsEqualTo(value);
        }
    }
}
