using System.Text.RegularExpressions;

namespace TUnit.Assertions.Tests.Old;

public partial class StringRegexAssertionTests
{

    #region Matches Succeeds
    [Test]
    public async Task Matches_WithValidPattern_StringPattern_Succeeds()
    {
        var text = "Hello123World";
        var pattern = @"\w+\d+\w+";

        await TUnitAssert.That(text).Matches(pattern);
    }

    [Test]
    public async Task Matches_WithValidPattern_RegexPattern_Succeeds()
    {
        var text = "Hello123World";
        var pattern = new Regex(@"\w+\d+\w+");

        await TUnitAssert.That(text).Matches(pattern);
    }

#if NET // Needed because NetFramework doesn't support partial methods
    [GeneratedRegex(@"\w+\d+\w+")]
    private static partial Regex FindHello123WorldRegex();

    [Test]
    public async Task Matches_WithValidPattern_GeneratedRegexPattern_Succeeds() 
    {
        var text = "Hello123World";
        Regex regex = FindHello123WorldRegex();
        
        await TUnitAssert.That(text).Matches(regex);
    }
#endif
    #endregion

    #region Matches Throws
    [Test]
#if NET
    [Arguments(typeof(RegexParseException), @"[", null!)] // invalid regex
#endif
    [Arguments(typeof(ArgumentNullException), @"^\d+$", null)]
    [Arguments(typeof(TUnitAssertionException), @"^\d+$", "Hello123World")]
    public async Task Matches_WithInvalidPattern_StringPattern_Throws(Type exceptionType, string pattern, string? text)
    {
        Func<Task> action = async () => await TUnitAssert.That(text).Matches(pattern);

        Exception? exception = await TUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException))
        {
            return;
        }

        await TUnitAssert.That(exception!.Message).IsEqualTo(
            $"""
             Expected text match pattern

             but The regex "^\d+$" does not match with "{text}"

             at Assert.That(text).Matches(pattern)
             """
        );
    }

    [Test]
    [Arguments(typeof(ArgumentNullException), null)]
    [Arguments(typeof(TUnitAssertionException), "Hello123World")]
    public async Task Matches_WithInvalidPattern_RegexPattern_Throws(Type exceptionType, string? text)
    {
        var pattern = new Regex(@"^\d+$");

        Func<Task> action = async () => await TUnitAssert.That(text).Matches(pattern);

        var exception = await TUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException))
        {
            return;
        }

        await TUnitAssert.That(exception!.Message).IsEqualTo(
            $"""
             Expected text match pattern

             but The regex "^\d+$" does not match with "{text}"

             at Assert.That(text).Matches(pattern)
             """
        );
    }

#if NET // Needed because NetFramework doesn't support partial methods
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex Matches_FindNumberRegex();
    
    [Test]
    [Arguments(typeof(ArgumentNullException), null)]
    [Arguments(typeof(TUnitAssertionException), "Hello123World")]
    public async Task Matches_WithInvalidPattern_GeneratedRegexPattern_Throws(Type exceptionType, string? text) 
    {
        Regex regex = Matches_FindNumberRegex();
        
        Func<Task> action = async () => await TUnitAssert.That(text).Matches(regex);

        Exception? exception = await TUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            $"""
             Expected text match regex
             
             but The regex "^\d+$" does not match with "Hello123World"
             
             at Assert.That(text).Matches(regex)
             """
        );
    }
