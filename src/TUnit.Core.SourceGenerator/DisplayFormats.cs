using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator;

public class DisplayFormats
{
    public static readonly SymbolDisplayFormat FullyQualifiedNonGenericWithGlobalPrefix = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.None,
        SymbolDisplayMemberOptions.IncludeContainingType,
        SymbolDisplayDelegateStyle.NameAndSignature,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.IncludeType,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
#if ROSLYN4_7_OR_GREATER
        | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat FullyQualifiedGenericWithGlobalPrefix = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.IncludeTypeParameters,
        SymbolDisplayMemberOptions.IncludeContainingType,
        SymbolDisplayDelegateStyle.NameOnly,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.IncludeType,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
#if ROSLYN4_7_OR_GREATER
        | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat FullyQualifiedNonGeneric = new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.None,
        SymbolDisplayMemberOptions.IncludeContainingType,
        SymbolDisplayDelegateStyle.NameAndSignature,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.IncludeType,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
#if ROSLYN4_7_OR_GREATER
        | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat FullyQualifiedGenericWithoutGlobalPrefix = new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.IncludeTypeParameters,
        SymbolDisplayMemberOptions.IncludeContainingType,
        SymbolDisplayDelegateStyle.NameAndSignature,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.IncludeType,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.IncludeType,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
#if ROSLYN4_7_OR_GREATER
        | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );

    public static readonly SymbolDisplayFormat NameOnly = new(miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static readonly SymbolDisplayFormat FullyQualifiedGenericTypeOnly = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.None,
        SymbolDisplayMemberOptions.None,
        SymbolDisplayDelegateStyle.NameOnly,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.None,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes
#if ROSLYN4_7_OR_GREATER
        | SymbolDisplayMiscellaneousOptions.ExpandValueTuple
#endif
    );
}
