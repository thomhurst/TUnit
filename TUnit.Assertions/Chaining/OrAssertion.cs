using TUnit.Assertions.Core;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Chaining;

/// <summary>
/// Combines two assertions with Or logic - at least one must pass.
/// Used internally by Or chaining. Most users won't interact with this directly.
/// </summary>
public class OrAssertion<TValue> : Assertion<TValue>
{
    private readonly Assertion<TValue> _first;
    private readonly Assertion<TValue> _second;

    public OrAssertion(
        Assertion<TValue> first,
        Assertion<TValue> second)
        : base(first.InternalContext)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    /// <summary>
    /// Throws when attempting to mix And with Or operators.
    /// </summary>
    public new AndContinuation<TValue> And => throw new MixedAndOrAssertionsException();

    public override async Task<TValue?> AssertAsync()
    {
        var currentScope = AssertionScope.GetCurrentAssertionScope();
        Exception? firstException = null;

        // Try first assertion - use ExecuteCoreAsync to avoid recursion
        if (currentScope != null)
        {
            // Inside Assert.Multiple - track exception count
            var exceptionCountBefore = currentScope.ExceptionCount;
            await _first.ExecuteCoreAsync();

            if (currentScope.ExceptionCount > exceptionCountBefore)
            {
                // First assertion failed - store the exception
                firstException = currentScope.GetLastException();
            }
            else
            {
                // First assertion passed - we're done
                return default;
            }
        }
        else
        {
            // Not in Assert.Multiple - use exception handling
            try
            {
                var result = await _first.ExecuteCoreAsync();
                // First passed - return success
                return result;
            }
            catch (AssertionException ex)
            {
                firstException = ex;
            }
        }

        // First failed, try second assertion - use ExecuteCoreAsync to avoid recursion
        if (currentScope != null)
        {
            var exceptionCountBefore = currentScope.ExceptionCount;
            await _second.ExecuteCoreAsync();

            if (currentScope.ExceptionCount > exceptionCountBefore)
            {
                // Both failed - build combined message
                var secondException = currentScope.GetLastException();

                // Remove both individual exceptions and add combined one
                currentScope.RemoveLastExceptions(2);

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
            else
            {
                // Second passed, but first failed - remove first failure
                currentScope.RemoveLastExceptions(1);
                return default;
            }
        }
        else
        {
            // Not in Assert.Multiple
            try
            {
                var result = await _second.ExecuteCoreAsync();
                // Second passed - return success (first failed but Or means at least one passes)
                return result;
            }
            catch (AssertionException ex)
            {
                // Both failed - throw combined exception
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
            return $"{firstExpectation}, {becausePrefix}{Environment.NewLine}or {secondExpectation}";
        }

        return $"{firstExpectation}{Environment.NewLine}or {secondExpectation}";
    }

    protected override string GetExpectation() => "either condition";
}
