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
                                  Expected action to throw an ArgumentException which param name equals "bar"
                                  
                                  but it differs at index 0:
                                      ↓
                                     "foo"
                                     "bar"
                                      ↑
                                  
                                  at Assert.That(action).ThrowsExactly<ArgumentException>().WithParameterName(paramName2)
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
    }
}
