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
				Expected action to throw nothing but a CustomException was thrown
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
				Expected action to throw an exception but none was thrown
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
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
				Expected action to throw exactly a CustomException but none was thrown
				""";
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException().OfType<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            string expectedMessage = """
				Expected action to throw exactly a CustomException but an OtherException was thrown
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
				Expected action to throw exactly a CustomException but a SubCustomException was thrown
				""";
            Exception exception = SubCustomException.Create();
            Action action = () => throw exception;

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
        public async Task Fails_For_Code_Without_Exceptions()
        {
            string expectedMessage = """
				Expected action to throw a CustomException but none was thrown
				""";
            Action action = () => { };

            var sut = async ()
                => await Assert.That(action).ThrowsException().SubClassOf<CustomException>();

            await Assert.That(sut).ThrowsException()
                .With.Message.EqualTo(expectedMessage);
        }

        [Test]
        public async Task Fails_For_Code_With_Other_Exceptions()
        {
            string expectedMessage = """
				Expected action to throw a CustomException but an OtherException was thrown
				""";
            Exception exception = OtherException.Create();
            Action action = () => throw exception;

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
				Expected action to have Message equal to "Fails_For_Some_Other_Reason" but it differs at index 10:
				              ↓
				   "Fails_For_Exceptions_With_Different_Message"
				   "Fails_For_Some_Other_Reason"
				              ↑
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
				Expected action to have Message containing "Fails_For_Some_Other_Reason" but it was not found
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
