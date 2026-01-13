using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitMigrationCodeFixProvider)), Shared]
public class XUnitMigrationCodeFixProvider : BaseMigrationCodeFixProvider
{
    protected override string FrameworkName => "XUnit";
    protected override string DiagnosticId => Rules.XunitMigration.Id;
    protected override string CodeFixTitle => Rules.XunitMigration.Title.ToString();

    protected override bool ShouldAddTUnitUsings() => false;

    protected override AttributeRewriter CreateAttributeRewriter(Compilation compilation)
    {
        return new XUnitAttributeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new XUnitAssertionRewriter(semanticModel);
    }

    protected override CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new PassThroughRewriter();
    }

    protected override CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation)
    {
        return new PassThroughRewriter();
    }

    private class PassThroughRewriter : CSharpSyntaxRewriter
    {
    }

    protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation)
    {
        // Use the original syntax tree from the semantic model, not from the (potentially modified) compilation unit
        // After assertion rewriting, compilationUnit.SyntaxTree is a new tree not in the compilation
        var syntaxTree = semanticModel.SyntaxTree;
        SyntaxNode updatedRoot = compilationUnit;

        updatedRoot = UpdateInitializeDispose(compilation, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, ref updatedRoot);

        updatedRoot = UpdateClassAttributes(compilation, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, ref updatedRoot);

        updatedRoot = RemoveInterfacesAndBaseClasses(compilation, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, ref updatedRoot);

        updatedRoot = ConvertTheoryData(compilation, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, ref updatedRoot);

        updatedRoot = ConvertTestOutputHelpers(ref compilation, ref syntaxTree, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, ref updatedRoot);

        return (CompilationUnitSyntax)updatedRoot;
    }

    private static SyntaxNode ConvertTestOutputHelpers(ref Compilation compilation, ref SyntaxTree syntaxTree, SyntaxNode root)
    {
        var currentRoot = root;

        var compilationValue = compilation;

        while (currentRoot.DescendantNodes()
               .OfType<InvocationExpressionSyntax>()
               .FirstOrDefault(x => IsTestOutputHelperInvocation(compilationValue, x))
               is { } invocationExpressionSyntax)
        {
            var memberAccessExpressionSyntax = (MemberAccessExpressionSyntax) invocationExpressionSyntax.Expression;

            currentRoot = currentRoot.ReplaceNode(
                invocationExpressionSyntax,
                invocationExpressionSyntax.WithExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Console"),
                        SyntaxFactory.IdentifierName(memberAccessExpressionSyntax.Name.Identifier.Text)
                    )
                )
            );

            UpdateSyntaxTrees(ref compilation, ref syntaxTree, ref currentRoot);
            compilationValue = compilation;
        }

        while (currentRoot.DescendantNodes()
                     .OfType<ParameterSyntax>()
                     .FirstOrDefault(x => x.Type?.TryGetInferredMemberName() == "ITestOutputHelper")
               is { } parameterSyntax)
        {
            currentRoot = currentRoot.RemoveNode(parameterSyntax, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        var membersToRemove = currentRoot.DescendantNodes()
            .Where(n => (n is PropertyDeclarationSyntax prop && prop.Type.TryGetInferredMemberName() == "ITestOutputHelper") ||
                        (n is FieldDeclarationSyntax field && field.Declaration.Type.TryGetInferredMemberName() == "ITestOutputHelper"))
            .ToList();

        if (membersToRemove.Count > 0)
        {
            currentRoot = currentRoot.RemoveNodes(membersToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        return currentRoot;
    }

    private static bool IsTestOutputHelperInvocation(Compilation compilation, InvocationExpressionSyntax invocationExpressionSyntax)
    {
        var semanticModel = compilation.GetSemanticModel(invocationExpressionSyntax.SyntaxTree);

        var symbolInfo = semanticModel.GetSymbolInfo(invocationExpressionSyntax);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax)
        {
            return false;
        }

        return methodSymbol.ContainingType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
            is "global::Xunit.Abstractions.ITestOutputHelper" or "global::Xunit.ITestOutputHelper";
    }

    private static SyntaxNode ConvertTheoryData(Compilation compilation, SyntaxNode root)
    {
        var currentRoot = root;
        foreach (var objectCreationExpressionSyntax in currentRoot.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
        {
            var type = objectCreationExpressionSyntax switch
            {
                ObjectCreationExpressionSyntax explicitObjectCreationExpressionSyntax => explicitObjectCreationExpressionSyntax.Type,
                ImplicitObjectCreationExpressionSyntax implicitObjectCreationExpressionSyntax => SyntaxFactory.ParseTypeName(compilation.GetSemanticModel(implicitObjectCreationExpressionSyntax.SyntaxTree).GetTypeInfo(implicitObjectCreationExpressionSyntax).Type!.ToDisplayString()),
                _ => null
            };

            while (type is QualifiedNameSyntax qualifiedNameSyntax)
            {
                type = qualifiedNameSyntax.Right;
            }

            if (type is not GenericNameSyntax genericNameSyntax ||
                genericNameSyntax.Identifier.Text != "TheoryData")
            {
                continue;
            }

            var originalInitializer = objectCreationExpressionSyntax.Initializer!;
            var collectionExpressions = originalInitializer.Expressions;
            var elementType = genericNameSyntax.TypeArgumentList.Arguments[0];

            var arrayType = SyntaxFactory.ArrayType(elementType,
                SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression()
                        )
                    )
                ))
                .WithoutTrailingTrivia();

            var newKeyword = SyntaxFactory.Token(SyntaxKind.NewKeyword)
                .WithLeadingTrivia(objectCreationExpressionSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(SyntaxFactory.Space);

            var openBrace = originalInitializer.OpenBraceToken;
            if (!openBrace.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
            {
                openBrace = openBrace.WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Whitespace("    "));
            }

            var newInitializer = SyntaxFactory.InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    openBrace,
                    collectionExpressions,
                    originalInitializer.CloseBraceToken);

            var arrayCreationExpressionSyntax = SyntaxFactory.ArrayCreationExpression(
                newKeyword,
                arrayType,
                newInitializer
            )
            .WithTrailingTrivia(objectCreationExpressionSyntax.GetTrailingTrivia());

            currentRoot = currentRoot.ReplaceNode(objectCreationExpressionSyntax, arrayCreationExpressionSyntax);
        }

        foreach (var genericTheoryDataTypeSyntax in currentRoot.DescendantNodes().OfType<GenericNameSyntax>().Where(x => x.Identifier.Text == "TheoryData"))
        {
            var enumerableTypeSyntax = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("IEnumerable"),
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(genericTheoryDataTypeSyntax.TypeArgumentList.Arguments)))
                .WithLeadingTrivia(genericTheoryDataTypeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(genericTheoryDataTypeSyntax.GetTrailingTrivia());

            currentRoot = currentRoot.ReplaceNode(genericTheoryDataTypeSyntax, enumerableTypeSyntax);
        }

        return currentRoot;
    }

    private static SyntaxNode UpdateInitializeDispose(Compilation compilation, SyntaxNode root)
    {
        // Always operate on the latest root
        var currentRoot = root;
        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList())
        {
            if (classDeclaration.BaseList is null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not { } symbol)
            {
                continue;
            }

            // Always get the latest node from the current root
            var currentClass = currentRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(n => n.SpanStart == classDeclaration.SpanStart && n.Identifier.Text == classDeclaration.Identifier.Text);

            if (currentClass == null)
            {
                continue;
            }

            var newNode = new InitializeDisposeRewriter(symbol).VisitClassDeclaration(currentClass);

            if (!ReferenceEquals(currentClass, newNode))
            {
                currentRoot = currentRoot.ReplaceNode(currentClass, newNode);
            }
        }

        return currentRoot;
    }

    private static SyntaxNode RemoveInterfacesAndBaseClasses(Compilation compilation, SyntaxNode root)
    {
        var currentRoot = root;
        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList())
        {
            if (classDeclaration.BaseList is null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not { } symbol)
            {
                continue;
            }

            // Always get the latest node from the current root
            var currentClass = currentRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(n => n.SpanStart == classDeclaration.SpanStart && n.Identifier.Text == classDeclaration.Identifier.Text);

            if (currentClass == null)
            {
                continue;
            }

            var newNode = new BaseTypeRewriter(symbol).VisitClassDeclaration(currentClass);

            if (!ReferenceEquals(currentClass, newNode))
            {
                currentRoot = currentRoot.ReplaceNode(currentClass, newNode);
            }
        }

        return currentRoot;
    }

    private static SyntaxNode RemoveUsingDirectives(SyntaxNode updatedRoot)
    {
        var compilationUnit = updatedRoot.DescendantNodesAndSelf()
            .OfType<CompilationUnitSyntax>()
            .FirstOrDefault();

        if (compilationUnit is null)
        {
            return updatedRoot;
        }

        return compilationUnit.WithUsings(
            SyntaxFactory.List(
                compilationUnit.Usings
                    .Where(x => x.Name?.ToString().StartsWith("Xunit") is false)
            )
        );
    }

    private static SyntaxNode UpdateClassAttributes(Compilation compilation, SyntaxNode root)
    {
        var rewriter = new XUnitAttributeRewriterInternal(compilation);
        return rewriter.Visit(root);
    }

    private static string GetSimpleName(AttributeSyntax attributeSyntax)
    {
        var name = attributeSyntax.Name;

        while (name is not SimpleNameSyntax)
        {
            name = (name as QualifiedNameSyntax)?.Right;
        }

        return name.ToString();
    }

    private static AttributeArgumentListSyntax CreateArgumentListWithAddedArgument(
        AttributeArgumentListSyntax existingList,
        AttributeArgumentSyntax newArgument)
    {
        if (existingList.Arguments.Count == 0)
        {
            return existingList.AddArguments(newArgument);
        }

        // Preserve separator trivia by creating a new list with proper separators
        var newArguments = new List<AttributeArgumentSyntax>(existingList.Arguments);
        newArguments.Add(newArgument);

        var separators = new List<SyntaxToken>(existingList.Arguments.GetSeparators());
        // Add a comma with trailing space for the new argument
        separators.Add(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space));

        return SyntaxFactory.AttributeArgumentList(
            SyntaxFactory.SeparatedList(newArguments, separators));
    }

    private static IEnumerable<AttributeSyntax> ConvertTestAttribute(AttributeSyntax attributeSyntax)
    {
        yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Test"));

        if (attributeSyntax.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ValueText == "Skip") is { } skip)
        {
            yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Skip"))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(skip.Expression));
        }
    }

    private static IEnumerable<AttributeSyntax> ConvertCollection(Compilation compilation, AttributeSyntax attributeSyntax)
    {
        var collectionDefinition = GetCollectionAttribute(compilation, attributeSyntax);

        if (collectionDefinition is null)
        {
            return [attributeSyntax];
        }

        var disableParallelism =
            collectionDefinition.ArgumentList?.Arguments.Any(x => x.NameEquals?.Name.Identifier.Text == "DisableParallelization"
                && x.Expression is LiteralExpressionSyntax { Token.ValueText: "true" }) ?? false;

        var attributes = new List<AttributeSyntax>();

        if (disableParallelism)
        {
            attributes.Add(SyntaxFactory.Attribute(SyntaxFactory.ParseName("NotInParallel")));
        }

        var baseListSyntax = collectionDefinition.Parent?.Parent?.ChildNodes().OfType<BaseListSyntax>().FirstOrDefault();

        if (baseListSyntax is null)
        {
            return attributes;
        }

        var collectionFixture = baseListSyntax.Types.Select(x => x.Type).OfType<GenericNameSyntax>().FirstOrDefault(x => x.Identifier.Text == "ICollectionFixture");

        if (collectionFixture is null)
        {
            return attributes;
        }

        var type = collectionFixture.TypeArgumentList.Arguments.FirstOrDefault();

        if (type is null)
        {
            return attributes;
        }

        attributes.Add(SyntaxFactory.Attribute(
            SyntaxFactory.GenericName(SyntaxFactory.Identifier("ClassDataSource"),
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(type))),
            SyntaxFactory.AttributeArgumentList()
                .AddArguments(
                    SyntaxFactory.AttributeArgument(
                        nameEquals: SyntaxFactory.NameEquals("Shared"),
                        nameColon: null,
                        expression: SyntaxFactory.ParseExpression("SharedType.Keyed")
                    ),
                    SyntaxFactory.AttributeArgument(
                        nameEquals: SyntaxFactory.NameEquals("Key"),
                        nameColon: null,
                        expression: GetMethodArgumentName(attributeSyntax)
                    )
                ).NormalizeWhitespace()
        ));

        return attributes;
    }

    private static ExpressionSyntax GetMethodArgumentName(AttributeSyntax attributeSyntax)
    {
        var firstToken = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

        if (!firstToken.HasValue)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""));
        }

        return SyntaxFactory.ParseExpression(firstToken.Value.Text);
    }

    private static AttributeSyntax? GetCollectionAttribute(Compilation compilation, AttributeSyntax attributeSyntax)
    {
        var firstToken = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

        if (!firstToken.HasValue)
        {
            return null;
        }

        var collectionName = firstToken.Value.IsKind(SyntaxKind.NameOfKeyword)
            ? GetNameFromNameOfToken(firstToken.Value)
            : firstToken.Value.ValueText;

        if (collectionName is null)
        {
            return null;
        }

        return compilation.SyntaxTrees
            .Select(x => x.GetRoot())
            .SelectMany(x => x.DescendantNodes().OfType<AttributeSyntax>())
            .Where(attr => attr.Name.ToString() == "CollectionDefinition")
            .FirstOrDefault(x =>
            {
                var syntaxToken = x.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

                if (!syntaxToken.HasValue)
                {
                    return false;
                }

                var name = syntaxToken.Value.IsKind(SyntaxKind.NameOfKeyword)
                    ? GetNameFromNameOfToken(syntaxToken.Value)
                    : syntaxToken.Value.ValueText;

                return name == collectionName;
            });
    }

    private static string? GetNameFromNameOfToken(SyntaxToken token)
    {
        var expression = SyntaxFactory.ParseExpression(token.Text) as InvocationExpressionSyntax;

        if (expression?.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" } &&
            expression.ArgumentList.Arguments.FirstOrDefault()?.Expression is IdentifierNameSyntax nameOfArgument)
        {
            return nameOfArgument.Identifier.Text;
        }

        return null;
    }

    // This is the AttributeRewriter for the base class pattern
    private class XUnitAttributeRewriter : AttributeRewriter
    {
        protected override string FrameworkName => "XUnit";

        protected override bool IsFrameworkAttribute(string attributeName)
        {
            return attributeName is "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute"
                or "Trait" or "TraitAttribute" or "InlineData" or "InlineDataAttribute"
                or "MemberData" or "MemberDataAttribute" or "ClassData" or "ClassDataAttribute"
                or "Collection" or "CollectionAttribute" or "CollectionDefinition" or "CollectionDefinitionAttribute";
        }

        protected override AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName)
        {
            // XUnit attributes don't need special argument conversion - handled by XUnitAttributeRewriterInternal
            return argumentList;
        }
    }

    private class XUnitAssertionRewriter : AssertionRewriter
    {
        protected override string FrameworkName => "XUnit";

        public XUnitAssertionRewriter(SemanticModel semanticModel) : base(semanticModel)
        {
        }

        protected override bool IsFrameworkAssertionNamespace(string namespaceName)
        {
            return namespaceName.Equals("Xunit", StringComparison.OrdinalIgnoreCase) ||
                   namespaceName.StartsWith("Xunit.", StringComparison.OrdinalIgnoreCase);
        }

        protected override ExpressionSyntax? ConvertAssertionIfNeeded(InvocationExpressionSyntax invocation)
        {
            if (!IsFrameworkAssertion(invocation))
            {
                return null;
            }

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
            {
                return ConvertXUnitAssertion(invocation, memberAccess.Name.Identifier.Text, memberAccess.Name);
            }

            return null;
        }

        private ExpressionSyntax? ConvertXUnitAssertion(InvocationExpressionSyntax invocation, string methodName, SimpleNameSyntax nameNode)
        {
            var arguments = invocation.ArgumentList.Arguments;

            return methodName switch
            {
                // Equality assertions - check for comparer overloads
                "Equal" when arguments.Count >= 3 && IsLikelyComparerArgument(arguments[2]) == true =>
                    CreateEqualWithComparerComment(arguments),
                "Equal" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsEqualTo", arguments[1].Expression, arguments[0]),
                "NotEqual" when arguments.Count >= 3 && IsLikelyComparerArgument(arguments[2]) == true =>
                    CreateNotEqualWithComparerComment(arguments),
                "NotEqual" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsNotEqualTo", arguments[1].Expression, arguments[0]),

                // Boolean assertions
                "True" when arguments.Count >= 2 =>
                    CreateTUnitAssertionWithMessage("IsTrue", arguments[0].Expression, arguments[1].Expression),
                "True" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("IsTrue", arguments[0].Expression),
                "False" when arguments.Count >= 2 =>
                    CreateTUnitAssertionWithMessage("IsFalse", arguments[0].Expression, arguments[1].Expression),
                "False" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("IsFalse", arguments[0].Expression),

                // Null assertions
                "Null" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("IsNull", arguments[0].Expression),
                "NotNull" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("IsNotNull", arguments[0].Expression),

                // Reference assertions
                "Same" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsSameReferenceAs", arguments[1].Expression, arguments[0]),
                "NotSame" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsNotSameReferenceAs", arguments[1].Expression, arguments[0]),

                // String/Collection contains
                "Contains" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("Contains", arguments[1].Expression, arguments[0]),
                "DoesNotContain" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("DoesNotContain", arguments[1].Expression, arguments[0]),
                "StartsWith" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("StartsWith", arguments[1].Expression, arguments[0]),
                "EndsWith" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("EndsWith", arguments[1].Expression, arguments[0]),

                // Empty/Not empty
                "Empty" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("IsEmpty", arguments[0].Expression),
                "NotEmpty" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("IsNotEmpty", arguments[0].Expression),

                // Exception assertions
                "Throws" => ConvertThrows(invocation, nameNode),
                "ThrowsAsync" => ConvertThrowsAsync(invocation, nameNode),
                "ThrowsAny" => ConvertThrowsAny(invocation, nameNode),
                "ThrowsAnyAsync" => ConvertThrowsAnyAsync(invocation, nameNode),

                // Type assertions
                "IsType" => ConvertIsType(invocation, nameNode),
                "IsNotType" => ConvertIsNotType(invocation, nameNode),
                "IsAssignableFrom" => ConvertIsAssignableFrom(invocation, nameNode),

                // Range assertions
                "InRange" when arguments.Count >= 3 =>
                    CreateTUnitAssertion("IsInRange", arguments[0].Expression, arguments[1], arguments[2]),
                "NotInRange" when arguments.Count >= 3 =>
                    CreateTUnitAssertion("IsNotInRange", arguments[0].Expression, arguments[1], arguments[2]),

                // Collection assertions
                "Single" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("HasSingleItem", arguments[0].Expression),
                "All" when arguments.Count >= 2 =>
                    CreateAllAssertion(arguments[0].Expression, arguments[1].Expression),

                // Subset/superset
                "Subset" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsSubsetOf", arguments[0].Expression, arguments[1]),
                "Superset" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsSupersetOf", arguments[0].Expression, arguments[1]),
                "ProperSubset" when arguments.Count >= 2 =>
                    CreateProperSubsetWithTodo(arguments),
                "ProperSuperset" when arguments.Count >= 2 =>
                    CreateProperSupersetWithTodo(arguments),

                // Unique items
                "Distinct" when arguments.Count >= 1 =>
                    CreateTUnitAssertion("HasDistinctItems", arguments[0].Expression),

                // Equivalent (order independent)
                "Equivalent" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("IsEquivalentTo", arguments[1].Expression, arguments[0]),

                // Regex assertions
                "Matches" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("Matches", arguments[1].Expression, arguments[0]),
                "DoesNotMatch" when arguments.Count >= 2 =>
                    CreateTUnitAssertion("DoesNotMatch", arguments[1].Expression, arguments[0]),

                // Collection with inspectors - complex, needs TODO
                "Collection" when arguments.Count >= 2 =>
                    CreateCollectionWithTodo(arguments),

                // PropertyChanged - not supported in TUnit
                "PropertyChanged" when arguments.Count >= 3 =>
                    CreatePropertyChangedTodo(arguments),
                "PropertyChangedAsync" when arguments.Count >= 3 =>
                    CreatePropertyChangedTodo(arguments),

                // Raises events - not supported in TUnit
                "Raises" => CreateRaisesTodo(arguments),
                "RaisesAsync" => CreateRaisesTodo(arguments),
                "RaisesAny" => CreateRaisesTodo(arguments),
                "RaisesAnyAsync" => CreateRaisesTodo(arguments),

                _ => null
            };
        }

        private ExpressionSyntax CreateEqualWithComparerComment(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var result = CreateTUnitAssertion("IsEqualTo", arguments[1].Expression, arguments[0]);
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - custom comparer was used. Consider using Assert.That(...).IsEquivalentTo() or a custom condition."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreateNotEqualWithComparerComment(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var result = CreateTUnitAssertion("IsNotEqualTo", arguments[1].Expression, arguments[0]);
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - custom comparer was used. Consider using a custom condition."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreateCollectionWithTodo(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            // Assert.Collection(collection, inspector1, inspector2, ...) has no direct TUnit equivalent
            // Convert to HasCount check and add TODO for manual inspector conversion
            var collection = arguments[0].Expression;
            var inspectorCount = arguments.Count - 1;

            var result = CreateTUnitAssertion("HasCount", collection,
                SyntaxFactory.Argument(
                    SyntaxFactory.LiteralExpression(
                        SyntaxKind.NumericLiteralExpression,
                        SyntaxFactory.Literal(inspectorCount))));

            // Just add TODO comment and newline - indentation will be handled by VisitInvocationExpression
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - Assert.Collection had element inspectors. Manually add assertions for each element."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreatePropertyChangedTodo(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            // Assert.PropertyChanged(object, propertyName, action) - TUnit doesn't have this
            // Create a placeholder that executes the action and add TODO
            var action = arguments.Count > 2 ? arguments[2].Expression : arguments[0].Expression;

            // Create: action() with TODO comment
            var invocation = action is LambdaExpressionSyntax
                ? (ExpressionSyntax)SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParenthesizedExpression(action))
                : SyntaxFactory.InvocationExpression(action);

            return invocation.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - PropertyChanged assertion not supported. Implement INotifyPropertyChanged testing manually."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreateRaisesTodo(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            // Assert.Raises(attach, detach, action) - TUnit doesn't have this
            // Create placeholder with TODO
            var action = arguments.Count > 2 ? arguments[2].Expression : arguments[0].Expression;

            var invocation = action is LambdaExpressionSyntax
                ? (ExpressionSyntax)SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParenthesizedExpression(action))
                : SyntaxFactory.InvocationExpression(action);

            return invocation.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - Raises assertion not supported. Implement event testing manually."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreateProperSubsetWithTodo(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            // ProperSubset means strict subset (not equal to superset)
            // TUnit's IsSubsetOf doesn't distinguish between proper/improper
            var result = CreateTUnitAssertion("IsSubsetOf", arguments[0].Expression, arguments[1]);
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - ProperSubset requires strict subset (not equal). Add additional assertion if needed."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreateProperSupersetWithTodo(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            // ProperSuperset means strict superset (not equal to subset)
            // TUnit's IsSupersetOf doesn't distinguish between proper/improper
            var result = CreateTUnitAssertion("IsSupersetOf", arguments[0].Expression, arguments[1]);
            return result.WithLeadingTrivia(
                SyntaxFactory.Comment("// TODO: TUnit migration - ProperSuperset requires strict superset (not equal). Add additional assertion if needed."),
                SyntaxFactory.EndOfLine("\n"));
        }

        private ExpressionSyntax CreateAllAssertion(ExpressionSyntax collection, ExpressionSyntax actionOrPredicate)
        {
            // Assert.All(collection, action) -> await Assert.That(collection).All(predicate)
            // Try to extract a simple predicate from the action if possible

            var predicateExpression = TryConvertActionToPredicate(actionOrPredicate);

            // Create Assert.That(collection)
            var assertThatInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Assert"),
                    SyntaxFactory.IdentifierName("That")
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(collection)
                    )
                )
            );

            // Create Assert.That(collection).All(predicate)
            var allInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    assertThatInvocation,
                    SyntaxFactory.IdentifierName("All")
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(predicateExpression)
                    )
                )
            );

            // Wrap in await
            var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);
            return SyntaxFactory.AwaitExpression(awaitKeyword, allInvocation);
        }

        private ExpressionSyntax TryConvertActionToPredicate(ExpressionSyntax actionExpression)
        {
            // Try to convert xUnit action patterns to TUnit predicates
            // Pattern: item => Assert.True(item > 0) -> item => item > 0
            // Pattern: item => Assert.False(item < 0) -> item => !(item < 0)
            // Pattern: item => Assert.NotNull(item) -> item => item != null
            // Pattern: item => Assert.Null(item) -> item => item == null

            if (actionExpression is SimpleLambdaExpressionSyntax simpleLambda)
            {
                var parameter = simpleLambda.Parameter;
                var body = simpleLambda.Body;

                // Check if body is an xUnit assertion invocation
                if (body is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
                {
                    var methodName = memberAccess.Name.Identifier.Text;
                    var args = invocation.ArgumentList.Arguments;

                    ExpressionSyntax? predicateBody = methodName switch
                    {
                        "True" when args.Count >= 1 => args[0].Expression,
                        "False" when args.Count >= 1 => SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            SyntaxFactory.ParenthesizedExpression(args[0].Expression)),
                        "NotNull" when args.Count >= 1 => SyntaxFactory.BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            args[0].Expression,
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        "Null" when args.Count >= 1 => SyntaxFactory.BinaryExpression(
                            SyntaxKind.EqualsExpression,
                            args[0].Expression,
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        _ => null
                    };

                    if (predicateBody != null)
                    {
                        return SyntaxFactory.SimpleLambdaExpression(parameter, predicateBody)
                            .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                                .WithTrailingTrivia(SyntaxFactory.Space));
                    }
                }
            }
            else if (actionExpression is ParenthesizedLambdaExpressionSyntax parenLambda)
            {
                // Handle (item) => Assert.True(expr) pattern
                if (parenLambda.ParameterList.Parameters.Count == 1)
                {
                    var parameter = parenLambda.ParameterList.Parameters[0];
                    var body = parenLambda.Body;

                    if (body is InvocationExpressionSyntax invocation &&
                        invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                        memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" })
                    {
                        var methodName = memberAccess.Name.Identifier.Text;
                        var args = invocation.ArgumentList.Arguments;

                        ExpressionSyntax? predicateBody = methodName switch
                        {
                            "True" when args.Count >= 1 => args[0].Expression,
                            "False" when args.Count >= 1 => SyntaxFactory.PrefixUnaryExpression(
                                SyntaxKind.LogicalNotExpression,
                                SyntaxFactory.ParenthesizedExpression(args[0].Expression)),
                            "NotNull" when args.Count >= 1 => SyntaxFactory.BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                args[0].Expression,
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            "Null" when args.Count >= 1 => SyntaxFactory.BinaryExpression(
                                SyntaxKind.EqualsExpression,
                                args[0].Expression,
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                            _ => null
                        };

                        if (predicateBody != null)
                        {
                            // Convert to simple lambda for cleaner output
                            return SyntaxFactory.SimpleLambdaExpression(
                                SyntaxFactory.Parameter(parameter.Identifier),
                                predicateBody)
                                .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken)
                                    .WithTrailingTrivia(SyntaxFactory.Space));
                        }
                    }
                }
            }

            // Fallback: return the original expression as-is
            // This will likely cause a compilation error, prompting manual conversion
            return actionExpression;
        }

        private ExpressionSyntax ConvertThrowsAny(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Assert.ThrowsAny<T>(action) -> await Assert.ThrowsAsync<T>(action)
            // Note: ThrowsAny accepts derived types, ThrowsAsync should work similarly
            if (nameNode is GenericNameSyntax genericName)
            {
                var exceptionType = genericName.TypeArgumentList.Arguments[0];
                var action = invocation.ArgumentList.Arguments[0].Expression;

                var invocationExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.GenericName("ThrowsAsync")
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(exceptionType)
                                )
                            )
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(action)
                        )
                    )
                );

                var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword, invocationExpression);
            }

            return CreateTUnitAssertion("Throws", invocation.ArgumentList.Arguments[0].Expression);
        }

        private ExpressionSyntax ConvertThrowsAnyAsync(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Same as ThrowsAny but for async
            return ConvertThrowsAny(invocation, nameNode);
        }

        private ExpressionSyntax ConvertIsNotType(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Assert.IsNotType<T>(value) -> await Assert.That(value).IsNotTypeOf<T>()
            if (nameNode is GenericNameSyntax genericName)
            {
                var expectedType = genericName.TypeArgumentList.Arguments[0];
                var value = invocation.ArgumentList.Arguments[0].Expression;

                var assertThatInvocation = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.IdentifierName("That")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(value)
                        )
                    )
                );

                var methodAccess = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    assertThatInvocation,
                    SyntaxFactory.GenericName("IsNotTypeOf")
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(expectedType)
                            )
                        )
                );

                var fullInvocation = SyntaxFactory.InvocationExpression(methodAccess, SyntaxFactory.ArgumentList());
                var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword, fullInvocation);
            }

            return CreateTUnitAssertion("IsNotTypeOf", invocation.ArgumentList.Arguments[0].Expression);
        }

        private ExpressionSyntax ConvertThrows(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Assert.Throws<T>(action) -> await Assert.ThrowsAsync<T>(action)
            if (nameNode is GenericNameSyntax genericName)
            {
                var exceptionType = genericName.TypeArgumentList.Arguments[0];
                var action = invocation.ArgumentList.Arguments[0].Expression;

                var invocationExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.GenericName("ThrowsAsync")
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(exceptionType)
                                )
                            )
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(action)
                        )
                    )
                );

                var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword, invocationExpression);
            }

            // Fallback
            return CreateTUnitAssertion("Throws", invocation.ArgumentList.Arguments[0].Expression);
        }

        private ExpressionSyntax ConvertThrowsAsync(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Assert.ThrowsAsync<T>(asyncAction) -> await Assert.ThrowsAsync<T>(asyncAction)
            if (nameNode is GenericNameSyntax genericName)
            {
                var exceptionType = genericName.TypeArgumentList.Arguments[0];
                var action = invocation.ArgumentList.Arguments[0].Expression;

                var invocationExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.GenericName("ThrowsAsync")
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(exceptionType)
                                )
                            )
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(action)
                        )
                    )
                );

                var awaitKeyword2 = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword2, invocationExpression);
            }

            return CreateTUnitAssertion("ThrowsAsync", invocation.ArgumentList.Arguments[0].Expression);
        }

        private ExpressionSyntax ConvertIsType(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Assert.IsType<T>(value) -> await Assert.That(value).IsTypeOf<T>()
            if (nameNode is GenericNameSyntax genericName)
            {
                var expectedType = genericName.TypeArgumentList.Arguments[0];
                var value = invocation.ArgumentList.Arguments[0].Expression;

                var assertThatInvocation = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.IdentifierName("That")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(value)
                        )
                    )
                );

                var methodAccess = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    assertThatInvocation,
                    SyntaxFactory.GenericName("IsTypeOf")
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(expectedType)
                            )
                        )
                );

                var fullInvocation = SyntaxFactory.InvocationExpression(methodAccess, SyntaxFactory.ArgumentList());
                var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword, fullInvocation);
            }

            return CreateTUnitAssertion("IsTypeOf", invocation.ArgumentList.Arguments[0].Expression);
        }

        private ExpressionSyntax ConvertIsAssignableFrom(InvocationExpressionSyntax invocation, SimpleNameSyntax nameNode)
        {
            // Assert.IsAssignableFrom<T>(value) -> await Assert.That(value).IsAssignableTo<T>()
            if (nameNode is GenericNameSyntax genericName)
            {
                var expectedType = genericName.TypeArgumentList.Arguments[0];
                var value = invocation.ArgumentList.Arguments[0].Expression;

                var assertThatInvocation = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Assert"),
                        SyntaxFactory.IdentifierName("That")
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(value)
                        )
                    )
                );

                var methodAccess = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    assertThatInvocation,
                    SyntaxFactory.GenericName("IsAssignableTo")
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(expectedType)
                            )
                        )
                );

                var fullInvocation = SyntaxFactory.InvocationExpression(methodAccess, SyntaxFactory.ArgumentList());
                var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space);
                return SyntaxFactory.AwaitExpression(awaitKeyword, fullInvocation);
            }

            return CreateTUnitAssertion("IsAssignableTo", invocation.ArgumentList.Arguments[0].Expression);
        }
    }

    // Internal rewriter used by ApplyFrameworkSpecificConversions with compilation access
    private class XUnitAttributeRewriterInternal : CSharpSyntaxRewriter
    {
        private readonly Compilation _compilation;

        public XUnitAttributeRewriterInternal(Compilation compilation)
        {
            _compilation = compilation;
        }

        public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
        {
            var newAttributes = new List<AttributeSyntax>();
            var separators = new List<SyntaxToken>();

            // Preserve the original separators (commas with their trivia/spacing)
            var originalSeparators = node.Attributes.GetSeparators().ToList();

            for (int i = 0; i < node.Attributes.Count; i++)
            {
                var attr = node.Attributes[i];
                var name = GetSimpleName(attr);

                var converted = name switch
                {
                    "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute" => ConvertTestAttribute(attr),
                    "Trait" or "TraitAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Property"), attr.ArgumentList)],
                    "InlineData" or "InlineDataAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Arguments"), attr.ArgumentList)],
                    "MemberData" or "MemberDataAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("MethodDataSource"), attr.ArgumentList)],
                    "ClassData" or "ClassDataAttribute" =>
                    [
                        SyntaxFactory.Attribute(
                            SyntaxFactory.IdentifierName("MethodDataSource"),
                            CreateArgumentListWithAddedArgument(attr.ArgumentList ?? SyntaxFactory.AttributeArgumentList(),
                                SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal("GetEnumerator")))))
                    ],
                    "Collection" or "CollectionAttribute" => ConvertCollection(_compilation, attr),
                    "CollectionDefinition" or "CollectionDefinitionAttribute" => [SyntaxFactory.Attribute(
                        SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("Obsolete")))],
                    _ => [attr]
                };

                int attributesBeforeConversion = newAttributes.Count;
                newAttributes.AddRange(converted);
                int attributesAfterConversion = newAttributes.Count;

                // Add separators for the newly added attributes
                // If we added more than one attribute, add separators between them
                for (int j = attributesBeforeConversion; j < attributesAfterConversion - 1; j++)
                {
                    // Use the original separator if available, otherwise create one with space
                    var separator = i < originalSeparators.Count
                        ? originalSeparators[i]
                        : SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space);
                    separators.Add(separator);
                }

                // Add the original separator after this group of attributes if it exists
                if (i < originalSeparators.Count && attributesAfterConversion > attributesBeforeConversion)
                {
                    separators.Add(originalSeparators[i]);
                }
            }

            if (node.Attributes.SequenceEqual(newAttributes))
            {
                return node;
            }

            // Create separated list with preserved separators
            return SyntaxFactory.AttributeList(
                    SyntaxFactory.SeparatedList(newAttributes, separators))
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }
    }

    private class BaseTypeRewriter(INamedTypeSymbol namedTypeSymbol) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.BaseList is null)
            {
                return node;
            }

            INamedTypeSymbol[] types = namedTypeSymbol.BaseType != null && namedTypeSymbol.BaseType.SpecialType != SpecialType.System_Object
                ? [namedTypeSymbol.BaseType, .. namedTypeSymbol.AllInterfaces]
                : [.. namedTypeSymbol.AllInterfaces];

            var classFixturesToConvert = types
                .Where(x => x.Name == "IClassFixture" && x.ContainingNamespace.Name.StartsWith("Xunit"))
                .Select(x => SyntaxFactory.Attribute(
                    SyntaxFactory.GenericName(SyntaxFactory.ParseToken("ClassDataSource"), SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(x.TypeArguments.First().ToDisplayString())))).WithoutTrailingTrivia(),
                    SyntaxFactory.AttributeArgumentList()
                        .AddArguments(
                            SyntaxFactory.AttributeArgument(
                                nameEquals: SyntaxFactory.NameEquals("Shared"),
                                nameColon: null,
                                expression: SyntaxFactory.ParseExpression("SharedType.PerClass")
                            )
                        )
                ).WithLeadingTrivia(SyntaxFactory.ElasticMarker))
                .ToList();

            if (classFixturesToConvert.Count > 0)
            {
                node = node.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(classFixturesToConvert)));
            }

            var newBaseList = types.Where(x => !x.ContainingNamespace.Name.StartsWith("Xunit"))
                .Select(x => SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(x.ToDisplayString())))
                .ToList();

            if (newBaseList.Count == 0)
            {
                // When removing the entire base list, preserve the trailing trivia
                // The base list's trailing trivia typically contains the newline before the opening brace
                var baseListTrailingTrivia = node.BaseList.GetTrailingTrivia();

                // Apply the trivia to the element before the base list (parameter list or identifier)
                // REPLACE the trailing trivia rather than adding to it to avoid extra spaces
                if (node.ParameterList != null)
                {
                    node = node.WithParameterList(
                        node.ParameterList.WithTrailingTrivia(baseListTrailingTrivia));
                }
                else
                {
                    node = node.WithIdentifier(
                        node.Identifier.WithTrailingTrivia(baseListTrailingTrivia));
                }

                return node.WithBaseList(null);
            }

            var baseListSyntax = node.BaseList!.WithTypes(SyntaxFactory.SeparatedList<BaseTypeSyntax>(newBaseList));

            return node.WithBaseList(baseListSyntax);
        }
    }

    private class InitializeDisposeRewriter(INamedTypeSymbol namedTypeSymbol) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (node.BaseList is null)
            {
                return node;
            }

            var interfaces = namedTypeSymbol.Interfaces
                .Where(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) is "global::Xunit.IAsyncLifetime" or "global::System.IAsyncDisposable" or "global::System.IDisposable")
                .ToArray();

            if (interfaces.Length == 0)
            {
                return node;
            }

            var hasAsyncLifetime = interfaces.Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == "global::Xunit.IAsyncLifetime");
            var hasAsyncDisposable = interfaces.Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == "global::System.IAsyncDisposable");
            var hasDisposable = interfaces.Any(x => x.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) == "global::System.IDisposable");

            var isTestClass = namedTypeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Any(m => m.GetAttributes()
                    .Any(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) is "global::Xunit.FactAttribute"
                        or "global::Xunit.TheoryAttribute")
                );

            if (isTestClass)
            {
                // Collect all replacements first, then apply them together
                var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

                if (hasAsyncLifetime && GetInitializeMethod(node) is { } initializeMethod)
                {
                    var attributeList = SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("Before"), SyntaxFactory.ParseAttributeArgumentList("(Test)"))));

                    var newMethod = initializeMethod
                        .WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space))
                        .WithAttributeLists(SyntaxFactory.SingletonList(attributeList));

                    replacements[initializeMethod] = newMethod;
                }

                if ((hasAsyncLifetime || hasAsyncDisposable) && GetDisposeAsyncMethod(node) is { } disposeAsyncMethod)
                {
                    var attributeList = SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("After"), SyntaxFactory.ParseAttributeArgumentList("(Test)"))));

                    var newMethod = disposeAsyncMethod
                        .WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space))
                        .WithAttributeLists(SyntaxFactory.SingletonList(attributeList));

                    replacements[disposeAsyncMethod] = newMethod;
                }

                if (hasDisposable && GetDisposeMethod(node) is { } disposeMethod)
                {
                    var attributeList = SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName("After"), SyntaxFactory.ParseAttributeArgumentList("(Test)"))));

                    var newMethod = disposeMethod
                        .WithAttributeLists(SyntaxFactory.SingletonList(attributeList));

                    replacements[disposeMethod] = newMethod;
                }

                // Apply all replacements at once
                if (replacements.Count > 0)
                {
                    node = node.ReplaceNodes(replacements.Keys, (oldNode, _) => replacements[oldNode]);
                }

                // Reorder methods: Test methods should come first, then Before/After methods
                var testMethods = node.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.AttributeLists.Any(al => al.Attributes.Any(a =>
                        a.Name.ToString() is "Test" or "Fact" or "Theory")))
                    .ToList();

                var beforeAfterMethods = node.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.AttributeLists.Any(al => al.Attributes.Any(a =>
                        a.Name.ToString() is "Before" or "After")))
                    .ToList();

                var otherMembers = node.Members
                    .Except(testMethods.Cast<MemberDeclarationSyntax>())
                    .Except(beforeAfterMethods.Cast<MemberDeclarationSyntax>())
                    .ToList();

                // If we have both test methods and before/after methods, reorder
                if (testMethods.Count > 0 && beforeAfterMethods.Count > 0)
                {
                    // Normalize trivia: all members should have blank line before them (except first)
                    var allMethodsToReorder = new List<MethodDeclarationSyntax>();
                    allMethodsToReorder.AddRange(testMethods);
                    allMethodsToReorder.AddRange(beforeAfterMethods);

                    var normalizedMethods = allMethodsToReorder.Select((m, i) =>
                    {
                        // Strip existing leading and trailing trivia, then set normalized trivia
                        var strippedMethod = m.WithLeadingTrivia().WithTrailingTrivia();

                        // Check if method has attributes
                        var hasAttributes = strippedMethod.AttributeLists.Count > 0;

                        if (hasAttributes)
                        {
                            // For methods with attributes, we need to:
                            // 1. Set trivia on the attribute's first token
                            // 2. Strip the attribute list's trailing trivia
                            // 3. Set trivia on the first modifier token

                            var firstToken = strippedMethod.GetFirstToken(); // This is the '[' token

                            if (i == 0)
                            {
                                // First method: just indentation on attribute
                                strippedMethod = strippedMethod.ReplaceToken(
                                    firstToken,
                                    firstToken.WithLeadingTrivia(SyntaxFactory.Whitespace("    ")));
                            }
                            else
                            {
                                // Subsequent methods: blank line + indentation on attribute
                                strippedMethod = strippedMethod.ReplaceToken(
                                    firstToken,
                                    firstToken.WithLeadingTrivia(
                                        SyntaxFactory.CarriageReturnLineFeed,
                                        SyntaxFactory.Whitespace("    ")));
                            }

                            // Strip trailing trivia from all attribute lists to prevent extra newlines
                            var attributeListsWithoutTrailing = strippedMethod.AttributeLists
                                .Select(al => al.WithTrailingTrivia())
                                .ToList();
                            strippedMethod = strippedMethod.WithAttributeLists(
                                SyntaxFactory.List(attributeListsWithoutTrailing));

                            // Now get the first modifier AFTER the replacements
                            var firstModifier = strippedMethod.Modifiers.FirstOrDefault();

                            // Add newline + indentation before the modifier (public, etc.)
                            if (firstModifier != default)
                            {
                                strippedMethod = strippedMethod.ReplaceToken(
                                    firstModifier,
                                    firstModifier.WithLeadingTrivia(
                                        SyntaxFactory.CarriageReturnLineFeed,
                                        SyntaxFactory.Whitespace("    ")));
                            }

                            return strippedMethod.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                        }
                        else
                        {
                            // No attributes: set trivia on first token (modifier or return type)
                            if (i == 0)
                            {
                                // First method: just indentation
                                return strippedMethod
                                    .WithLeadingTrivia(SyntaxFactory.Whitespace("    "))
                                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                            }
                            else
                            {
                                // Subsequent methods: blank line + indentation
                                return strippedMethod.WithLeadingTrivia(
                                    SyntaxFactory.CarriageReturnLineFeed,
                                    SyntaxFactory.Whitespace("    ")
                                ).WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                            }
                        }
                    }).ToList();

                    // New order: other members, then all normalized methods
                    var reorderedMembers = new List<MemberDeclarationSyntax>();
                    reorderedMembers.AddRange(otherMembers);
                    reorderedMembers.AddRange(normalizedMethods);

                    node = node.WithMembers(SyntaxFactory.List(reorderedMembers));
                }

                // Update base list to remove interfaces
                var interfacesToRemove = new[] { "IAsyncLifetime", "IAsyncDisposable", "IDisposable" };
                var newBaseTypes = node.BaseList!.Types
                    .Where(x => !interfacesToRemove.Any(i => x.Type.TryGetInferredMemberName()?.EndsWith(i) == true))
                    .ToList();

                if (newBaseTypes.Count != node.BaseList.Types.Count)
                {
                    if (newBaseTypes.Count == 0)
                    {
                        // When removing the entire base list, preserve the trailing trivia
                        // The base list's trailing trivia typically contains the newline before the opening brace
                        var baseListTrailingTrivia = node.BaseList.GetTrailingTrivia();

                        // Apply the trivia to the element before the base list (parameter list or identifier)
                        // REPLACE the trailing trivia rather than adding to it to avoid extra spaces
                        if (node.ParameterList != null)
                        {
                            node = node.WithParameterList(
                                node.ParameterList.WithTrailingTrivia(baseListTrailingTrivia));
                        }
                        else
                        {
                            node = node.WithIdentifier(
                                node.Identifier.WithTrailingTrivia(baseListTrailingTrivia));
                        }

                        node = node.WithBaseList(null);
                    }
                    else
                    {
                        node = node.WithBaseList(
                            SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>(newBaseTypes))
                                .WithTrailingTrivia(node.BaseList.GetTrailingTrivia()));
                    }
                }
            }
            else
            {
                if (hasAsyncLifetime && GetInitializeMethod(node) is { } initializeMethod)
                {
                    node = node
                        .ReplaceNode(initializeMethod, initializeMethod.WithReturnType(SyntaxFactory.ParseTypeName("Task").WithTrailingTrivia(SyntaxFactory.Space)));

                    node = node.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(
                        [
                            ..node.BaseList!.Types.Where(x => x.Type.TryGetInferredMemberName()?.EndsWith("IAsyncLifetime") is null or false),
                            SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IAsyncInitializer"))
                        ])))
                        .WithTrailingTrivia(node.BaseList.GetTrailingTrivia());
                }

                if (hasAsyncLifetime && !hasAsyncDisposable)
                {
                    node = node
                        .WithBaseList(node.BaseList!.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IAsyncDisposable"))));
                }
            }

            return node;

            MethodDeclarationSyntax? GetInitializeMethod(ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == "InitializeAsync");
            }

            MethodDeclarationSyntax? GetDisposeAsyncMethod(ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == "DisposeAsync");
            }

            MethodDeclarationSyntax? GetDisposeMethod(ClassDeclarationSyntax classDeclaration)
            {
                return classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == "Dispose");
            }
        }
    }

    private static void UpdateSyntaxTrees(ref Compilation compilation, ref SyntaxTree syntaxTree, ref SyntaxNode updatedRoot)
    {
        var parseOptions = syntaxTree.Options;
        var newSyntaxTree = updatedRoot.SyntaxTree;

        // If the parse options differ, re-parse the updatedRoot with the correct options
        if (!Equals(newSyntaxTree.Options, parseOptions))
        {
            newSyntaxTree = CSharpSyntaxTree.ParseText(
                updatedRoot.ToFullString(),
                (CSharpParseOptions) parseOptions,
                syntaxTree.FilePath
            );
        }

        compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);
        syntaxTree = newSyntaxTree;

        updatedRoot = newSyntaxTree.GetRoot();
    }
}
