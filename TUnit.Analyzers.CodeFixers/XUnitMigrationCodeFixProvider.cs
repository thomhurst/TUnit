using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitMigrationCodeFixProvider)), Shared]
public class XUnitMigrationCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitMigration.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitMigration.Title.ToString(),
                    createChangedDocument: c => ConvertCodeAsync(context.Document, c),
                    equivalenceKey: Rules.XunitMigration.Title.ToString()),
                diagnostic);
        }
        
        return Task.CompletedTask;
    }

    private static async Task<Document> ConvertCodeAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }
        
        var compilation = await document.Project.GetCompilationAsync(cancellationToken);

        if (compilation is null)
        {
            return document;
        }
        
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        
        if (semanticModel is null)
        {
            return document;
        }
        
        var syntaxTree = root.SyntaxTree;
        
        // Always use the latest updatedRoot as input for the next transformation
        var updatedRoot = UpdateInitializeDispose(compilation, root);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, updatedRoot);

        updatedRoot = UpdateClassAttributes(compilation, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, updatedRoot);

        updatedRoot = RemoveInterfacesAndBaseClasses(compilation, updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, updatedRoot);

        updatedRoot = RemoveUsingDirectives(updatedRoot);
        UpdateSyntaxTrees(ref compilation, ref syntaxTree, updatedRoot);

        // Apply all changes in one step
        return document.WithSyntaxRoot(updatedRoot);
    }

    private static SyntaxNode UpdateInitializeDispose(Compilation compilation, SyntaxNode root)
    {
        // Always operate on the latest root
        var currentRoot = root;
        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList())
        {
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclaration) is not { } symbol)
            {
                continue;
            }

            // Always get the latest node from the current root
            var currentClass = currentRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(n => n.SpanStart == classDeclaration.SpanStart && n.Identifier.Text == classDeclaration.Identifier.Text);

            if (currentClass == null)
                continue;

            var newNode = new InitializeDisposeRewriter(symbol).VisitClassDeclaration(currentClass);

            if (!ReferenceEquals(currentClass, newNode))
                currentRoot = currentRoot.ReplaceNode(currentClass, newNode);
        }

        return currentRoot;
    }

    private static SyntaxNode RemoveInterfacesAndBaseClasses(Compilation compilation, SyntaxNode root)
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
                continue;

            var newNode = new BaseTypeRewriter(symbol).VisitClassDeclaration(currentClass);

            if (newNode != null && !ReferenceEquals(currentClass, newNode))
                currentRoot = currentRoot.ReplaceNode(currentClass, newNode);
        }

        return currentRoot;
    }

    private static SyntaxNode RemoveUsingDirectives(SyntaxNode updatedRoot)
    {
        return updatedRoot.RemoveNodes(
            updatedRoot.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Where(x => x.Name?.ToString().StartsWith("Xunit") is true),
            SyntaxRemoveOptions.AddElasticMarker)!;
    }

    private static SyntaxNode UpdateClassAttributes(Compilation compilation, SyntaxNode root)
    {
        // Always operate on the latest root
        var rewriter = new AttributeRewriter(compilation);
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

    private class AttributeRewriter(Compilation compilation) : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitAttributeList(AttributeListSyntax node)
        {
            var newAttributes = new List<AttributeSyntax>();

            foreach (var attr in node.Attributes)
            {
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
                            (attr.ArgumentList ?? SyntaxFactory.AttributeArgumentList()).AddArguments(
                                SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal("GetEnumerator")))))
                    ],
                    "Collection" or "CollectionAttribute" => ConvertCollection(compilation, attr),
                    "CollectionDefinition" or "CollectionDefinitionAttribute" => [SyntaxFactory.Attribute(
                        SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("Obsolete")))],
                    _ => [attr]
                };

                newAttributes.AddRange(converted);
            }

            // Preserve original trivia instead of forcing elastic trivia
            return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(newAttributes))
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
                ? [namedTypeSymbol.BaseType, ..namedTypeSymbol.AllInterfaces] 
                : [..namedTypeSymbol.AllInterfaces];
            
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
            
            if(classFixturesToConvert.Count > 0)
            {
                node = node.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(classFixturesToConvert)));
            }

            var newBaseList = types.Where(x => !x.ContainingNamespace.Name.StartsWith("Xunit"))
                .Select(x => SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(x.ToDisplayString())))
                .ToList();
            
            if (newBaseList.Count == 0)
            {
                // Preserve original trivia instead of forcing elastic trivia
                return node.WithBaseList(null)
                    .WithOpenBraceToken(node.OpenBraceToken.WithLeadingTrivia(node.BaseList!.GetTrailingTrivia()));
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

            var interfaces = namedTypeSymbol.AllInterfaces
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
            
            var initializeMethod = node.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "InitializeAsync");
            
            var disposeAsyncMethod = node.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "DisposeAsync");
            
            var disposeMethod = node.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "Dispose");

            if (isTestClass)
            {
                if (hasAsyncLifetime && initializeMethod is not null)
                {
                    node = node.RemoveNode(initializeMethod, SyntaxRemoveOptions.AddElasticMarker)!;
                }
                
                if ((hasAsyncLifetime || hasAsyncDisposable) && disposeAsyncMethod is not null)
                {
                    node = node.RemoveNode(disposeAsyncMethod, SyntaxRemoveOptions.AddElasticMarker)!;
                }
                
                if (hasDisposable && disposeMethod is not null)
                {
                    node = node.RemoveNode(disposeMethod, SyntaxRemoveOptions.AddElasticMarker)!;
                }
                
                if(hasAsyncLifetime && initializeMethod is not null)
                {
                    node = node
                            
                        .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(
                            node.BaseList!.Types.Where(x => x.Type.TryGetInferredMemberName()?.EndsWith("IAsyncLifetime") is null or false)))).WithTrailingTrivia(node.BaseList.GetTrailingTrivia())
                        .AddMembers(
                            SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task"), "InitializeAsync")
                                .WithModifiers(initializeMethod.Modifiers)
                                .WithBody(initializeMethod.Body)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                .WithAttributeLists(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.AttributeList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Attribute(SyntaxFactory.ParseName("Before"), SyntaxFactory.ParseAttributeArgumentList("Test")))
                                        )
                                    )
                                )
                        );
                }
                
                if((hasAsyncLifetime || hasAsyncDisposable) && disposeMethod is not null)
                {
                    node = node
                        .RemoveNode(disposeMethod!, SyntaxRemoveOptions.AddElasticMarker)!
                        .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(
                            node.BaseList!.Types.Where(x => x.Type.TryGetInferredMemberName()?.EndsWith("IAsyncDisposable") is null or false)))).WithTrailingTrivia(node.BaseList.GetTrailingTrivia())
                        .AddMembers(
                            SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task"), "DisposeAsync")
                                .WithModifiers(disposeMethod.Modifiers)
                                .WithBody(disposeMethod?.Body)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                .WithAttributeLists(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.AttributeList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Attribute(SyntaxFactory.ParseName("After"), SyntaxFactory.ParseAttributeArgumentList("Test")))
                                        )
                                    )
                                )
                        );
                }
                
                if(hasDisposable && disposeMethod is not null)
                {
                    node = node
                        .RemoveNode(disposeMethod!, SyntaxRemoveOptions.AddElasticMarker)!
                        .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(
                            node.BaseList!.Types.Where(x => x.Type.TryGetInferredMemberName()?.EndsWith("IDisposable") is null or false)))).WithTrailingTrivia(node.BaseList.GetTrailingTrivia())
                        .AddMembers(
                            SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Dispose")
                                .WithModifiers(disposeMethod.Modifiers)
                                .WithBody(disposeMethod?.Body)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                .WithAttributeLists(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.AttributeList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Attribute(SyntaxFactory.ParseName("After"), SyntaxFactory.ParseAttributeArgumentList("Test")))
                                        )
                                    )
                                )
                        );
                }
            }
            else
            {
                if (hasAsyncLifetime && initializeMethod is not null)
                {
                    node = node
                        .WithBaseList(SyntaxFactory.BaseList(node.BaseList.Types.Remove(node.BaseList.Types.First(x => x.Type.TryGetInferredMemberName()!.EndsWith("IAsyncLifetime"))))).WithTrailingTrivia(node.BaseList.GetTrailingTrivia())
                        .ReplaceNode(initializeMethod, initializeMethod.WithReturnType(SyntaxFactory.ParseTypeName("Task")));
                }
            }

            if (node.BaseList is not null && node.BaseList.Types.Count == 0)
            {
                node = node.WithBaseList(null)
                    .WithOpenBraceToken(node.OpenBraceToken.WithLeadingTrivia(node.BaseList.GetTrailingTrivia()));
            }

            return node.NormalizeWhitespace();
        }
    }

    private static void UpdateSyntaxTrees(ref Compilation compilation, ref SyntaxTree syntaxTree, SyntaxNode updatedRoot)
    {
        compilation = compilation.ReplaceSyntaxTree(syntaxTree, updatedRoot.SyntaxTree);
        syntaxTree = updatedRoot.SyntaxTree;
    }
}
