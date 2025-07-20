using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InitOnlyPropertyAnalyzer : DiagnosticAnalyzer
{
    private const string PropertyInjectionCategory = "PropertyInjection";
    
    public static readonly DiagnosticDescriptor InitOnlyPropertyNotSupported = new(
        id: "TUnit0100", 
        title: "Init-only properties with data source attributes are not supported on this framework",
        messageFormat: "The init-only property '{0}' with a data source attribute is not supported on frameworks older than .NET 8. Change it to use 'set' instead of 'init' or upgrade to .NET 8 or later",
        category: PropertyInjectionCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Init-only properties with data source attributes require .NET 8 or later for AOT-compatible code generation. On older frameworks, use regular settable properties instead.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(InitOnlyPropertyNotSupported);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        
        // Check if it has init accessor
        var hasInit = propertyDeclaration.AccessorList?.Accessors
            .Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false;
        
        if (!hasInit)
            return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
        if (propertySymbol == null)
            return;

        // Check if property has any data source attribute
        var hasDataSourceAttribute = propertySymbol.GetAttributes()
            .Any(attr => IsDataSourceAttribute(attr.AttributeClass));
        
        if (!hasDataSourceAttribute)
            return;

        // Check if we're targeting .NET 8 or later
        var compilation = context.Compilation;
        var isNet8OrLater = IsTargetingNet8OrLater(compilation);
        
        if (!isNet8OrLater)
        {
            var diagnostic = Diagnostic.Create(
                InitOnlyPropertyNotSupported,
                propertyDeclaration.Identifier.GetLocation(),
                propertySymbol.Name);
            
            context.ReportDiagnostic(diagnostic);
        }
    }
    
    private static bool IsDataSourceAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null)
            return false;
            
        var name = attributeClass.Name;
        return name.EndsWith("DataSourceAttribute") || 
               name.EndsWith("DataSourceGeneratorAttribute") ||
               name == "MatrixAttribute";
    }
    
    private static bool IsTargetingNet8OrLater(Compilation compilation)
    {
        // Check if UnsafeAccessor is available (indicates .NET 8+)
        var unsafeAccessorType = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnsafeAccessorAttribute");
        return unsafeAccessorType != null;
    }
}