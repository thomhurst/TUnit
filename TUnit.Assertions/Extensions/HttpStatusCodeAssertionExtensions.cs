using System.Net;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class HttpStatusCodeAssertionExtensions
{
    public static IsSuccessStatusCodeAssertion IsSuccess(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsSuccess()");
        return new IsSuccessStatusCodeAssertion(source.Context);
    }

    public static IsNotSuccessStatusCodeAssertion IsNotSuccess(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotSuccess()");
        return new IsNotSuccessStatusCodeAssertion(source.Context);
    }

    public static IsClientErrorStatusCodeAssertion IsClientError(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsClientError()");
        return new IsClientErrorStatusCodeAssertion(source.Context);
    }

    public static IsServerErrorStatusCodeAssertion IsServerError(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsServerError()");
        return new IsServerErrorStatusCodeAssertion(source.Context);
    }

    public static IsRedirectionStatusCodeAssertion IsRedirection(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsRedirection()");
        return new IsRedirectionStatusCodeAssertion(source.Context);
    }

    public static IsInformationalStatusCodeAssertion IsInformational(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsInformational()");
        return new IsInformationalStatusCodeAssertion(source.Context);
    }

    public static IsErrorStatusCodeAssertion IsError(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.Context.ExpressionBuilder.Append(".IsError()");
        return new IsErrorStatusCodeAssertion(source.Context);
    }
}
