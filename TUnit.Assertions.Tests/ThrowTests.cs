using System.Runtime.CompilerServices;

namespace TUnit.Assertions.Tests;

public class ThrowTests
{
    public class ThrowsNothing
    {
        [Test]
        public async Task Fails_For_Code_With_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw nothing, but a CustomException was thrown.
                At Assert.That(action).ThrowsNothing()
                """;
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsNothing();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_Without_Exceptions()
        {
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsNothing();

            await Assert.That(sut).ThrowsNothing();
        }
    }

    public class ThrowsException
    {
        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw an exception, but none was thrown.
                At Assert.That(action).ThrowsException.OfAnyType()
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Exceptions()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException();

            await Assert.That(sut).ThrowsNothing();
        }
    }

    public class ThrowsException_OfType
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but an OtherException was thrown.
                At Assert.That(action).ThrowsException.OfType(CustomException)
                """;
            Exception exception = OtherException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException().OfType<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Subtype_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but a SubCustomException was thrown.
                At Assert.That(action).ThrowsException.OfType(CustomException)
                """;
            Exception exception = SubCustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException().OfType<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw exactly a CustomException, but none was thrown.
                At Assert.That(action).ThrowsException.OfType(CustomException)
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException().OfType<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Correct_Exception()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException().OfType<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }
    }

    public class ThrowsException_SubClassOf
    {
        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw a CustomException, but an OtherException was thrown.
                At Assert.That(action).ThrowsException.SubClassOf(CustomException)
                """;
            Exception exception = OtherException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException().SubClassOf<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
                Expected action to throw a CustomException, but none was thrown.
                At Assert.That(action).ThrowsException.SubClassOf(CustomException)
                """;
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException().SubClassOf<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Code_With_Subtype_Exceptions()
        {
            Exception exception = SubCustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException().SubClassOf<CustomException>();

            await Assert.That(sut).ThrowsNothing();
        }
    }

    public class ThrowsException_With_Message_EqualTo
    {
        [Test]
        public async Task Fails_For_Exceptions_With_Different_Message()
        {
            string expectedMessage = """
                                     Expected action to have Message equal to "Fails_For_Some_Other_Reason", but it differs at index 10:
                                                   ↓
                                        "Fails_For_Exceptions_With_Different_Message"
                                        "Fails_For_Some_Other_Reason"
                                                   ↑.
                                     At Assert.That(action).ThrowsException.With.Message.EqualTo("Fails_For_Some_Other_Reason", StringCompar...
                                     """;

            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                    .With.Message.EqualTo("Fails_For_Some_Other_Reason");

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Exceptions_With_Different_Multiline_Message()
        {
            string newline = $"{Environment.NewLine}".Replace("\n", "\\n").Replace("\r", "\\r");
            string longCommonString = """
                                      Lorem ipsum dolor sit amet, consetetur sadipscing elitr,
                                      sed diam nonumy eirmod tempor invidunt ut labore et dolore
                                      magna aliquyam erat, sed diam voluptua. At vero eos et
                                      accusam et justo duo dolores et ea rebum. Stet clita kasd
                                      gubergren, no sea takimata sanctus est Lorem ipsum dolor
                                      sit amet.
                                      """;
            string expectedMessage = $$"""
                                     Expected action to have Message equal to "Lorem ipsum dolor sit amet, consetetur sadipscing elitr,{{newline}}sed diam nonumy eirmod tempor invidunt u…", but it differs at index 302:
                                                                     ↓
                                        "ipsum dolor\r\nsit amet.\r\nsome value"
                                        "ipsum dolor\r\nsit amet.\r\nanother value"
                                                                     ↑.
                                     At Assert.That(action).ThrowsException.With.Message.EqualTo($"{longCommonString}{Environment.NewLine}an...
                                     """;

            Exception exception = CustomException.Create($"{longCommonString}{Environment.NewLine}some value");
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                    .With.Message.EqualTo($"{longCommonString}{Environment.NewLine}another value");

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Exceptions_With_Equal_Message()
        {
            string expectedMessage = nameof(Succeeds_For_Exceptions_With_Equal_Message);
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException().With.Message.EqualTo(expectedMessage);

            await Assert.That(sut).ThrowsNothing();
        }
    }

    public class ThrowsException_With_Message_Containing
    {
        [Test]
        public async Task Fails_For_Exceptions_With_Different_Message()
        {
            string expectedMessage = """
                Expected action to have Message containing "Fails_For_Some_Other_Reason", but it was not found.
                At Assert.That(action).ThrowsException.With.Message.Containing("Fails_For_Some_Other_Reason", StringCom...
                """;
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .With.Message.Containing("Fails_For_Some_Other_Reason");

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Succeeds_For_Exceptions_With_Equal_Message()
        {
            Exception exception = CustomException.Create();
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .With.Message.Containing(nameof(Succeeds_For_Exceptions_With_Equal_Message));

            await Assert.That(sut).ThrowsNothing();
        }
    }

    private class CustomException : Exception
    {
        public CustomException(string message) : base(message)
        {

        }

        public static CustomException Create([CallerMemberName] string message = "")
        {
            return new CustomException(message);
        }
    }

    private class SubCustomException : CustomException
    {
        public SubCustomException(string message) : base(message)
        {

        }

        public static SubCustomException Create([CallerMemberName] string message = "")
        {
            return new SubCustomException(message);
        }
    }

    private class OtherException : Exception
    {
        public OtherException(string message) : base(message)
        {

        }

        public static OtherException Create([CallerMemberName] string message = "")
        {
            return new OtherException(message);
        }
    }

}
