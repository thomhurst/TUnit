using System.Net;
#if !NET472
using System.Net.Http.Json;
#endif
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;
using TUnit.Assertions.Sources;

namespace TUnit.Assertions.Tests;

public class AsyncMapTests
{
#if !NET472
    [Test]
    public async Task Map_WithAsyncMapper_HttpResponseExample()
    {
        // Arrange
        var json = """{"title":"Test Error","status":400}""";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        // Act & Assert - Using custom assertion with async Map
        await Assert.That(response)
            .ToProblemDetails()
            .And.Satisfies(pd => pd.Title == "Test Error")
            .And.Satisfies(pd => pd.Status == 400);
    }

    [Test]
    public async Task Map_WithAsyncMapper_ComplexObjectTransformation()
    {
        // Arrange
        var container = new Container { Data = "42" };

        // Act & Assert - Using custom assertion with async Map
        await Assert.That(container)
            .ToIntValue()
            .And.IsEqualTo(42);
    }

    [Test]
    public async Task Map_WithAsyncMapper_PropagatesExceptions()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "text/plain")
        };

        // Act & Assert - Exception during mapping should fail the assertion
        await Assert.ThrowsAsync<TUnit.Assertions.Exceptions.AssertionException>(async () =>
        {
            await Assert.That(response).ToProblemDetails();
        });
    }
#endif

    [Test]
    public async Task Map_WithAsyncMapper_SyncCode()
    {
        // Arrange
        var container = new Container { Data = "100" };

        // Act & Assert - Test async Map even with synchronous operation
        await Assert.That(container)
            .ToIntValue()
            .And.IsGreaterThan(50);
    }

    public record TestProblemDetails
    {
        public string? Title { get; init; }
        public int Status { get; init; }
    }

    public class Container
    {
        public string? Data { get; init; }
    }
}

// Extension methods for custom assertions
public static class AsyncMapTestExtensions
{
#if !NET472
    public static ToProblemDetailsAssertion ToProblemDetails(
        this IAssertionSource<HttpResponseMessage> source)
    {
        source.Context.ExpressionBuilder.Append(".ToProblemDetails()");
        return new ToProblemDetailsAssertion(source.Context);
    }
#endif

    public static ToIntValueAssertion ToIntValue(
        this IAssertionSource<AsyncMapTests.Container> source)
    {
        source.Context.ExpressionBuilder.Append(".ToIntValue()");
        return new ToIntValueAssertion(source.Context);
    }
}

#if !NET472
// Custom assertion using async Map
public class ToProblemDetailsAssertion : Assertion<AsyncMapTests.TestProblemDetails>
{
    public ToProblemDetailsAssertion(AssertionContext<HttpResponseMessage> context)
        : base(context.Map<AsyncMapTests.TestProblemDetails>(async response =>
        {
            var content = await response.Content.ReadFromJsonAsync<AsyncMapTests.TestProblemDetails>();
            if (content is null)
            {
                throw new InvalidOperationException("Response body is not Problem Details");
            }
            return content;
        }))
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<AsyncMapTests.TestProblemDetails> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(metadata.Exception.Message));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation()
    {
        return "HTTP response to be in the format of a Problem Details object";
    }
}
#endif

// Custom assertion for testing async transformation with sync parsing
public class ToIntValueAssertion : Assertion<int>
{
    public ToIntValueAssertion(AssertionContext<AsyncMapTests.Container> context)
        : base(context.Map<int>(async container =>
        {
            var timeProvider = TimeProviderContext.Current;
            await timeProvider.Delay(TimeSpan.FromMilliseconds(1)); // Simulate async work
            if (container?.Data is null)
            {
                throw new InvalidOperationException("Container data is null");
            }
            return int.Parse(container.Data);
        }))
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<int> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(metadata.Exception.Message));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation()
    {
        return "Container data to be parseable as an integer";
    }
}
