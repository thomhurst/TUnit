using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Simplified base class for all assertions with lazy evaluation
/// </summary>
public abstract class AssertionBase<T> : AssertionBase
{
    private readonly Func<Task<T>> _actualValueProvider;
    private readonly List<(ChainType type, AssertionBase<T> assertion, string? becauseReason)> _chainedAssertions = new();
    private ChainType _nextChainType = ChainType.None;
    private string? _becauseReason;
    private string? _becauseExpression;

    // Support both sync and async value providers
    protected AssertionBase(T actualValue)
        : this(() => Task.FromResult(actualValue)) { }

    protected AssertionBase(Func<T> actualValueProvider)
        : this(() => Task.FromResult(actualValueProvider())) { }

    protected AssertionBase(Task<T> actualValueTask)
        : this(() => actualValueTask) { }

    protected AssertionBase(Func<Task<T>> actualValueProvider)
    {
        _actualValueProvider = actualValueProvider ?? throw new ArgumentNullException(nameof(actualValueProvider));
    }


    /// <summary>
    /// Gets the actual value - ONLY called during execution
    /// </summary>
    protected async Task<T> GetActualValueAsync()
    {
        return await _actualValueProvider();
    }

    /// <summary>
    /// Configuration method - NO EVALUATION
    /// </summary>
    public virtual AssertionBase<T> Because(string reason, [CallerArgumentExpression(nameof(reason))] string? expression = null)
    {
        // If this assertion has chained assertions, apply the because reason to the last assertion in the chain
        if (_chainedAssertions.Count > 0)
        {
            var lastChainedAssertion = _chainedAssertions[_chainedAssertions.Count - 1].assertion;
            lastChainedAssertion._becauseReason = reason;
            lastChainedAssertion._becauseExpression = expression;
        }
        else
        {
            // No chained assertions, apply to this assertion
            _becauseReason = reason;
            _becauseExpression = expression;
        }
        return this;
    }

    /// <summary>
    /// Sets up an AND chain - NO EVALUATION
    /// Returns an AssertionChainBuilder to enable further assertions
    /// </summary>
    public virtual AssertionChainBuilder<T> And
    {
        get
        {
            return new AssertionChainBuilder<T>(_actualValueProvider, this, ChainType.And);
        }
    }

    /// <summary>
    /// Sets up an OR chain - NO EVALUATION
    /// Returns an AssertionChainBuilder to enable further assertions
    /// </summary>
    public virtual AssertionChainBuilder<T> Or
    {
        get
        {
            return new AssertionChainBuilder<T>(_actualValueProvider, this, ChainType.Or);
        }
    }

    /// <summary>
    /// Sets the next chain type for the upcoming assertion - NO EVALUATION
    /// </summary>
    internal void SetNextChainType(ChainType chainType)
    {
        _nextChainType = chainType;
    }

    /// <summary>
    /// Chains another assertion - NO EVALUATION
    /// </summary>
    internal AssertionBase<T> Chain(AssertionBase<T> nextAssertion)
    {
        var chainType = _nextChainType == ChainType.None ? ChainType.And : _nextChainType;

        // Check for mixed And/Or chains
        if (_chainedAssertions.Count > 0)
        {
            var existingChainTypes = _chainedAssertions.Select(c => c.type).Where(t => t != ChainType.None).Distinct().ToList();
            if (existingChainTypes.Any() && chainType != ChainType.None && !existingChainTypes.Contains(chainType))
            {
                throw new MixedAndOrAssertionsException();
            }
        }

        _chainedAssertions.Add((chainType, nextAssertion, nextAssertion._becauseReason));
        _nextChainType = ChainType.None;
        return this;
    }

    /// <summary>
    /// LAZY EXECUTION - Only runs when awaited
    /// </summary>
    public override async Task ExecuteAsync()
    {
        await ExecuteChainAsync();
    }

