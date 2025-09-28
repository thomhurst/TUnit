using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Demonstration of the simplified assertions API
/// </summary>
public class SimplifiedApiDemo
{
    [Test]
    public async Task SimplifiedApiExample()
    {
        // Direct value assertion - simple and clean
        await new StringEqualToAssertion("hello", "hello");

        // With configuration - fluent and intuitive
        await new StringEqualToAssertion("  HELLO  ", "hello")
            .WithTrimming()
            .IgnoringCase();

        // Lazy evaluation - value is only computed when awaited
        var expensiveComputation = new Func<string>(() =>
        {
            Console.WriteLine("Computing expensive value...");
            return "result";
        });

        var assertion = new StringEqualToAssertion(expensiveComputation, "result");
        // At this point, expensiveComputation has NOT been called yet

        await assertion; // NOW it gets called

        // Generic assertions work the same way
        await new GenericEqualToAssertion<int>(42, 42);

        // With tolerance for numeric types
#if NET
        await new GenericEqualToAssertion<double>(3.14159, 3.14)
            .Within(0.01);
#endif

        // Chaining assertions with And/Or
        var value = 5;
        var assertion1 = new GenericEqualToAssertion<int>(() => value, 5);
        var assertion2 = new GenericEqualToAssertion<int>(() => value * 2, 10);

        // Both must pass
        await assertion1.And.Chain(assertion2);

        // Custom comparisons
        await new GenericEqualToAssertion<string>("Hello", "HELLO")
            .WithComparison((a, b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task ShowingHowSimpleItIsToAddNewAssertion()
    {
        // Here's how easy it is to create a custom assertion:
        var assertion = new ContainsSubstringAssertion("Hello World", "World");
        await assertion;

        // With configuration
        await new ContainsSubstringAssertion("Hello World", "WORLD")
            .IgnoringCase();
    }
}

/// <summary>
/// Example of how simple it is to create a new assertion type
/// </summary>
public class ContainsSubstringAssertion : AssertionBase<string>
{
    private readonly string _substring;
    private StringComparison _comparison = StringComparison.Ordinal;

    public ContainsSubstringAssertion(string value, string substring)
        : base(value)
    {
        _substring = substring;
    }

    public ContainsSubstringAssertion(Func<string> valueProvider, string substring)
        : base(valueProvider)
    {
        _substring = substring;
    }

    public ContainsSubstringAssertion IgnoringCase()
    {
        _comparison = StringComparison.OrdinalIgnoreCase;
        return this;
    }

    // This is ALL you need to implement!
    protected override async Task<TUnit.Assertions.AssertConditions.AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual?.Contains(_substring, _comparison) == true)
        {
            return TUnit.Assertions.AssertConditions.AssertionResult.Passed;
        }

        return TUnit.Assertions.AssertConditions.AssertionResult.Fail(
            $"Expected string to contain '{_substring}' but was '{actual}'");
    }
}