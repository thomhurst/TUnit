namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class NothingTests
    {
        [Test]
        public async Task Fails_For_Code_With_Exceptions()
        {
            string expectedMessage = $$"""
                Expected action to throw nothing, but a CustomException was thrown:
                {{nameof(Fails_For_Code_With_Exceptions)}}.
                At Assert.That(action).ThrowsNothing()
                """;
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsNothing();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_Without_Exceptions()
        {
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsNothing();

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Returns_Awaited_Result()
        {
            int value = 42;
            Func<int> action = () => value;

            var result = await Assert.That(action).ThrowsNothing();

            await Assert.That(result).IsEqualTo(value);
        }
    }
}
