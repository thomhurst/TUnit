using System.Reflection;
using TUnit.Assertions.Conditions;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Extensions;

/// <summary>
/// Assembly-specific assertion extension methods.
/// </summary>
public static class AssemblyAssertionExtensions
{
    public static IsCollectibleAssertion IsCollectible(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsCollectible()");
        return new IsCollectibleAssertion(source.Context);
    }

    public static IsNotCollectibleAssertion IsNotCollectible(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotCollectible()");
        return new IsNotCollectibleAssertion(source.Context);
    }

    public static IsDynamicAssertion IsDynamic(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsDynamic()");
        return new IsDynamicAssertion(source.Context);
    }

    public static IsNotDynamicAssertion IsNotDynamic(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotDynamic()");
        return new IsNotDynamicAssertion(source.Context);
    }

    public static IsFullyTrustedAssertion IsFullyTrusted(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsFullyTrusted()");
        return new IsFullyTrustedAssertion(source.Context);
    }

    public static IsNotFullyTrustedAssertion IsNotFullyTrusted(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotFullyTrusted()");
        return new IsNotFullyTrustedAssertion(source.Context);
    }

    public static IsSignedAssertion IsSigned(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsSigned()");
        return new IsSignedAssertion(source.Context);
    }

    public static IsNotSignedAssertion IsNotSigned(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsNotSigned()");
        return new IsNotSignedAssertion(source.Context);
    }

    public static IsDebugBuildAssertion IsDebugBuild(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsDebugBuild()");
        return new IsDebugBuildAssertion(source.Context);
    }

    public static IsReleaseBuildAssertion IsReleaseBuild(this IAssertionSource<Assembly> source)
    {
        source.Context.ExpressionBuilder.Append(".IsReleaseBuild()");
        return new IsReleaseBuildAssertion(source.Context);
    }
}
