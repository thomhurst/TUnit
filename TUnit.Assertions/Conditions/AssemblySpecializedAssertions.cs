using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

[AssertionExtension("IsCollectible")]
public class IsCollectibleAssertion : Assertion<Assembly>
{
    public IsCollectibleAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to be collectible";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
#if NETSTANDARD2_0
        return Task.FromResult(AssertionResult.Failed("IsCollectible is not supported in .NET Standard 2.0"));
#else
        if (metadata.Value!.IsCollectible)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be collectible, but it was not"));
#endif
    }
}

[AssertionExtension("IsNotCollectible")]
public class IsNotCollectibleAssertion : Assertion<Assembly>
{
    public IsNotCollectibleAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to not be collectible";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
#if NETSTANDARD2_0
        return Task.FromResult(AssertionResult.Failed("IsCollectible is not supported in .NET Standard 2.0"));
#else
        if (!metadata.Value!.IsCollectible)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be collectible, but it was"));
#endif
    }
}

[AssertionExtension("IsDynamic")]
public class IsDynamicAssertion : Assertion<Assembly>
{
    public IsDynamicAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to be dynamic";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
        if (metadata.Value!.IsDynamic)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be dynamic, but it was not"));
    }
}

[AssertionExtension("IsNotDynamic")]
public class IsNotDynamicAssertion : Assertion<Assembly>
{
    public IsNotDynamicAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to not be dynamic";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
        if (!metadata.Value!.IsDynamic)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be dynamic, but it was"));
    }
}

[AssertionExtension("IsFullyTrusted")]
public class IsFullyTrustedAssertion : Assertion<Assembly>
{
    public IsFullyTrustedAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to be fully trusted";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
#pragma warning disable SYSLIB0003 // IsFullyTrusted is obsolete
        if (metadata.Value!.IsFullyTrusted)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be fully trusted, but it was not"));
#pragma warning restore SYSLIB0003
    }
}

[AssertionExtension("IsNotFullyTrusted")]
public class IsNotFullyTrustedAssertion : Assertion<Assembly>
{
    public IsNotFullyTrustedAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to not be fully trusted";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
#pragma warning disable SYSLIB0003 // IsFullyTrusted is obsolete
        if (!metadata.Value!.IsFullyTrusted)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be fully trusted, but it was"));
#pragma warning restore SYSLIB0003
    }
}

[AssertionExtension("IsSigned")]
public class IsSignedAssertion : Assertion<Assembly>
{
    public IsSignedAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to be signed";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
        var publicKeyToken = metadata.Value!.GetName().GetPublicKeyToken();
        if (publicKeyToken != null && publicKeyToken.Length > 0)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be signed, but it was not"));
    }
}

[AssertionExtension("IsNotSigned")]
public class IsNotSignedAssertion : Assertion<Assembly>
{
    public IsNotSignedAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to not be signed";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
        var publicKeyToken = metadata.Value!.GetName().GetPublicKeyToken();
        if (publicKeyToken == null || publicKeyToken.Length == 0)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to not be signed, but it was"));
    }
}

[AssertionExtension("IsDebugBuild")]
public class IsDebugBuildAssertion : Assertion<Assembly>
{
    public IsDebugBuildAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to be a debug build";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
        var debuggableAttribute = metadata.Value!.GetCustomAttribute<DebuggableAttribute>();
        if (debuggableAttribute != null && debuggableAttribute.IsJITTrackingEnabled)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a debug build, but it was not"));
    }
}

[AssertionExtension("IsReleaseBuild")]
public class IsReleaseBuildAssertion : Assertion<Assembly>
{
    public IsReleaseBuildAssertion(AssertionContext<Assembly> context) : base(context) { }
    protected override string GetExpectation() => "to be a release build";
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Assembly> metadata)
    {
        var debuggableAttribute = metadata.Value!.GetCustomAttribute<DebuggableAttribute>();
        if (debuggableAttribute == null || !debuggableAttribute.IsJITTrackingEnabled)
            return Task.FromResult(AssertionResult.Passed);
        return Task.FromResult(AssertionResult.Failed($"Expected {Context.ExpressionBuilder} to be a release build, but it was not"));
    }
}