#endif

    [Test]
    [Arguments(typeof(RegexMatchTimeoutException), "(a+)+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    [Arguments(typeof(RegexMatchTimeoutException), @"^(([a-z])+.)+[A-Z]([a-z])+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    public async Task Matches_WithTimeoutPattern_Throws(Type exceptionType, string pattern, string text)
    {
        // Create regex with a short timeout
#if NET8_0_OR_GREATER
            var timeout = TimeSpan.FromMicroseconds(1);
#else
        var timeout = TimeSpan.FromTicks(1);
#endif
        var regex = new Regex(pattern, RegexOptions.None, timeout);

        Func<Task> action = async () => await TUnitAssert.That(text).Matches(regex);

        var exception = await TUnitAssert.ThrowsAsync<RegexMatchTimeoutException>(action);
        await TUnitAssert.That(exception!.Pattern).IsEqualTo(pattern);
    }
    #endregion

    #region DoesNotMatch Succeeds
    [Test]
    public async Task DoesNotMatch_WithValidPattern_StringPattern_Succeeds()
    {
        var text = "Hello123World";
        var pattern = @"^\d+$";

        await TUnitAssert.That(text).DoesNotMatch(pattern);
    }

    [Test]
    public async Task DoesNotMatch_WithValidPattern_RegexPattern_Succeeds()
    {
        var text = "Hello123World";
        var pattern = new Regex(@"^\d+$");

        await TUnitAssert.That(text).DoesNotMatch(pattern);
    }

#if NET // Needed because NetFramework doesn't support partial methods
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex DoesNotMatch_FindNumberOnlyRegex();

    [Test]
    public async Task DoesNotMatch_WithValidPattern_GeneratedRegexPattern_Succeeds() 
    {
        var text = "Hello123World";
        Regex regex = DoesNotMatch_FindNumberOnlyRegex();
        
        await TUnitAssert.That(text).DoesNotMatch(regex);
    }
#endif
    #endregion

    #region DoesNotMatch Throws
    [Test]
#if NET
    [Arguments(typeof(RegexParseException), @"[", null!)] // invalid regex
#endif
    [Arguments(typeof(ArgumentNullException), @"^\d+$", null)]
    [Arguments(typeof(TUnitAssertionException), @"^\d+$", "123")]
    public async Task DoesNotMatch_WithInvalidPattern_StringPattern_Throws(Type exceptionType, string pattern, string? text)
    {
        Func<Task> action = async () => await TUnitAssert.That(text).DoesNotMatch(pattern);

        Exception? exception = await TUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException))
        {
            return;
        }

        await TUnitAssert.That(exception!.Message).IsEqualTo(
            $"""
             Expected text to not match with pattern
             
             but The regex "^\d+$" matches with "{text}"
             
             at Assert.That(text).DoesNotMatch(pattern)
             """
        );
    }

    [Test]
    [Arguments(typeof(ArgumentNullException), null)]
    [Arguments(typeof(TUnitAssertionException), "123")]
    public async Task DoesNotMatch_WithInvalidPattern_RegexPattern_Throws(Type exceptionType, string? text)
    {
        var pattern = new Regex(@"^\d+$");

        Func<Task> action = async () => await TUnitAssert.That(text).DoesNotMatch(pattern);

        Exception? exception = await TUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException))
        {
            return;
        }

        await TUnitAssert.That(exception!.Message).IsEqualTo(
            $"""
             Expected text to not match with pattern
             
             but The regex "^\d+$" matches with "{text}"
             
             at Assert.That(text).DoesNotMatch(pattern)
             """
        );
    }

#if NET // Needed because NetFramework doesn't support partial methods
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex FindNumberRegex();

    [Test]
    [Arguments(typeof(ArgumentNullException), null)]
    [Arguments(typeof(TUnitAssertionException), "123")]
    public async Task DoesNotMatch_WithInvalidPattern_GeneratedRegexPattern_Throws(Type exceptionType, string? text) 
    {
        Regex regex = FindNumberRegex();
        
        Func<Task> action = async () => await TUnitAssert.That(text).DoesNotMatch(regex);

        Exception? exception = await TUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        await TUnitAssert.That(exception!.Message).IsEqualTo(
            $"""
             Expected text to not match with regex
             
             but The regex "^\d+$" matches with "{text}"
             
             at Assert.That(text).DoesNotMatch(regex)
             """
        );
    }
#endif

    [Test]
    [Arguments(typeof(RegexMatchTimeoutException), "(a+)+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    [Arguments(typeof(RegexMatchTimeoutException), @"^(([a-z])+.)+[A-Z]([a-z])+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    public async Task DoesNotMatch_WithTimeoutPattern_Throws(Type exceptionType, string pattern, string text)
    {
        // Create regex with a short timeout
#if NET8_0_OR_GREATER
            var timeout = TimeSpan.FromMicroseconds(1);
#else
        var timeout = TimeSpan.FromTicks(1);
#endif
        var regex = new Regex(pattern, RegexOptions.None, timeout);

        Func<Task> action = async () => await TUnitAssert.That(text).DoesNotMatch(regex);

        var exception = await TUnitAssert.ThrowsAsync<RegexMatchTimeoutException>(action);
        await TUnitAssert.That(exception!.Pattern).IsEqualTo(pattern);
    }
    #endregion
}