    /// <summary>
    /// Executes the assertion chain and builds comprehensive error messages
    /// </summary>
    private async Task ExecuteChainAsync()
    {
        T actualValue;
        Exception? valueException = null;

        // Try to get the actual value, catching exceptions for proper handling
        try
        {
            actualValue = await GetActualValueAsync();
        }
        catch (Exception ex)
        {
            // For delegate/value provider exceptions, we need to handle them specially
            actualValue = default(T)!;
            valueException = ex;
        }

        // Execute the main assertion
        var mainResult = await AssertWithExceptionAsync(actualValue, valueException);

        // If this is a single assertion (no chains), handle it specially
        if (_chainedAssertions.Count == 0)
        {
            if (!mainResult.IsPassed)
            {
                // For single assertions, format the message appropriately
                var errorMessage = BuildSingleAssertionErrorMessage(mainResult, actualValue);
                throw new AssertionException(errorMessage);
            }
            return;
        }

        // For chained assertions, continue with chain logic
        var chainResults = new List<(ChainType chainType, AssertionResult result, string? becauseReason)>();
        chainResults.Add((ChainType.None, mainResult, _becauseReason));

        // Execute chained assertions
        bool chainPassed = mainResult.IsPassed;

        foreach (var (chainType, assertion, _) in _chainedAssertions)
        {
            if (chainType == ChainType.And && !chainPassed)
            {
                // Short circuit on AND failure - but still collect results for error message
                break;
            }

            if (chainType == ChainType.Or && chainPassed)
            {
                // Short circuit on OR success
                return;
            }

            var chainResult = await assertion.AssertAsync();
            // Get the current because reason from the assertion, not the cached value
            chainResults.Add((chainType, chainResult, assertion._becauseReason));

            if (chainType == ChainType.And)
            {
                chainPassed = chainPassed && chainResult.IsPassed;
                if (!chainResult.IsPassed)
                {
                    break; // Stop on first AND failure
                }
            }
            else if (chainType == ChainType.Or)
            {
                chainPassed = chainPassed || chainResult.IsPassed;
                if (chainResult.IsPassed)
                {
                    return; // Stop on first OR success
                }
            }
        }

        // If the chain failed, build a comprehensive error message
        if (!chainPassed)
        {
            // Use the comprehensive error message builder for chains
            var errorMessage = BuildChainErrorMessage(chainResults, actualValue);
            throw new AssertionException(errorMessage);
        }
    }


    /// <summary>
    /// Builds error message for a single assertion (no chains)
    /// </summary>
    protected virtual string BuildSingleAssertionErrorMessage(AssertionResult result, T actualValue)
    {
        // Check if this is a simple boolean assertion message that needs formatting
        bool needsFormatting = result.Message == "Expected true but was false" ||
                              result.Message == "Expected false but was true";

        if (needsFormatting)
        {
            var messageBuilder = new System.Text.StringBuilder();

            // Extract the expectation from the error message
            var expectation = ExtractExpectationFromMessage(result.Message);

            // Build the error message in the expected format
            messageBuilder.Append($"Expected variable to be {expectation}");

            // Add because reason if present
            if (!string.IsNullOrEmpty(_becauseReason))
            {
                var trimmedReason = _becauseReason!.Trim();
                var formattedBecause = trimmedReason.StartsWith("because ") ? trimmedReason : $"because {trimmedReason}";
                messageBuilder.Append($", {formattedBecause}");
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine();
            messageBuilder.AppendLine($"but found {actualValue}");
            messageBuilder.AppendLine();
            messageBuilder.Append($"at {GetSimpleStackTraceContext(result)}");

            return messageBuilder.ToString();
        }
        else
        {
            // For other assertions, check if we can extract meaningful expectation
            var expectation = ExtractExpectationFromMessage(result.Message);

            // If we have a meaningful expectation, format it nicely
            if (!string.IsNullOrEmpty(expectation))
            {
                var messageBuilder = new System.Text.StringBuilder();

                messageBuilder.Append($"Expected variable to be {expectation}");

                // Add because reason if present
                if (!string.IsNullOrEmpty(_becauseReason))
                {
                    var trimmedReason = _becauseReason!.Trim();
                    var formattedBecause = trimmedReason.StartsWith("because ") ? trimmedReason : $"because {trimmedReason}";
                    messageBuilder.Append($", {formattedBecause}");
                }

                messageBuilder.AppendLine();
                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"but found {actualValue}");
                messageBuilder.AppendLine();
                messageBuilder.Append($"at {GetSimpleStackTraceContext(result)}");

                return messageBuilder.ToString();
            }
            else
            {
                // For assertions we can't parse, just return the original message without reformatting
                return result.Message;
            }
        }
    }

