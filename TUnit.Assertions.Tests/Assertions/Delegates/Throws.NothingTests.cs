namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class NothingTests
    {
        [Test]
        public async Task Fails_For_Code_With_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw nothing, but a CustomException was thrown.
                At Assert.That(action).Throws().Nothing()
                """;
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Nothing();

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_Without_Exceptions()
        {
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws().Nothing();

            await Assert.That(sut).Throws().Nothing();
        }

        [Test]
        public async Task Returns_Awaited_Result()
        {
            int value = 42;
            Func<int> action = () => value;

            var result = await Assert.That(action).Throws().Nothing();

            await Assert.That(result).IsEqualTo(value);
        }
    }
}
