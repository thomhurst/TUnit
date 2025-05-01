using System.Text.RegularExpressions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

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
    [TestCase(typeof(RegexParseException), @"[", null!)] // invalid regex
    #endif 
    [TestCase(typeof(ArgumentNullException), @"^\d+$", null)]
    [TestCase(typeof(TUnitAssertionException), @"^\d+$", "Hello123World")]
    public void Matches_WithInvalidPattern_StringPattern_Throws(Type exceptionType, string pattern, string? text) 
    {
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).Matches(pattern);

        Exception? exception = NUnitAssert.ThrowsAsync(exceptionType,action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            $"""
             Expected text match pattern

             but The regex "^\d+$" does not match with "{text}"

             at Assert.That(text).Matches(pattern)
             """
        ));
    }
    
    [Test]
    [TestCase(typeof(ArgumentNullException), null)]
    [TestCase(typeof(TUnitAssertionException), "Hello123World")]
    public void Matches_WithInvalidPattern_RegexPattern_Throws(Type exceptionType, string? text) 
    {
        var pattern = new Regex(@"^\d+$");
        
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).Matches(pattern); 

        var exception = NUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }

        NUnitAssert.That(exception!.Message, Is.EqualTo(
            $"""
             Expected text match pattern

             but The regex "^\d+$" does not match with "{text}"

             at Assert.That(text).Matches(pattern)
             """
        ));
    }
    
    #if NET // Needed because NetFramework doesn't support partial methods
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex Matches_FindNumberRegex();
    
    [Test]
    [TestCase(typeof(ArgumentNullException), null)]
    [TestCase(typeof(TUnitAssertionException), "Hello123World")]
    public void Matches_WithInvalidPattern_GeneratedRegexPattern_Throws(Type exceptionType, string? text) 
    {
        Regex regex = Matches_FindNumberRegex();
        
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).Matches(regex);

        Exception? exception = NUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            $"""
             Expected text match regex
             
             but The regex "^\d+$" does not match with "Hello123World"
             
             at Assert.That(text).Matches(regex)
             """
        ));
    }
    #endif
    
    [Test]
    [TestCase(typeof(RegexMatchTimeoutException), "(a+)+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    [TestCase(typeof(RegexMatchTimeoutException), @"^(([a-z])+.)+[A-Z]([a-z])+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    public void Matches_WithTimeoutPattern_Throws(Type exceptionType, string pattern, string text)
    {
        // Create regex with a short timeout
        #if NET8_0_OR_GREATER
            var timeout = TimeSpan.FromMicroseconds(1);
        #else
            var timeout = TimeSpan.FromTicks(1);
        #endif
        var regex = new Regex(pattern, RegexOptions.None, timeout);
    
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).Matches(regex);

        var exception = NUnitAssert.ThrowsAsync<RegexMatchTimeoutException>(action);
        NUnitAssert.That(exception!.Pattern, Is.EqualTo(pattern));
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
    [TestCase(typeof(RegexParseException), @"[", null!)] // invalid regex
    #endif 
    [TestCase(typeof(ArgumentNullException), @"^\d+$", null)]
    [TestCase(typeof(TUnitAssertionException), @"^\d+$", "123")]
    public void DoesNotMatch_WithInvalidPattern_StringPattern_Throws(Type exceptionType, string pattern, string? text) 
    {
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).DoesNotMatch(pattern);

        Exception? exception = NUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            $"""
             Expected text to not match with pattern
             
             but The regex "^\d+$" matches with "{text}"
             
             at Assert.That(text).DoesNotMatch(pattern)
             """
        ));
    }

    [Test]
    [TestCase(typeof(ArgumentNullException), null)]
    [TestCase(typeof(TUnitAssertionException), "123")]
    public void DoesNotMatch_WithInvalidPattern_RegexPattern_Throws(Type exceptionType, string? text) 
    {
        var pattern = new Regex(@"^\d+$");
        
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).DoesNotMatch(pattern);

        Exception? exception = NUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            $"""
             Expected text to not match with pattern
             
             but The regex "^\d+$" matches with "{text}"
             
             at Assert.That(text).DoesNotMatch(pattern)
             """
        ));
    }

    #if NET // Needed because NetFramework doesn't support partial methods
    [GeneratedRegex(@"^\d+$")]
    private static partial Regex FindNumberRegex();

    [Test]
    [TestCase(typeof(ArgumentNullException), null)]
    [TestCase(typeof(TUnitAssertionException), "123")]
    public void DoesNotMatch_WithInvalidPattern_GeneratedRegexPattern_Throws(Type exceptionType, string? text) 
    {
        Regex regex = FindNumberRegex();
        
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).DoesNotMatch(regex);

        Exception? exception = NUnitAssert.ThrowsAsync(exceptionType, action);
        if (exceptionType != typeof(TUnitAssertionException)) 
        {
            return;
        }
        
        NUnitAssert.That(exception!.Message, Is.EqualTo(
            $"""
             Expected text to not match with regex
             
             but The regex "^\d+$" matches with "{text}"
             
             at Assert.That(text).DoesNotMatch(regex)
             """
        ));
    }
    #endif
    
    [Test]
    [TestCase(typeof(RegexMatchTimeoutException), "(a+)+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    [TestCase(typeof(RegexMatchTimeoutException), @"^(([a-z])+.)+[A-Z]([a-z])+$", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa!")]
    public void DoesNotMatch_WithTimeoutPattern_Throws(Type exceptionType, string pattern, string text)
    {
        // Create regex with a short timeout
        #if NET8_0_OR_GREATER
            var timeout = TimeSpan.FromMicroseconds(1);
        #else
            var timeout = TimeSpan.FromTicks(1);
        #endif
        var regex = new Regex(pattern, RegexOptions.None, timeout);
    
        AsyncTestDelegate action = async () => await TUnitAssert.That(text).DoesNotMatch(regex);

        var exception = NUnitAssert.ThrowsAsync<RegexMatchTimeoutException>(action);
        NUnitAssert.That(exception!.Pattern, Is.EqualTo(pattern));
    }
    #endregion
}