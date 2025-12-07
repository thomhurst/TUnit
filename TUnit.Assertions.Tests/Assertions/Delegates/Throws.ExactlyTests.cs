using System.Diagnostics.CodeAnalysis;
namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class ExactlyTests
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            var expectedMessage = """
                                  Expected to throw exactly CustomException
                                  but threw TUnit.Assertions.Tests.Assertions.Delegates.Throws+OtherException

                                  at Assert.That(action).ThrowsExactly<CustomException>()
                                  """.NormalizeLineEndings();
            Exception exception = CreateOtherException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Subtype_Exceptions()
        {
            var expectedMessage = """
                                  Expected to throw exactly CustomException
                                  but wrong exception type: SubCustomException instead of exactly CustomException

                                  at Assert.That(action).ThrowsExactly<CustomException>()
                                  """.NormalizeLineEndings();
            Exception exception = CreateSubCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            var expectedMessage = """
                                  Expected to throw exactly CustomException
                                  but no exception was thrown

                                  at Assert.That(action).ThrowsExactly<CustomException>()
                                  """.NormalizeLineEndings();
            var action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            var thrownException = await Assert.That(sut).ThrowsException();
            await Assert.That(thrownException.Message.NormalizeLineEndings()).IsEqualTo(expectedMessage);
        }

        [Test]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).ThrowsExactly<CustomException>();

            await Assert.That(result).IsSameReferenceAs(exception);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Correct_Exception()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        public async Task Can_Convert_To_Value_Assertion_Builder_On_Casted_Exception_Type()
        {
            Exception exception = CreateCustomException("Foo bar message");

            Action action = () => throw exception;

            var ex = await Assert.That(action)
                .ThrowsExactly<CustomException>()
                .And
                .HasMessageEqualTo("Foo bar message");

            await Assert.That((object)ex).IsAssignableTo<CustomException>();
        }

        [Test]
        public async Task Conversion_To_Value_Assertion_Builder_On_Casted_Exception_Type_Throws_When_Wrong_Type()
        {
            Exception exception = CreateCustomException("Foo bar message", new ArgumentNullException());

            Action action = () => throw exception;

            var assertionException = await Assert.ThrowsAsync<AssertionException>(async () =>
            {
                var ex = await Assert.That(action)
                    .ThrowsExactly<Exception>()
                    .And
                    .HasMessageEqualTo("Foo bar message");

                await Assert.That((object)ex).IsAssignableTo<CustomException>();
            });

            var expectedPrefix = """
                Expected to throw exactly Exception
                but wrong exception type: CustomException instead of exactly Exception
                """.NormalizeLineEndings();
            await Assert.That(assertionException.Message.NormalizeLineEndings()).StartsWith(expectedPrefix);
        }

        [Test]
        public async Task Conversion_To_Value_Assertion_Builder_On_Casted_Exception_Type_Throws_When_InvalidMessage()
        {
            Exception exception = CreateCustomException("Foo bar message");

            Action action = () => throw exception;

            var assertionException = await Assert.ThrowsAsync<AssertionException>(async () =>
            {
                var ex = await Assert.That(action)
                    .ThrowsExactly<CustomException>()
                    .And
                    .HasMessageEqualTo("Foo bar message!");

                await Assert.That((object)ex).IsAssignableTo<CustomException>();
            });

            var expectedPrefix = """
                Expected to throw exactly CustomException
                and to have message equal to "Foo bar message!"
                """;

            await Assert.That(assertionException.Message.NormalizeLineEndings()).StartsWith(expectedPrefix.NormalizeLineEndings());
        }
    }
}
