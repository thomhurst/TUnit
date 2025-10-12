using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Assembly type using [AssertionFrom&lt;Assembly&gt;] and [GenerateAssertion] attributes.
/// </summary>
#if !NETSTANDARD2_0
[AssertionFrom<Assembly>(nameof(Assembly.IsCollectible), ExpectationMessage = "be collectible")]
[AssertionFrom<Assembly>(nameof(Assembly.IsCollectible), CustomName = "IsNotCollectible", NegateLogic = true, ExpectationMessage = "be collectible")]
#endif

[AssertionFrom<Assembly>(nameof(Assembly.IsDynamic), ExpectationMessage = "be dynamic")]
[AssertionFrom<Assembly>(nameof(Assembly.IsDynamic), CustomName = "IsNotDynamic", NegateLogic = true, ExpectationMessage = "be dynamic")]

[AssertionFrom<Assembly>(nameof(Assembly.IsFullyTrusted), ExpectationMessage = "be fully trusted")]
[AssertionFrom<Assembly>(nameof(Assembly.IsFullyTrusted), CustomName = "IsNotFullyTrusted", NegateLogic = true, ExpectationMessage = "be fully trusted")]
public static partial class AssemblyAssertionExtensions
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
