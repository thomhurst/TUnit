using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers;

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
        SymbolDisplayKindOptions.None,
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static readonly SymbolDisplayFormat FullyQualifiedGenericWithGlobalPrefix = new(
        SymbolDisplayGlobalNamespaceStyle.Included,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.IncludeTypeParameters,
        SymbolDisplayMemberOptions.IncludeContainingType,
        SymbolDisplayDelegateStyle.NameAndSignature,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.IncludeType,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.IncludeType,
        SymbolDisplayKindOptions.None,
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
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
        SymbolDisplayKindOptions.None,
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
    );

    public static readonly SymbolDisplayFormat NameOnly = new(miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}
