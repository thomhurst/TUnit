using System.Collections.Generic;
using System.Globalization;

namespace TUnit.Core.Helpers;

/// <summary>
/// The failure messages for a method-level <c>[GenerateGenericTest]</c> whose type arguments cannot close the
/// method. Reflection mode (TUnit.Engine) uses this class, and TUnit.Core.SourceGenerator compiles this same file,
/// so both modes report the same text (#7222). Callers pass type names already formatted as <c>Type.Name</c> gives them.
/// </summary>
internal static class GenerateGenericTestArityMessages
{
    /// <summary>How a null type argument, such as <c>[GenerateGenericTest((Type)null!)]</c>, is named.</summary>
    public const string NullTypeName = "null";

    public static string NoTypeArguments(string methodName)
        => $"[GenerateGenericTest] on method '{methodName}' has no type arguments";

    public static string WrongCount(string methodName, int providedCount, int requiredCount, IEnumerable<string> providedTypeNames)
        => $"[GenerateGenericTest] on method '{methodName}' provides {providedCount.ToString(CultureInfo.InvariantCulture)} type argument(s) "
            + $"but method requires {requiredCount.ToString(CultureInfo.InvariantCulture)}. "
            + $"Provided: [{string.Join(", ", providedTypeNames)}]";

    public static string InvalidTypeArgument(string methodName, string typeName)
        => $"[GenerateGenericTest] on method '{methodName}' provides type argument '{typeName}', which cannot be a generic type argument";
}
