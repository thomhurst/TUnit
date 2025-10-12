using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Assembly type using [GenerateAssertion] attributes.
/// These wrap Assembly signing and build configuration checks as extension methods.
/// </summary>
public static class AssemblyAssertionExtensions2
{
    [GenerateAssertion(ExpectationMessage = "to be signed")]
    public static bool IsSigned(this Assembly value)
    {
        var publicKeyToken = value.GetName().GetPublicKeyToken();
        return publicKeyToken != null && publicKeyToken.Length > 0;
    }

    [GenerateAssertion(ExpectationMessage = "to not be signed")]
    public static bool IsNotSigned(this Assembly value)
    {
        var publicKeyToken = value.GetName().GetPublicKeyToken();
        return publicKeyToken == null || publicKeyToken.Length == 0;
    }

    [GenerateAssertion(ExpectationMessage = "to be a debug build")]
    public static bool IsDebugBuild(this Assembly value)
    {
        var debuggableAttribute = value.GetCustomAttribute<DebuggableAttribute>();
        return debuggableAttribute != null && debuggableAttribute.IsJITTrackingEnabled;
    }

    [GenerateAssertion(ExpectationMessage = "to be a release build")]
    public static bool IsReleaseBuild(this Assembly value)
    {
        var debuggableAttribute = value.GetCustomAttribute<DebuggableAttribute>();
        return debuggableAttribute == null || !debuggableAttribute.IsJITTrackingEnabled;
    }
}
