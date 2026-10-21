using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Pins the <c>GetExpectation()</c> code <see cref="AssertionMethodGenerator"/> renders for each documented form of
/// [AssertionFrom] <c>ExpectationMessage</c>.
/// </summary>
internal class ExpectationMessageRenderingTests
{
    private const string MessagesSource =
        """
        using TUnit.Assertions.Attributes;

        namespace Rendering;

        public static class Methods
        {
            public static bool Canonical(string value) => true;
            public static bool SpelledOutTo(string value) => true;
            public static bool SpelledOutNot(string value, string text) => true;
            public static bool LeadingNot(string value, string text) => true;
            public static bool EscapedBraces(string value, string text) => true;
            public static bool UnknownPlaceholder(string value, string ending) => true;
            public static bool NonAscii(string value, string önskad) => true;
            public static bool GeneratedField(string value, string prefix) => true;
            public static bool Quoted(string value) => true;
            public static bool Default(string value, string text) => true;
            public static bool NotWithoutNegateLogic(string value, string text) => true;
            public static bool ToDo(string value) => true;
        }

        [AssertionFrom<string>(typeof(Methods), nameof(Methods.Canonical), ExpectationMessage = "be blank")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.SpelledOutTo), ExpectationMessage = "to be spelled out")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.SpelledOutNot), NegateLogic = true, ExpectationMessage = "to not contain {text}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.LeadingNot), NegateLogic = true, ExpectationMessage = "not start with {text}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.EscapedBraces), ExpectationMessage = "contain {{text}} and {text}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.UnknownPlaceholder), ExpectationMessage = "end with {suffix}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.NonAscii), ExpectationMessage = "contain {önskad}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.GeneratedField), ExpectationMessage = "start with {_prefix}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.Quoted), ExpectationMessage = "be \"quoted\" \\ here")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.Default))]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.NotWithoutNegateLogic), ExpectationMessage = "not end with {text}")]
        [AssertionFrom<string>(typeof(Methods), nameof(Methods.ToDo), ExpectationMessage = "to-do list be empty")]
        public static partial class MessageAssertions;
        """;

    private static readonly Lazy<string[]> RenderedExpectations = new(() =>
        AssertionMethodGeneratorRunner.Expectations(AssertionMethodGeneratorRunner.Run(MessagesSource).Sources));

    [Test]
    [Arguments("Canonical form", @"$""to {(_negated ? ""not "" : """")}be blank""")]
    [Arguments("Leading 'to' is not repeated", @"$""to {(_negated ? ""not "" : """")}be spelled out""")]
    [Arguments("Spelled-out 'not' is not negated again", @"$""to not contain {_text}""")]
    [Arguments("Leading 'not' without 'to' is not negated again", @"$""to not start with {_text}""")]
    [Arguments("Escaped braces stay literal", @"$""to {(_negated ? ""not "" : """")}contain {{text}} and {_text}""")]
    [Arguments("Unknown placeholder stays literal", @"$""to {(_negated ? ""not "" : """")}end with {{suffix}}""")]
    [Arguments("Non-ASCII placeholder", @"$""to {(_negated ? ""not "" : """")}contain {_önskad}""")]
    [Arguments("Generated field placeholder", @"$""to {(_negated ? ""not "" : """")}start with {_prefix}""")]
    [Arguments("Quotes and backslashes are escaped", @"$""to {(_negated ? ""not "" : """")}be \""quoted\"" \\ here""")]
    [Arguments("Default message", @"$""to {(_negated ? ""not "" : """")}satisfy Default({_text})""")]
    [Arguments("Leading 'not' is literal without NegateLogic too", @"$""to not end with {_text}""")]
    [Arguments("Only 'to' followed by a space is stripped", @"$""to {(_negated ? ""not "" : """")}to-do list be empty""")]
    public async Task Renders_Expectation(string form, string expected)
    {
        await Assert.That(RenderedExpectations.Value).Contains(expected);
    }
}
