using System.Net;
using System.Text;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

[AssertionExtension("IsSuccessStatusCode")]
public class IsSuccessStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsSuccessStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode < 200 || statusCode >= 300)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a success status code (2xx)";
}

[AssertionExtension("IsNotSuccessStatusCode")]
public class IsNotSuccessStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsNotSuccessStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode >= 200 && statusCode < 300)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to not be a success status code";
}

[AssertionExtension("IsClientErrorStatusCode")]
public class IsClientErrorStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsClientErrorStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode < 400 || statusCode >= 500)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a client error status code (4xx)";
}

[AssertionExtension("IsServerErrorStatusCode")]
public class IsServerErrorStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsServerErrorStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode < 500 || statusCode >= 600)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a server error status code (5xx)";
}

[AssertionExtension("IsRedirectionStatusCode")]
public class IsRedirectionStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsRedirectionStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode < 300 || statusCode >= 400)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be a redirection status code (3xx)";
}

[AssertionExtension("IsInformationalStatusCode")]
public class IsInformationalStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsInformationalStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode < 100 || statusCode >= 200)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be an informational status code (1xx)";
}

[AssertionExtension("IsErrorStatusCode")]
public class IsErrorStatusCodeAssertion : Assertion<HttpStatusCode>
{
    public IsErrorStatusCodeAssertion(
        AssertionContext<HttpStatusCode> context)
        : base(context)
    {
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<HttpStatusCode> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}"));
        }

        var statusCode = (int)value;
        if (statusCode < 400 || statusCode >= 600)
        {
            return Task.FromResult(AssertionResult.Failed($"status code was {statusCode} ({value})"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be an error status code (4xx or 5xx)";
}
