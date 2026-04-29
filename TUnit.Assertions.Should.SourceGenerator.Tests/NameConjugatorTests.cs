namespace TUnit.Assertions.Should.SourceGenerator.Tests;

/// <summary>
/// Direct unit tests for the pure-function <see cref="NameConjugator"/>. The end-to-end
/// generator tests in <see cref="ShouldExtensionGeneratorTests"/> exercise the same rules
/// indirectly, but only for the rules each test snippet happens to hit; these cases lock
/// in the full conjugation table — including the <c>-es</c> drops that surfaced as a bug
/// during PR review.
/// </summary>
public class NameConjugatorTests
{
    [Test]
    [Arguments("IsEqualTo", "BeEqualTo")]
    [Arguments("IsZero", "BeZero")]
    [Arguments("IsEmpty", "BeEmpty")]
    [Arguments("IsNotNull", "NotBeNull")]
    [Arguments("IsNotEqualTo", "NotBeEqualTo")]
    [Arguments("IsNotEmpty", "NotBeEmpty")]
    [Arguments("HasNotBeenCalled", "NotHaveBeenCalled")]
    [Arguments("HasCount", "HaveCount")]
    [Arguments("HasFiles", "HaveFiles")]
    [Arguments("DoesNotContain", "NotContain")]
    [Arguments("DoesNotMatch", "NotMatch")]
    [Arguments("DoesMatch", "Match")]
    [Arguments("Contains", "Contain")]
    [Arguments("StartsWith", "StartWith")]
    [Arguments("EndsWith", "EndWith")]
    [Arguments("Throws", "Throw")]
    [Arguments("Matches", "Match")]
    [Arguments("MatchesRegex", "MatchRegex")]
    [Arguments("Washes", "Wash")]
    [Arguments("Catches", "Catch")]
    [Arguments("Fixes", "Fix")]
    [Arguments("Buzzes", "Buzz")]
    [Arguments("Goes", "Go")]
    [Arguments("Passes", "Pass")]
    [Arguments("Writes", "Write")]
    [Arguments("Applies", "Apply")]
    [Arguments("Tries", "Try")]
    [Arguments("Dies", "Die")]
    [Arguments("Ties", "Tie")]
    [Arguments("Pass", "Pass")]
    [Arguments("Issue", "Issue")]
    [Arguments("Is", "Be")]
    [Arguments("Has", "Have")]
    [Arguments("", "")]
    public async Task Conjugate_returns_expected(string input, string expected)
    {
        await Assert.That(NameConjugator.Conjugate(input)).IsEqualTo(expected);
    }
}
