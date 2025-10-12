using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Chaining;

/// <summary>
/// Combines two assertions with And logic - both must pass.
/// Used internally by And chaining. Most users won't interact with this directly.
/// </summary>
public class AndAssertion<TValue> : Assertion<TValue>
{
    private readonly Assertion<TValue> _first;
    private readonly Assertion<TValue> _second;

    public AndAssertion(
        Assertion<TValue> first,
        Assertion<TValue> second)
        : base(first.InternalContext)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    /// <summary>
    /// Throws when attempting to mix Or with And operators.
    /// </summary>
    public new OrContinuation<TValue> Or => throw new MixedAndOrAssertionsException();

    public override async Task<TValue?> AssertAsync()
    {
        var currentScope = AssertionScope.GetCurrentAssertionScope();

        // Try first assertion
        if (currentScope != null)
        {
            // Inside Assert.Multiple - track exception count
            var exceptionCountBefore = currentScope.ExceptionCount;
            await _first.AssertAsync();

            if (currentScope.ExceptionCount > exceptionCountBefore)
            {
                // First assertion failed - build combined message showing both parts
                var firstException = currentScope.GetLastException();
                currentScope.RemoveLastExceptions(1);

                var combinedExpectation = BuildCombinedExpectation();
                var butIndex = firstException.Message.IndexOf("but ");
                if (butIndex >= 0)
                {
                    var restOfMessage = firstException.Message.Substring(butIndex);
                    var combinedException = new AssertionException($"""
                        Expected {combinedExpectation}
                        {restOfMessage}
                        """);
                    currentScope.AddException(combinedException);
                }
                else
                {
                    currentScope.AddException((AssertionException)firstException);
                }

                return default;
            }
        }
        else
        {
            // Not in Assert.Multiple - first must pass
            await _first.AssertAsync();  // Will throw if fails
        }

        // First passed, try second assertion
        if (currentScope != null)
        {
            var exceptionCountBefore = currentScope.ExceptionCount;
            await _second.AssertAsync();

            if (currentScope.ExceptionCount > exceptionCountBefore)
            {
                // Second failed - build combined message
                var secondException = currentScope.GetLastException();
                currentScope.RemoveLastExceptions(1);

                var combinedExpectation = BuildCombinedExpectation();
                var butIndex = secondException.Message.IndexOf("but ");
                if (butIndex >= 0)
                {
                    var restOfMessage = secondException.Message.Substring(butIndex);
                    var combinedException = new AssertionException($"""
                        Expected {combinedExpectation}
                        {restOfMessage}
                        """);
                    currentScope.AddException(combinedException);
                }
                else
                {
                    currentScope.AddException((AssertionException)secondException);
                }

                return default;
            }

            // Both passed
            return default;
        }
        else
        {
            // Not in Assert.Multiple
            try
            {
                var result = await _second.AssertAsync();
                return result;
            }
            catch (AssertionException ex)
            {
                // Second failed - build combined message
                var combinedExpectation = BuildCombinedExpectation();
                var butIndex = ex.Message.IndexOf("but ");
                if (butIndex >= 0)
                {
                    var restOfMessage = ex.Message.Substring(butIndex);
                    throw new AssertionException($"""
                        Expected {combinedExpectation}
                        {restOfMessage}
                        """);
                }
                throw;
            }
        }
    }

    private string BuildCombinedExpectation()
    {
        var firstExpectation = _first.InternalGetExpectation();
        var firstBecause = _first.InternalBecauseMessage;
        var secondExpectation = _second.InternalGetExpectation();

        if (firstBecause != null)
        {
            var becausePrefix = firstBecause.StartsWith("because ", StringComparison.OrdinalIgnoreCase)
                ? firstBecause
                : $"because {firstBecause}";
            return $"{firstExpectation}, {becausePrefix}{Environment.NewLine}and {secondExpectation}";
        }

        return $"{firstExpectation}{Environment.NewLine}and {secondExpectation}";
    }

    protected override string GetExpectation() => "both conditions";
}
