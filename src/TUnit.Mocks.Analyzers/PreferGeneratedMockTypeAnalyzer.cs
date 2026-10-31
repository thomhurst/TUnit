using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TUnit.Mocks.Analyzers;

/// <summary>
/// Reports locals, private fields and private properties declared as <c>Mock&lt;T&gt;</c> that are
/// only ever assigned the generated mock type <c>T.Mock()</c> returns for interfaces (a sealed
/// type deriving from <c>Mock&lt;T&gt;</c> that also implements <c>T</c>). Declaring them with the
/// generated type keeps that extra surface available.
/// </summary>
/// <remarks>
/// The code fix changes the declared type, so the analyzer only reports where it can see every
/// write and read of the member (locals, and private members of a type declared in one file), and
/// only when no use depends on the type staying <c>Mock&lt;T&gt;</c>.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PreferGeneratedMockTypeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic property holding the generated mock type, as it should be written at the declaration.
    /// </summary>
    public const string MockTypeProperty = "MockType";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rules.TM011_PreferGeneratedMockType);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            if (compilationContext.Compilation.GetTypeByMetadataName("TUnit.Mocks.Mock`1") is not { } mockType)
            {
                return;
            }

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeVariableDeclaration(nodeContext, mockType),
                SyntaxKind.VariableDeclaration);

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzePropertyDeclaration(nodeContext, mockType),
                SyntaxKind.PropertyDeclaration);
        });
    }

    private static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol mockType)
    {
        var declaration = (VariableDeclarationSyntax)context.Node;

        if (declaration.Parent is not (LocalDeclarationStatementSyntax or FieldDeclarationSyntax)
            || !LooksLikeMockType(declaration.Type)
            || GetDeclaredMockType(declaration.Type, mockType, context.SemanticModel, context.CancellationToken) is not { } declaredType)
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;
        var symbols = new List<ISymbol>(declaration.Variables.Count);
        var analysis = new Analysis(declaredType, semanticModel, cancellationToken);

        foreach (var declarator in declaration.Variables)
        {
            var symbol = semanticModel.GetDeclaredSymbol(declarator, cancellationToken);
            if (symbol is not (ILocalSymbol or IFieldSymbol))
            {
                return;
            }

            symbols.Add(symbol);

            if (declarator.Initializer is { } initializer && !analysis.AcceptValue(initializer.Value))
            {
                return;
            }
        }

        var scopes = declaration.Parent is LocalDeclarationStatementSyntax
            ? GetLocalScope(declaration)
            : GetMemberScopes(symbols[0], declaration.SyntaxTree);

        if (scopes is null || !analysis.AcceptReferences(scopes, symbols))
        {
            return;
        }

        Report(context, declaration.Type, symbols, analysis);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol mockType)
    {
        var property = (PropertyDeclarationSyntax)context.Node;

        if (property.ExplicitInterfaceSpecifier is not null
            || !LooksLikeMockType(property.Type)
            || GetDeclaredMockType(property.Type, mockType, context.SemanticModel, context.CancellationToken) is not { } declaredType
            || context.SemanticModel.GetDeclaredSymbol(property, context.CancellationToken) is not IPropertySymbol symbol)
        {
            return;
        }

        var analysis = new Analysis(declaredType, context.SemanticModel, context.CancellationToken);

        if (property.ExpressionBody is { } expressionBody)
        {
            if (!analysis.AcceptValue(expressionBody.Expression))
            {
                return;
            }
        }
        else
        {
            // Only auto-properties: an accessor body could read or return a Mock<T> from elsewhere.
            if (property.AccessorList is not { } accessorList
                || accessorList.Accessors.Any(static a => a.Body is not null || a.ExpressionBody is not null))
            {
                return;
            }

            if (property.Initializer is { } initializer && !analysis.AcceptValue(initializer.Value))
            {
                return;
            }
        }

        var symbols = new List<ISymbol> { symbol };

        if (GetMemberScopes(symbol, property.SyntaxTree) is not { } scopes
            || !analysis.AcceptReferences(scopes, symbols))
        {
            return;
        }

        Report(context, property.Type, symbols, analysis);
    }

    private static void Report(SyntaxNodeAnalysisContext context, TypeSyntax typeSyntax, List<ISymbol> symbols, Analysis analysis)
    {
        // Only report once a value of the generated type has been seen; a Mock<T> that is only
        // ever null has nothing to gain.
        if (analysis.GeneratedMockType is not { } generatedMockType
            || !context.SemanticModel.Compilation.IsSymbolAccessibleWithin(generatedMockType, context.SemanticModel.Compilation.Assembly))
        {
            return;
        }

        var position = typeSyntax.SpanStart;
        var generatedMockTypeName = generatedMockType.ToMinimalDisplayString(context.SemanticModel, position);
        var declaredTypeName = analysis.DeclaredType.ToMinimalDisplayString(context.SemanticModel, position);

        context.ReportDiagnostic(
            Diagnostic.Create(
                Rules.TM011_PreferGeneratedMockType,
                typeSyntax.GetLocation(),
                ImmutableDictionary<string, string?>.Empty.Add(MockTypeProperty, generatedMockTypeName),
                string.Join(", ", symbols.Select(static s => s.Name)),
                generatedMockTypeName,
                declaredTypeName));
    }

    /// <summary>
    /// Cheap syntactic filter: <c>Mock&lt;X&gt;</c>, optionally qualified and/or nullable.
    /// </summary>
    private static bool LooksLikeMockType(TypeSyntax type)
    {
        if (type is NullableTypeSyntax nullable)
        {
            type = nullable.ElementType;
        }

        var name = type switch
        {
            QualifiedNameSyntax qualified => qualified.Right,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name,
            _ => type,
        };

        return name is GenericNameSyntax { Identifier.ValueText: "Mock", TypeArgumentList.Arguments.Count: 1 };
    }

    private static INamedTypeSymbol? GetDeclaredMockType(
        TypeSyntax type,
        INamedTypeSymbol mockType,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        return semanticModel.GetTypeInfo(type, cancellationToken).Type is INamedTypeSymbol declaredType
               && SymbolEqualityComparer.Default.Equals(declaredType.OriginalDefinition, mockType)
            ? declaredType
            : null;
    }

    /// <summary>
    /// A local is only visible inside its enclosing block (or, for top-level statements, the
    /// compilation unit).
    /// </summary>
    private static IReadOnlyList<SyntaxNode>? GetLocalScope(VariableDeclarationSyntax declaration)
    {
        SyntaxNode? scope = declaration.FirstAncestorOrSelf<BlockSyntax>();
        scope ??= declaration.FirstAncestorOrSelf<CompilationUnitSyntax>();

        return scope is null ? null : new[] { scope };
    }

    /// <summary>
    /// A private member is only visible inside its containing type (including nested types). The
    /// fix can only be trusted when every part of that type is in this file.
    /// </summary>
    private static IReadOnlyList<SyntaxNode>? GetMemberScopes(ISymbol symbol, SyntaxTree syntaxTree)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Private
            || symbol.IsOverride
            || symbol.IsVirtual
            || symbol.IsAbstract
            || symbol.ContainingType is not { } containingType)
        {
            return null;
        }

        var scopes = new List<SyntaxNode>(containingType.DeclaringSyntaxReferences.Length);
        foreach (var reference in containingType.DeclaringSyntaxReferences)
        {
            if (reference.SyntaxTree != syntaxTree)
            {
                return null;
            }

            scopes.Add(reference.GetSyntax());
        }

        return scopes;
    }

    private sealed class Analysis
    {
        private readonly SemanticModel _semanticModel;
        private readonly CancellationToken _cancellationToken;

        public Analysis(INamedTypeSymbol declaredType, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            DeclaredType = declaredType;
            _semanticModel = semanticModel;
            _cancellationToken = cancellationToken;
        }

        public INamedTypeSymbol DeclaredType { get; }

        /// <summary>
        /// The generated mock type every non-null value assigned to the member has.
        /// </summary>
        public INamedTypeSymbol? GeneratedMockType { get; private set; }

        /// <summary>
        /// Whether a value stored in the member still converts to it once it is declared with the
        /// generated mock type: <c>null</c>/<c>default</c>, or a value of that generated type.
        /// </summary>
        public bool AcceptValue(ExpressionSyntax value)
        {
            while (true)
            {
                switch (value)
                {
                    case ParenthesizedExpressionSyntax parenthesized:
                        value = parenthesized.Expression;
                        continue;
                    case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed:
                        value = suppressed.Operand;
                        continue;
                }

                break;
            }

            if (value.IsKind(SyntaxKind.NullLiteralExpression) || value.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                return true;
            }

            if (_semanticModel.GetTypeInfo(value, _cancellationToken).Type is not INamedTypeSymbol valueType
                || SymbolEqualityComparer.Default.Equals(valueType, DeclaredType)
                || !SymbolEqualityComparer.Default.Equals(valueType.BaseType, DeclaredType))
            {
                return false;
            }

            if (GeneratedMockType is null)
            {
                GeneratedMockType = valueType;
                return true;
            }

            return SymbolEqualityComparer.Default.Equals(GeneratedMockType, valueType);
        }

        /// <summary>
        /// Checks every reference to the declared symbols: writes must store a value
        /// <see cref="AcceptValue"/> accepts, and no use may depend on the member's type. Writes
        /// are checked first, so the generated type is known when the uses are checked.
        /// </summary>
        public bool AcceptReferences(IReadOnlyList<SyntaxNode> scopes, List<ISymbol> symbols)
        {
            var uses = new List<ExpressionSyntax>();

            foreach (var scope in scopes)
            {
                foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    if (!IsReferenceTo(identifier, symbols))
                    {
                        continue;
                    }

                    if (GetReference(identifier) is not { } reference)
                    {
                        return false;
                    }

                    if (MockTypeFlow.IsWrite(reference))
                    {
                        if (GetStore(reference) is not { } store || !AcceptValue(store.Right))
                        {
                            return false;
                        }

                        // The assignment's own value has the member's type, so it must not flow
                        // anywhere type-sensitive either.
                        uses.Add(store);
                    }
                    else
                    {
                        uses.Add(reference);
                    }
                }
            }

            foreach (var use in uses)
            {
                if (MockTypeFlow.ClimbTypeFlow(use) != use
                    || !MockTypeFlow.IsTypeInsensitivePosition(use, GeneratedMockType, _semanticModel, _cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsReferenceTo(IdentifierNameSyntax identifier, List<ISymbol> symbols)
        {
            var name = identifier.Identifier.ValueText;
            if (!symbols.Any(s => s.Name == name))
            {
                return false;
            }

            // A member of a generic type can also be reached through another constructed type
            // (`Holder<int> other; other._mock = ...`), so compare original definitions.
            var referenced = _semanticModel.GetSymbolInfo(identifier, _cancellationToken).Symbol?.OriginalDefinition;
            return referenced is not null
                   && symbols.Any(s => SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, referenced));
        }

        private static ExpressionSyntax? GetReference(IdentifierNameSyntax identifier)
        {
            return identifier.Parent switch
            {
                // this._mock, Tests._mock, other._mock
                MemberAccessExpressionSyntax memberAccess when memberAccess.Name == identifier => memberAccess,
                // other?._mock: the conditional access result type would change.
                MemberBindingExpressionSyntax => null,
                _ => identifier,
            };
        }

        /// <summary>
        /// Plain stores only (<c>=</c> and <c>??=</c>). Ref/out arguments, ref expressions and
        /// deconstruction could bind differently once the member's type changes.
        /// </summary>
        private static AssignmentExpressionSyntax? GetStore(ExpressionSyntax reference)
        {
            SyntaxNode target = reference;
            while (target.Parent is ParenthesizedExpressionSyntax parenthesized)
            {
                target = parenthesized;
            }

            return target.Parent is AssignmentExpressionSyntax assignment
                   && assignment.Left == target
                   && (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) || assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression))
                ? assignment
                : null;
        }
    }
}
