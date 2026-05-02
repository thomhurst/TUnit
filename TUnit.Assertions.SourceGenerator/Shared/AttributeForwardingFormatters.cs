using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace TUnit.SourceGen.Shared;

/// <summary>
/// Shared formatters that turn <see cref="System.ObsoleteAttribute"/> and
/// <see cref="System.ComponentModel.EditorBrowsableAttribute"/> instances back into the C# source
/// fragments needed to forward them onto generator-emitted methods.
/// </summary>
/// <remarks>
/// Linked into both <c>TUnit.Assertions.SourceGenerator</c> and
/// <c>TUnit.Assertions.Should.SourceGenerator</c> via <c>&lt;Compile Include="..."&gt;</c>. The
/// generators use these to propagate user deprecation/visibility decisions through the source
/// surface they emit; without forwarding, an <c>[Obsolete]</c> on a <c>[GenerateAssertion]</c>
/// or <c>[AssertionExtension]</c> source method would be dead because users only ever call the
/// generated extension/wrapper methods.
/// </remarks>
internal static class AttributeForwardingFormatters
{
    /// <summary>Formats an <see cref="System.ObsoleteAttribute"/> for the source generator's emitted method.</summary>
    /// <param name="globalQualifier">The fully-qualified prefix to use, e.g. <c>"global::"</c> or <c>""</c>.</param>
    public static string FormatObsolete(AttributeData attr, string globalQualifier = "")
    {
        var args = new List<string>();
        if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string message)
        {
            args.Add($"\"{message.Replace("\"", "\\\"")}\"");
        }
        if (attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is bool isError && isError)
        {
            args.Add("true");
        }

        var diagnosticIdPart = "";
        var urlFormatPart = "";
        foreach (var named in attr.NamedArguments)
        {
            if (named.Key == "DiagnosticId" && named.Value.Value is string id)
            {
                diagnosticIdPart = $", DiagnosticId = \"{id}\"";
            }
            else if (named.Key == "UrlFormat" && named.Value.Value is string url)
            {
                urlFormatPart = $", UrlFormat = \"{url}\"";
            }
        }
        return $"[{globalQualifier}System.Obsolete({string.Join(", ", args)}{diagnosticIdPart}{urlFormatPart})]";
    }

    /// <summary>Formats an <see cref="System.ComponentModel.EditorBrowsableAttribute"/> for the emitted method.</summary>
    public static string FormatEditorBrowsable(AttributeData attr, string globalQualifier = "")
    {
        var state = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int s ? s : 0;
        return $"[{globalQualifier}System.ComponentModel.EditorBrowsable(({globalQualifier}System.ComponentModel.EditorBrowsableState){state})]";
    }
}