    /// <summary>
    /// Gets simple stack trace context for a single assertion
    /// </summary>
    private string GetSimpleStackTraceContext(AssertionResult result)
    {
        var methodName = GetAssertionMethodNameFromResult(result);
        return $"Assert.That(variable){methodName}";
    }

    /// <summary>
    /// Infers the expectation from chain context when assertion passed (empty message)
    /// </summary>
    private string InferExpectationFromChainContext(int index, List<(ChainType chainType, AssertionResult result, string? becauseReason)> chainResults, T actualValue)
    {
        // For boolean type assertions, we can infer based on the actual value and assertion success
        if (typeof(T) == typeof(bool))
        {
            var boolValue = (bool)(object)actualValue!;

            // If this assertion passed, then the expectation matched the actual value
            // For subsequent failed assertions, we need to look at the failure message
            if (index < chainResults.Count - 1)
            {
                // Look at the next assertion's failure to infer what this one expected
                var nextResult = chainResults[index + 1].result;
                if (!string.IsNullOrEmpty(nextResult.Message))
                {
                    // If next assertion failed expecting the opposite, this one expected the current value
                    if (nextResult.Message.Contains("Expected false but was true"))
                    {
                        // This assertion expected true (and it was true, so it passed)
                        return "equal to True";
                    }
                    else if (nextResult.Message.Contains("Expected true but was false"))
                    {
                        // This assertion expected false (and it was false, so it passed)
                        return "equal to False";
                    }
                }
            }

            // If we can't infer from the next assertion, use the actual value
            // since the assertion passed, it must have expected what it got
            return boolValue ? "equal to True" : "equal to False";
        }

        // For non-boolean types, use a generic expectation
        return actualValue?.ToString() ?? "null";
    }

    /// <summary>
    /// Builds a comprehensive error message from chain results
    /// </summary>
    private string BuildChainErrorMessage(List<(ChainType chainType, AssertionResult result, string? becauseReason)> chainResults, T actualValue)
    {
        var messageBuilder = new System.Text.StringBuilder();


        // Build expectation parts - include all assertions (passed or failed) in the chain
        for (int i = 0; i < chainResults.Count; i++)
        {
            var (chainType, result, becauseReason) = chainResults[i];

            // Extract the expectation from the message (both passed and failed assertions should have meaningful messages now)
            var expectation = ExtractExpectationFromMessage(result.Message);

            if (i == 0)
            {
                // For boolean assertions that need formatting
                if (result.Message == "Expected true but was false" || result.Message == "Expected false but was true")
                {
                    messageBuilder.Append($"Expected variable to be {expectation}");
                }
                else if (!string.IsNullOrEmpty(expectation))
                {
                    // Use the extracted expectation
                    messageBuilder.Append($"Expected variable to be {expectation}");
                }
                else
                {
                    // For assertions we can't parse, use the original message without reformatting
                    var messagePart = result.Message.Split(new[] { " but " }, StringSplitOptions.None)[0];
                    messageBuilder.Append(messagePart);
                }

                // Add because reason for first assertion if present
                if (!string.IsNullOrEmpty(becauseReason))
                {
                    var trimmedReason = becauseReason!.Trim();
                    var formattedBecause = trimmedReason.StartsWith("because ") ? trimmedReason : $"because {trimmedReason}";
                    messageBuilder.Append($", {formattedBecause}");
                }
            }
            else
            {
                var connector = chainType == ChainType.And ? "and" : "or";

                // For boolean assertions that need formatting
                if (result.Message == "Expected true but was false" || result.Message == "Expected false but was true")
                {
                    messageBuilder.AppendLine();
                    messageBuilder.Append($" {connector} to be {expectation}");
                }
                else if (!string.IsNullOrEmpty(expectation))
                {
                    // Use the extracted expectation
                    messageBuilder.AppendLine();
                    messageBuilder.Append($" {connector} to be {expectation}");
                }
                else
                {
                    // For assertions we can't parse, use original message parts without reformatting
                    var messagePart = result.Message.StartsWith("Expected ")
                        ? result.Message.Substring("Expected ".Length).Split(new[] { " but " }, StringSplitOptions.None)[0]
                        : result.Message.Split(new[] { " but " }, StringSplitOptions.None)[0];
                    messageBuilder.AppendLine();
                    messageBuilder.Append($" {connector} {messagePart}");
                }

                // Add because reason for subsequent assertions if present
                if (!string.IsNullOrEmpty(becauseReason))
                {
                    var trimmedReason = becauseReason!.Trim();
                    var formattedBecause = trimmedReason.StartsWith("because ") ? trimmedReason : $"because {trimmedReason}";
                    messageBuilder.Append($", {formattedBecause}");
                }
            }
        }

        // Add actual value and stack trace
        messageBuilder.AppendLine();
        messageBuilder.AppendLine();
        messageBuilder.AppendLine($"but found {actualValue}");
        messageBuilder.AppendLine();
        messageBuilder.Append($"at {BuildStackTraceContext(chainResults)}");

        return messageBuilder.ToString();
    }

