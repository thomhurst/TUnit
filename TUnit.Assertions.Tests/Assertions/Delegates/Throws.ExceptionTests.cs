namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class ExceptionTests
    {
        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw an exception, but none was thrown.
                At Assert.That(action).Throws().Exception()
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).Throws().Exception();

            await Assert.That(sut).Throws().Exception()
                .WithMessageMatching(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Exceptions()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws().Exception();

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
