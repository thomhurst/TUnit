using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Tests.Assertions.Delegates;

public partial class Throws
{
    public class WithMessageMatchingTests
    {
        [Test]
        [Arguments("some message", "*me me*", true)]
        [Arguments("some message", "*ME ME*", false)]
        [Arguments("some message", "some?message", true)]
        [Arguments("some message", "some*message", true)]
        [Arguments("some message", "some me?age", false)]
        [Arguments("some message", "some me??age", true)]
        public async Task Defaults_To_Case_Sensitive_Wildcard_Pattern(
            string message, string pattern, bool expectMatch)
        {
            Exception exception = CreateCustomException(message);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>().WithMessageMatching(pattern);

            if (expectMatch)
            {
                await Assert.That(sut).ThrowsNothing();
            }
            else
            {
                await Assert.That(sut).ThrowsException();
            }
        }

        [Test]
        public async Task Fails_For_Different_Messages()
        {
            var message1 = "foo";
            var message2 = "bar";
            var expectedMessage = """
                                  Expected action to throw a CustomException which message matches "bar"
                                  
                                  but found "foo"
                                  
                                  at Assert.That(action).ThrowsExactly<CustomException>().WithMessageMatching(message2)
                                  """;
            Exception exception = CreateCustomException(message1);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsExactly<CustomException>().WithMessageMatching(message2);

            await Assert.That(sut).ThrowsException()
                .WithMessage(expectedMessage);
        }

        [Test]
        public async Task Returns_Exception_When_Awaited()
        {
            var matchingMessage = "foo";
            Exception exception = CreateCustomException(matchingMessage);
            Action action = () => throw exception;

            var result = await Assert.That(action).Throws<CustomException>().WithMessageMatching(matchingMessage);

            await Assert.That((object?)result).IsSameReference(exception);
        }

        [Test]
        public async Task Succeeds_For_Matching_Message()
        {
            var matchingMessage = "foo";
            Exception exception = CreateCustomException(matchingMessage);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).Throws<CustomException>().WithMessageMatching(matchingMessage);

            await Assert.That(sut).ThrowsNothing();
        }

        [Test]
        [Arguments("some message", "*me me*", true)]
        [Arguments("some message", "*ME ME*", true)]
        [Arguments("some message", "Some?message", true)]
        [Arguments("some message", "some*Message", true)]
        [Arguments("some message", "some me?agE", false)]
        [Arguments("some message", "some me??agE", true)]
        public async Task Supports_Case_Insensitive_Wildcard_Pattern(
            string message, string pattern, bool expectMatch)
        {
            var expectedExpression = "*Assert.That(action).ThrowsException().WithMessageMatching(StringMatcher.AsWildcard(pattern).Ignor*";
            Exception exception = CreateCustomException(message);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .WithMessageMatching(StringMatcher.AsWildcard(pattern).IgnoringCase());

            if (expectMatch)
            {
                await Assert.That(sut).ThrowsNothing();
            }
            else
            {
                await Assert.That(sut).ThrowsException().WithMessageMatching(expectedExpression);
            }
        }

        [Test]
        [Arguments("A 1st message", ".*", true)]
        [Arguments("A 1st message", "A \\d.*", true)]
        [Arguments("A 1st message", "a \\d.*", false)]
        [Arguments("A 1st message", "\\dst", true)]
        [Arguments("A 1st message", "^\\dst", false)]
        [Arguments("A 1st message", "s{2,}", true)]
        [Arguments("A 1st message", "s{3,}", false)]
        public async Task Supports_Regex_Pattern(
            string message, string pattern, bool expectMatch)
        {
            var expectedExpression = "*Assert.That(action).ThrowsException().WithMessageMatching(StringMatcher.AsRegex(pattern))*";
            Exception exception = CreateCustomException(message);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .WithMessageMatching(StringMatcher.AsRegex(pattern));

            if (expectMatch)
            {
                await Assert.That(sut).ThrowsNothing();
            }
            else
            {
                await Assert.That(sut).ThrowsException().WithMessageMatching(expectedExpression);
            }
        }

        [Test]
        [Arguments("A 1st message", "a \\d.*", true)]
        [Arguments("A 1st message", "^\\dst", false)]
        public async Task Supports_Case_Insensitive_Regex_Pattern(
            string message, string pattern, bool expectMatch)
        {
            var expectedExpression = "*Assert.That(action).ThrowsException().WithMessageMatching(StringMatcher.AsRegex(pattern).Ignoring*";
            Exception exception = CreateCustomException(message);
            Action action = () => throw exception;

            var sut = async ()
                => await Assert.That(action).ThrowsException()
                .WithMessageMatching(StringMatcher.AsRegex(pattern).IgnoringCase());

            if (expectMatch)
            {
                await Assert.That(sut).ThrowsNothing();
            }
            else
            {
                await Assert.That(sut).ThrowsException().WithMessageMatching(expectedExpression);
            }
        }
    }
}
