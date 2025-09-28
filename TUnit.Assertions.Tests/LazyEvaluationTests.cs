using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Core;

namespace TUnit.Assertions.Tests;

public class LazyEvaluationTests
{
    [Test]
    public async Task ValuesShouldNotBeEvaluatedUntilAwaited()
    {
        // Arrange
        var evaluationCount = 0;
        Func<int> valueProvider = () =>
        {
            evaluationCount++;
            return 42;
        };

        // Act - Create assertion (should NOT evaluate)
        var assertion = new GenericEqualToAssertion<int>(valueProvider, 42);

        // Assert - Value should not be evaluated yet
        await Assert.That(evaluationCount).IsEqualTo(0);

        // Act - Now await the assertion
        await assertion;

        // Assert - Value should now be evaluated exactly once
        await Assert.That(evaluationCount).IsEqualTo(1);
    }

    [Test]
    public async Task StringAssertionShouldBeLazilyEvaluated()
    {
        // Arrange
        var evaluationCount = 0;
        Func<string> valueProvider = () =>
        {
            evaluationCount++;
            return "  HELLO WORLD  ";
        };

        // Act - Build assertion with configuration (should NOT evaluate)
        var assertion = new StringEqualToAssertion(valueProvider, "hello world")
            .WithTrimming()
            .IgnoringCase();

        // Assert - Value should not be evaluated yet
        await Assert.That(evaluationCount).IsEqualTo(0);

        // Act - Now await the assertion
        await assertion;

        // Assert - Value should now be evaluated exactly once
        await Assert.That(evaluationCount).IsEqualTo(1);
    }

    [Test]
    public async Task ChainedAssertionsShouldBeLazilyEvaluated()
    {
        // Arrange
        var evaluation1Count = 0;
        var evaluation2Count = 0;

        Func<int> valueProvider1 = () =>
        {
            evaluation1Count++;
            return 5;
        };

        Func<int> valueProvider2 = () =>
        {
            evaluation2Count++;
            return 10;
        };

        // Act - Create chained assertions (should NOT evaluate)
        var assertion1 = new GenericEqualToAssertion<int>(valueProvider1, 5);
        var assertion2 = new GenericEqualToAssertion<int>(valueProvider2, 10);

        var chainedAssertion = assertion1.And.Chain(assertion2);

        // Assert - Values should not be evaluated yet
        await Assert.That(evaluation1Count).IsEqualTo(0);
        await Assert.That(evaluation2Count).IsEqualTo(0);

        // Act - Now await the chained assertion
        await chainedAssertion;

        // Assert - Both values should now be evaluated
        await Assert.That(evaluation1Count).IsEqualTo(1);
        await Assert.That(evaluation2Count).IsEqualTo(1);
    }

    [Test]
    public async Task AsyncValueProviderShouldBeLazilyEvaluated()
    {
        // Arrange
        var evaluationCount = 0;
        Func<Task<string>> asyncValueProvider = async () =>
        {
            evaluationCount++;
            await Task.Delay(10); // Simulate async work
            return "async result";
        };

        // Act - Create assertion (should NOT evaluate)
        var assertion = new StringEqualToAssertion(asyncValueProvider, "async result");

        // Assert - Value should not be evaluated yet
        await Assert.That(evaluationCount).IsEqualTo(0);

        // Act - Now await the assertion
        await assertion;

        // Assert - Value should now be evaluated exactly once
        await Assert.That(evaluationCount).IsEqualTo(1);
    }

    [Test]
    public async Task OrChainShouldShortCircuitOnFirstSuccess()
    {
        // Arrange
        var evaluation1Count = 0;
        var evaluation2Count = 0;

        Func<int> valueProvider1 = () =>
        {
            evaluation1Count++;
            return 5;
        };

        Func<int> valueProvider2 = () =>
        {
            evaluation2Count++;
            return 10;
        };

        // Act - Create OR chained assertions
        var assertion1 = new GenericEqualToAssertion<int>(valueProvider1, 5); // This will pass
        var assertion2 = new GenericEqualToAssertion<int>(valueProvider2, 999); // This won't be evaluated

        var chainedAssertion = assertion1.Or.Chain(assertion2);

        // Act - Await the chained assertion
        await chainedAssertion;

        // Assert - First value evaluated, second should not be due to short-circuit
        await Assert.That(evaluation1Count).IsEqualTo(1);
        await Assert.That(evaluation2Count).IsEqualTo(0); // Short-circuited!
    }

    [Test]
    public async Task AndChainShouldShortCircuitOnFirstFailure()
    {
        // Arrange
        var evaluation1Count = 0;
        var evaluation2Count = 0;

        Func<int> valueProvider1 = () =>
        {
            evaluation1Count++;
            return 5;
        };

        Func<int> valueProvider2 = () =>
        {
            evaluation2Count++;
            return 10;
        };

        // Act - Create AND chained assertions
        var assertion1 = new GenericEqualToAssertion<int>(valueProvider1, 999); // This will fail
        var assertion2 = new GenericEqualToAssertion<int>(valueProvider2, 10); // This won't be evaluated

        var chainedAssertion = assertion1.And.Chain(assertion2);

        // Act - Try to await the chained assertion (should throw)
        try
        {
            await chainedAssertion;
        }
        catch
        {
            // Expected to fail
        }

        // Assert - First value evaluated, second should not be due to short-circuit
        await Assert.That(evaluation1Count).IsEqualTo(1);
        await Assert.That(evaluation2Count).IsEqualTo(0); // Short-circuited!
    }
}