namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithMessageMatchingTests
    {
        [Test]
        public async Task Fails_For_Different_Messages()
        {
            string message1 = "foo";
            string message2 = "bar";
            string expectedMessage = """
                Expected action to throw a CustomException which message matches "bar", but found "foo".
                At Assert.That(action).Throws().Exactly<CustomException>().WithMessageMatching(message2)
                """;
            Exception exception = CreateCustomException(message1);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exactly<CustomException>().WithMessageMatching(message2);

            await Assert.That(sut).Throws().Exception()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            string matchingMessage = "foo";
            Exception exception = CreateCustomException(matchingMessage);
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws().OfType<CustomException>().WithMessageMatching(matchingMessage);

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeeds_For_Matching_Message()
        {
            string matchingMessage = "foo";
            Exception exception = CreateCustomException(matchingMessage);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().OfType<CustomException>().WithMessageMatching(matchingMessage);

            await Assert.That(sut).Throws().Nothing();
        }
    }
}
