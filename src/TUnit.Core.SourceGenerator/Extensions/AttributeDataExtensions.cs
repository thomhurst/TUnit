using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class AttributeDataExtensions
{
    /// <summary>
    /// Returns the attribute application syntax only when its syntax tree belongs to
    /// <paramref name="compilation"/>; otherwise <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// On the command line, project references are PE references, so attributes declared in another
    /// project have no <see cref="AttributeData.ApplicationSyntaxReference"/>. In IDE workspaces
    /// (C# DevKit, Visual Studio, Rider) project references are <see cref="CompilationReference"/>s, so
    /// attributes on base types or inherited methods from another project keep a syntax reference into
    /// that project's trees. Passing such a tree to <see cref="Compilation.GetSemanticModel(SyntaxTree, bool)"/>
    /// throws "SyntaxTree is not part of the compilation" and crashes the generator, so callers must treat
    /// it exactly like the metadata case and fall back to <see cref="AttributeData.ConstructorArguments"/>.
    /// </remarks>
    public static AttributeSyntax? GetApplicationSyntaxInCompilation(this AttributeData attributeData, Compilation compilation)
    {
        var syntaxReference = attributeData.ApplicationSyntaxReference;

        if (syntaxReference is null || !compilation.ContainsSyntaxTree(syntaxReference.SyntaxTree))
        {
            return null;
        }

        return syntaxReference.GetSyntax() as AttributeSyntax;
    }

    public static string? GetFullyQualifiedAttributeTypeName(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualifiedNonGeneric();
    }

    public static bool IsTestAttribute(this AttributeData? attributeData)
    {
        return attributeData?.AttributeClass?.GloballyQualified() == WellKnownFullyQualifiedClassNames.TestAttribute.WithGlobalPrefix;
    }

    public static bool IsDataSourceAttribute(this AttributeData? attributeData)
    {
        return DataSourceAttributeHelper.IsDataSourceAttribute(attributeData?.AttributeClass);
    }

    public static bool IsTypedDataSourceAttribute(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass == null)
        {
            return false;
        }

        return InterfaceHelper.ImplementsGenericInterface(attributeData.AttributeClass,
            WellKnownFullyQualifiedClassNames.ITypedDataSourceAttribute.WithGlobalPrefix + "`1");
    }

    public static ITypeSymbol? GetTypedDataSourceType(this AttributeData? attributeData)
    {
        if (attributeData?.AttributeClass == null)
        {
            return null;
        }

        var typedInterface = InterfaceHelper.GetGenericInterface(attributeData.AttributeClass,
            WellKnownFullyQualifiedClassNames.ITypedDataSourceAttribute.WithGlobalPrefix + "`1");

        return typedInterface?.TypeArguments.FirstOrDefault();
    }
}