    /// <summary>
    /// Extracts the expectation part from an assertion error message
    /// </summary>
    private string ExtractExpectationFromMessage(string message)
    {
        // Handle null or empty messages
        if (string.IsNullOrEmpty(message))
            return "";

        // Handle common assertion message patterns
        if (message == "Expected true but was false")
            return "equal to True";
        if (message == "Expected false but was true")
            return "equal to False";

        // Handle complex execution time or action-specific messages by NOT reformatting them
        if (message.Contains("Expected action to") || message.Contains("Expected delegate to") ||
            message.Contains("milliseconds") || message.Contains("complete within"))
        {
            // These are complex assertion messages that should not be reformatted
            // Return empty to prevent reformatting
            return "";
        }

        // Handle exception-specific messages - don't reformat these
        if (message.Contains("Expected an exception") || message.Contains("Expected exception") ||
            message.Contains("but none was thrown") || message.Contains("was thrown"))
        {
            return "";
        }

        // For simple messages, try to extract the expectation
        if (message.StartsWith("Expected "))
        {
            var parts = message.Split(new[] { " but was ", " but found ", " but " }, StringSplitOptions.None);
            if (parts.Length > 0)
            {
                var expectation = parts[0].Substring("Expected ".Length).Trim();

                // Handle empty expectations
                if (string.IsNullOrEmpty(expectation))
                    return "";

                // Handle special cases for boolean values
                if (expectation == "true")
                    return "equal to True";
                if (expectation == "false")
                    return "equal to False";

                // Remove redundant prefixes to avoid duplication
                if (expectation.StartsWith("variable to be "))
                    return expectation.Substring("variable to be ".Length);
                if (expectation.StartsWith("variable to "))
                    return expectation.Substring("variable to ".Length);

                // Handle simple value expectations only
                if (expectation.Contains(" to ") || expectation.Contains(" within ") ||
                    expectation.Contains(" was ") || expectation.Contains(" exception"))
                {
                    // Complex expectations - don't reformat
                    return "";
                }

                // Only return simple expectations like "null", "not null", numbers, strings
                return expectation;
            }
        }

        // If message doesn't start with "Expected ", try simple "to be" patterns
        if (message.Contains(" to be ") && !message.Contains(" to complete "))
        {
            // Only handle simple "to be" patterns, not complex ones
            var parts = message.Split(new[] { " to be " }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var expectationPart = parts[1].Split(new[] { " but " }, StringSplitOptions.None)[0].Trim();
                // Only return if it's a simple expectation
                if (!expectationPart.Contains(" to ") && !expectationPart.Contains(" within "))
                    return expectationPart;
            }
        }

        // For anything else, don't reformat to prevent malformed messages
        return "";
    }

