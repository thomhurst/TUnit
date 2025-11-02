using System.Diagnostics.CodeAnalysis;
namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithParameterName
    {
        [Test]
        public async Task Fails_For_Different_Parameter_Name()
        {
            var paramName1 = "foo";
            var paramName2 = "bar";
            var expectedMessage = """
                                  Expected exactly ArgumentException to have parameter name "bar"
                                  but ArgumentException parameter name was "foo"

                                  at Assert.That(action).ThrowsExactly<ArgumentException>().WithParameterName("bar")
                                  """;
            ArgumentException exception = new(string.Empty, paramName1);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<ArgumentException>().WithParameterName(paramName2);

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public async Task Returns_Exception_When_Awaited()
        {
            var matchingParamName = "foo";
            ArgumentException exception = new(string.Empty, matchingParamName);
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws<ArgumentException>().WithParameterName(matchingParamName);

            await Assert.That(result).IsSameReferenceAs(exception);
        }

        [Test]
        public async Task Succeed_For_Matching_Parameter_Name()
        {
            var matchingParamName = "foo";
            ArgumentException exception = new(string.Empty, matchingParamName);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<ArgumentException>().WithParameterName(matchingParamName);

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task ThrowsExactly_Fails_For_Subclass_Exception_With_WithParameterName()
        {
            var expectedMessage = """
                                  Expected exactly ArgumentException to have parameter name "quantity"
                                  but wrong exception type: ArgumentOutOfRangeException instead of exactly ArgumentException

                                  at Assert.That(action).ThrowsExactly<ArgumentException>().WithParameterName("quantity")
                                  """;
            var exception = new ArgumentOutOfRangeException(paramName: "quantity", message: "must be less than 20");
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<ArgumentException>().WithParameterName("quantity");

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task ThrowsExactly_Succeeds_For_Exact_Type_With_WithParameterName()
        {
            var matchingParamName = "foo";
            ArgumentException exception = new(string.Empty, matchingParamName);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<ArgumentException>().WithParameterName(matchingParamName);

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Throws_Succeeds_For_Subclass_Exception_With_WithParameterName()
        {
            var matchingParamName = "quantity";
            var exception = new ArgumentOutOfRangeException(paramName: matchingParamName, message: "must be less than 20");
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<ArgumentException>().WithParameterName(matchingParamName);

            await Assert.That(sut).ThrowsNothing();
        }
    }
}
