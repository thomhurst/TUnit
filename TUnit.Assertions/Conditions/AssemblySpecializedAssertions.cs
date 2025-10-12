using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

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
