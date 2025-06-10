using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class ExceptionTests
    {
        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw an exception
                                  
                                  but none was thrown
                                  
                                  at Assert.That(action).ThrowsException()
                                  """;
            var action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application assemblies.", Justification = "Test method.")]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).ThrowsException();

            await Assert.That(result).IsSameReferenceAs(exception);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Exceptions()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException();

            await Assert.That(sut).ThrowsNothing();
        }

        private void ThrowsArgumentException()
            => throw new ArgumentException("Just for testing");

        [Test]
        public async Task ThrowsAsync_DoesNotCheckType()
        {
            await Assert.That(async () =>
                await Assert.ThrowsAsync<OverflowException>(LocalTestFunction)
            ).Throws<AssertionException>();
            return;

            Task LocalTestFunction()
            {
                ThrowsArgumentException();
                return Task.CompletedTask;
            }
        }
    }
}
