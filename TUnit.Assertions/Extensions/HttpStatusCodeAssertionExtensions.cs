using System.Net;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

public static class HttpStatusCodeAssertionExtensions
{
    public static IsSuccessStatusCodeAssertion IsSuccess(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsSuccess()");
        return new IsSuccessStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsNotSuccessStatusCodeAssertion IsNotSuccess(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsNotSuccess()");
        return new IsNotSuccessStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsClientErrorStatusCodeAssertion IsClientError(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsClientError()");
        return new IsClientErrorStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsServerErrorStatusCodeAssertion IsServerError(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsServerError()");
        return new IsServerErrorStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsRedirectionStatusCodeAssertion IsRedirection(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsRedirection()");
        return new IsRedirectionStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsInformationalStatusCodeAssertion IsInformational(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsInformational()");
        return new IsInformationalStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }

    public static IsErrorStatusCodeAssertion IsError(
        this IAssertionSource<HttpStatusCode> source)
    {
        source.ExpressionBuilder.Append(".IsError()");
        return new IsErrorStatusCodeAssertion(source.Context, source.ExpressionBuilder);
    }
}
