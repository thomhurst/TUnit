using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Assembly type using [AssertionFrom&lt;Assembly&gt;] and [GenerateAssertion] attributes.
/// </summary>
#if NET5_0_OR_GREATER
[AssertionFrom<Assembly>(nameof(Assembly.IsCollectible), ExpectationMessage = "be collectible")]
[AssertionFrom<Assembly>(nameof(Assembly.IsCollectible), CustomName = "IsNotCollectible", NegateLogic = true, ExpectationMessage = "be collectible")]
#endif

[AssertionFrom<Assembly>(nameof(Assembly.IsDynamic), ExpectationMessage = "be dynamic")]
[AssertionFrom<Assembly>(nameof(Assembly.IsDynamic), CustomName = "IsNotDynamic", NegateLogic = true, ExpectationMessage = "be dynamic")]

[AssertionFrom<Assembly>(nameof(Assembly.IsFullyTrusted), ExpectationMessage = "be fully trusted")]
[AssertionFrom<Assembly>(nameof(Assembly.IsFullyTrusted), CustomName = "IsNotFullyTrusted", NegateLogic = true, ExpectationMessage = "be fully trusted")]
public static partial class AssemblyAssertionExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be signed")]
    public static bool IsSigned(this Assembly value) => value.GetName().GetPublicKeyToken() is { Length: > 0 };

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to not be signed")]
    public static bool IsNotSigned(this Assembly value) => value.GetName().GetPublicKeyToken() is null or { Length: 0 };

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a debug build")]
    public static bool IsDebugBuild(this Assembly value) => value.GetCustomAttribute<DebuggableAttribute>() is { IsJITTrackingEnabled: true };

    [EditorBrowsable(EditorBrowsableState.Never)]
    [GenerateAssertion(ExpectationMessage = "to be a release build")]
    public static bool IsReleaseBuild(this Assembly value) => value.GetCustomAttribute<DebuggableAttribute>() is not { IsJITTrackingEnabled: true };
}
