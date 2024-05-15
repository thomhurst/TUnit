using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator;

public class DisplayFormats
{
    public static SymbolDisplayFormat FullyQualifiedNonGenericWithGlobalPrefix => new(
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
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );
    
    public static SymbolDisplayFormat FullyQualifiedGenericWithGlobalPrefix => new(
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
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );
    
    public static SymbolDisplayFormat FullyQualifiedNonGeneric => new(
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
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public static SymbolDisplayFormat FullyQualifiedGenericWithoutGlobalPrefix => new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        SymbolDisplayGenericsOptions.IncludeTypeParameters,
        SymbolDisplayMemberOptions.IncludeContainingType,
        SymbolDisplayDelegateStyle.NameAndSignature,
        SymbolDisplayExtensionMethodStyle.Default,
        SymbolDisplayParameterOptions.IncludeType,
        SymbolDisplayPropertyStyle.NameOnly,
        SymbolDisplayLocalOptions.IncludeType,
        SymbolDisplayKindOptions.None, 
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );
}