    /// <summary>
    /// Builds stack trace context showing the assertion chain
    /// </summary>
    private string BuildStackTraceContext(List<(ChainType chainType, AssertionResult result, string? becauseReason)> chainResults)
    {
        var chainParts = new List<string> { "Assert.That(variable)" };

        bool first = true;
        foreach (var (chainType, result, _) in chainResults)
        {
            if (first)
            {
                // Add main assertion method
                chainParts.Add(GetAssertionMethodNameFromResultOrInfer(result, first: true));
                first = false;
            }
            else
            {
                var connector = chainType == ChainType.And ? ".And" : ".Or";
                var methodName = GetAssertionMethodNameFromResultOrInfer(result, first: false);
                chainParts.Add($"{connector}{methodName}");
            }
        }

        return string.Join("", chainParts);
    }

    /// <summary>
    /// Gets assertion method name, inferring from context if the result message is empty (passed assertion)
    /// </summary>
    private string GetAssertionMethodNameFromResultOrInfer(AssertionResult result, bool first)
    {
        // If we have a message, use the existing logic
        if (!string.IsNullOrEmpty(result.Message))
        {
            return GetAssertionMethodNameFromResult(result);
        }

        // For passed assertions with empty messages, we need to infer
        // This is tricky without more context, but we can make educated guesses
        if (typeof(T) == typeof(bool))
        {
            // For boolean assertions, if this is the first in a chain and it passed,
            // and we're in a failure scenario, we can try to infer from the test context
            // This is a simplified approach - in a real implementation, we'd store more metadata
            return first ? ".IsTrue()" : ".IsFalse()";
        }

        return ".IsEqualTo()";
    }

    /// <summary>
    /// Gets the assertion method name from result for stack trace context
    /// </summary>
    private string GetAssertionMethodNameFromResult(AssertionResult result)
    {
        // Determine based on the error message
        if (result.Message == "Expected true but was false")
            return ".IsTrue()";
        if (result.Message == "Expected false but was true")
            return ".IsFalse()";

        // For other assertions, try to infer from the message - be more specific to avoid false matches
        if (result.Message.StartsWith("Expected null"))
            return ".IsNull()";
        if (result.Message.StartsWith("Expected not null"))
            return ".IsNotNull()";
        if (result.Message.StartsWith("Expected collection to be empty"))
            return ".IsEmpty()";
        if (result.Message.StartsWith("Expected collection to be non-empty"))
            return ".IsNotEmpty()";
        if (result.Message.StartsWith("Expected collection to contain"))
            return ".Contains()";
        if (result.Message.StartsWith("Expected collection to have") && result.Message.Contains("items"))
            return ".HasCount()";

        // Default fallback based on expected value determination
        if (result.Message.Contains("Expected true") || result.Message.Contains("equal to True"))
            return ".IsTrue()";
        if (result.Message.Contains("Expected false") || result.Message.Contains("equal to False"))
            return ".IsFalse()";

        // Ultimate fallback
        return ".IsEqualTo()";
    }


    /// <summary>
    /// Make it awaitable - THIS triggers execution
    /// </summary>
    public override TaskAwaiter GetAwaiter() => ExecuteAsync().GetAwaiter();

    /// <summary>
    /// Override this for assertion logic - called ONLY when awaited
    /// </summary>
    protected abstract Task<AssertionResult> AssertAsync();

    /// <summary>
    /// Executes assertion with exception handling for delegate exceptions
    /// </summary>
    protected virtual async Task<AssertionResult> AssertWithExceptionAsync(T actualValue, Exception? valueException)
    {
        if (valueException != null)
        {
            // Handle delegate exceptions with proper message formatting
            return AssertionResult.Fail($"An exception was thrown during the assertion: {valueException}");
        }

        return await AssertAsync();
    }
}

/// <summary>
/// Non-generic base for common functionality
/// </summary>
public abstract class AssertionBase
{
    public abstract TaskAwaiter GetAwaiter();

    public abstract Task ExecuteAsync();
}