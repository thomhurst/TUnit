using System.Net;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

public class IsSuccessStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsSuccessStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode < 200 || statusCode >= 300)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a success status code (2xx)";
}

public class IsNotSuccessStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsNotSuccessStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode >= 200 && statusCode < 300)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be a success status code";
}

public class IsClientErrorStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsClientErrorStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode < 400 || statusCode >= 500)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a client error status code (4xx)";
}

public class IsServerErrorStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsServerErrorStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode < 500 || statusCode >= 600)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a server error status code (5xx)";
}

public class IsRedirectionStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsRedirectionStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode < 300 || statusCode >= 400)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a redirection status code (3xx)";
}

public class IsInformationalStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsInformationalStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode < 100 || statusCode >= 200)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be an informational status code (1xx)";
}

public class IsErrorStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsErrorStatusCodeAssertion(
        EvaluationContext<HttpStatusCode> context,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
    }

    protected override Task<AssertionResult> CheckAsync(HttpStatusCode value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));

        var statusCode = (int)value;
        if (statusCode < 400 || statusCode >= 600)
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be an error status code (4xx or 5xx)";
}
