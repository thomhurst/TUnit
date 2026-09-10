using System.Diagnostics;
using System.Reflection;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Source-generated assertions for Assembly type using [AssertionFrom&lt;Assembly&gt;] and [GenerateAssertion(InlineMethodBody = true)] attributes.
/// </summary>
#if NET5_0_OR_GREATER
[AssertionFrom<Assembly>(nameof(Assembly.IsCollectible), ExpectationMessage = "be collectible")]
[AssertionFrom<Assembly>(nameof(Assembly.IsCollectible), CustomName = "IsNotCollectible", NegateLogic = true, ExpectationMessage = "be collectible")]
#endif

[AssertionFrom<Assembly>(nameof(Assembly.IsDynamic), ExpectationMessage = "be dynamic")]
[AssertionFrom<Assembly>(nameof(Assembly.IsDynamic), CustomName = "IsNotDynamic", NegateLogic = true, ExpectationMessage = "be dynamic")]

[AssertionFrom<Assembly>(nameof(Assembly.IsFullyTrusted), ExpectationMessage = "be fully trusted")]
[AssertionFrom<Assembly>(nameof(Assembly.IsFullyTrusted), CustomName = "IsNotFullyTrusted", NegateLogic = true, ExpectationMessage = "be fully trusted")]
file static partial class AssemblyAssertionExtensions
{
    [GenerateAssertion(ExpectationMessage = "to be signed", InlineMethodBody = true)]
    public static bool IsSigned(this Assembly value) => value.GetName().GetPublicKeyToken() is { Length: > 0 };
    [GenerateAssertion(ExpectationMessage = "to not be signed", InlineMethodBody = true)]
    public static bool IsNotSigned(this Assembly value) => value.GetName().GetPublicKeyToken() is null or { Length: 0 };

    // TODO: These methods cannot be inlined due to missing using System.Diagnostics; in generated code
    // and cannot be called as non-inlined methods due to generator limitations
    // [EditorBrowsable(EditorBrowsableState.Never)]
    // [GenerateAssertion(ExpectationMessage = "to be a debug build")]
    // public static bool IsDebugBuild(this Assembly value) => value.GetCustomAttribute<DebuggableAttribute>() is { IsJITTrackingEnabled: true };

    // [EditorBrowsable(EditorBrowsableState.Never)]
    // [GenerateAssertion(ExpectationMessage = "to be a release build")]
    // public static bool IsReleaseBuild(this Assembly value) => value.GetCustomAttribute<DebuggableAttribute>() is not { IsJITTrackingEnabled: true };
}
