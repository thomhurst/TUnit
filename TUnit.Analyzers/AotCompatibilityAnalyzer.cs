using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using TUnit.Analyzers.Extensions;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotCompatibilityAnalyzer : ConcurrentDiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            Rules.GenericTypeNotAotCompatible,
            Rules.TupleNotAotCompatible);

    protected override void InitializeInternal(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeTestMethod, SymbolKind.Method);
    }


    private void AnalyzeTestMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsTestMethod(context.Compilation))
        {
            return;
        }

        // Note: We don't skip warnings even in source-generated mode because
        // generic test methods still require special handling for AOT

        // Check if test method has generic parameters
        if (methodSymbol.IsGenericMethod || methodSymbol.ContainingType.IsGenericType)
        {
            // Check if the method or class has the AotCompatible attribute
            if (HasAotCompatibleAttribute(methodSymbol))
            {
                // Method has been marked as AOT-safe - no warning
                return;
            }

            // Generic test methods with any data source attributes are problematic for AOT
            // because the generic type arguments need to be determined at runtime
            var hasDataSource = methodSymbol.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "ArgumentsAttribute" ||
                            attr.AttributeClass?.Name == "MethodDataSourceAttribute" ||
                            attr.AttributeClass?.Name == "ClassDataSourceAttribute" ||
                            IsDataSourceAttribute(attr, context.Compilation));

            if (hasDataSource || methodSymbol.IsGenericMethod)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.GenericTypeNotAotCompatible,
                    methodSymbol.Locations.FirstOrDefault(),
                    "Generic test method may require runtime type creation"));
            }
        }

        // Check for tuple parameters only if not using ITuple interface
        #if !NET
        foreach (var parameter in methodSymbol.Parameters)
        {
            if (IsTupleType(parameter.Type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.TupleNotAotCompatible,
                    parameter.Locations.FirstOrDefault(),
                    $"Tuple parameter '{parameter.Name}' - consider using concrete types for AOT compatibility"));
            }
        }
        #endif
    }






    private static bool IsTupleType(ITypeSymbol type)
    {
        // Check if it's a tuple type using the IsTupleType property
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.IsTupleType;
        }
        return false;
    }



    private static bool HasAotCompatibleAttribute(IMethodSymbol method)
    {
        // Check method attributes
        if (method.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "AotCompatibleAttribute"))
        {
            return true;
        }

        // Check containing type attributes
        if (method.ContainingType != null &&
            method.ContainingType.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "AotCompatibleAttribute"))
        {
            return true;
        }

        return false;
    }

    private static bool IsDataSourceAttribute(AttributeData attr, Compilation compilation)
    {
        var dataSourceInterface = compilation.GetTypeByMetadataName("TUnit.Core.IDataSourceAttribute");
        if (dataSourceInterface == null || attr.AttributeClass == null)
        {
            return false;
        }

        return attr.AttributeClass.AllInterfaces.Contains(dataSourceInterface, SymbolEqualityComparer.Default) ||
               SymbolEqualityComparer.Default.Equals(attr.AttributeClass, dataSourceInterface);
    }
}