namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class ExactlyTests
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw exactly a CustomException

                                  but an OtherException was thrown

                                  at Assert.That(action).ThrowsExactly<CustomException>()
                                  """;
            Exception exception = CreateOtherException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Subtype_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw exactly a CustomException

                                  but a SubCustomException was thrown

                                  at Assert.That(action).ThrowsExactly<CustomException>()
                                  """;
            Exception exception = CreateSubCustomException();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            var expectedMessage = """
                                  Expected action to throw exactly a CustomException

                                  but none was thrown

                                  at Assert.That(action).ThrowsExactly<CustomException>()
                                  """;
            var action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>();

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            Exception exception = CreateCustomException();
            Action action = () => throw exception;

            var result = await Assert.That(action).ThrowsExactly<CustomException>();

            await Assert.That<object?>(result).IsSameReference(exception);
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

            await Assert.That(action)
                .ThrowsExactly<CustomException>()
                .And
                .HasMessageEqualTo("Foo bar message")
                .And
                .IsAssignableTo<CustomException>();
        }
        
        [Test]
        public async Task Conversion_To_Value_Assertion_Builder_On_Casted_Exception_Type_Throws_When_Wrong_Type()
        {
            Exception exception = CreateCustomException("Foo bar message", new ArgumentNullException());
            
            Action action = () => throw exception;

            var assertionException = await Assert.ThrowsAsync<AssertionException>(async () =>
                await Assert.That(action)
                    .ThrowsExactly<Exception>()
                    .And
                    .HasMessageEqualTo("Foo bar message")
                    .And
                    .IsAssignableTo<CustomException>()
            );

            await Assert.That(assertionException).HasMessageStartingWith("""
                                                                         Expected action to throw exactly an Exception
                                                                         
                                                                         but a CustomException was thrown
                                                                         """);
        }
        
        [Test]
        public async Task Conversion_To_Value_Assertion_Builder_On_Casted_Exception_Type_Throws_When_InvalidMessage()
        {
            Exception exception = CreateCustomException("Foo bar message");
            
            Action action = () => throw exception;

            var assertionException = await Assert.ThrowsAsync<AssertionException>(async () =>
                await Assert.That(action)
                    .ThrowsExactly<CustomException>()
                    .And
                    .HasMessageEqualTo("Foo bar message!")
                    .And
                    .IsAssignableTo<CustomException>()
            );

            await Assert.That(assertionException).HasMessageStartingWith(
                """
                Expected action to throw exactly a CustomException
                 and message to be equal to "Foo bar message!"
                 and to be assignable to type CustomException
                
                but found message "Foo bar message" which differs at index 15:
                                   ↓
                   "Foo bar message"
                   "Foo bar message!"
                                   ↑
                
                at Assert.That(action).ThrowsExactly<CustomException>().And.HasMessageEqualTo("Foo bar message!", Strin...
                """
                );
        }
    }
}
