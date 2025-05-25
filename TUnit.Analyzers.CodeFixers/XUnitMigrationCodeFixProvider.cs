using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitMigrationCodeFixProvider)), Shared]
public class XUnitMigrationCodeFixProvider : CodeFixProvider
{
    public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(Rules.XunitMigration.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Rules.XunitMigration.Title.ToString(),
                    createChangedDocument: c => ConvertCodeAsync(context.Document, root?.FindNode(diagnosticSpan), c),
                    equivalenceKey: Rules.XunitMigration.Title.ToString()),
                diagnostic);
        }
    }
    
    private static async Task<Document> ConvertCodeAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        await UpdateClassAttributesAsync(editor, root);
        
        return editor.GetChangedDocument();
    }
    
    private static async Task UpdateClassAttributesAsync(DocumentEditor editor, SyntaxNode root)
    {
        // Find the class declaration by name
        var classDeclarations = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>();

        foreach (var classDeclaration in classDeclarations)
        {
            foreach (var attributeSyntax in classDeclaration.AttributeLists.SelectMany(x => x.Attributes))
            {
                await ConvertAttribute(editor, attributeSyntax);
            }

            foreach (var methodDeclaration in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                foreach (var attributeSyntax in methodDeclaration.AttributeLists.SelectMany(x => x.Attributes))
                {
                    await ConvertAttribute(editor, attributeSyntax);
                }
            }
        }
    }

    private static async Task ConvertAttribute(DocumentEditor editor, AttributeSyntax attributeSyntax)
    {
        var name = GetSimpleName(attributeSyntax);

        var newSyntaxes = name switch
        {
            "Fact" or "FactAttribute"
                or "Theory" or "TheoryAttribute" => ConvertTestAttribute(attributeSyntax),
            
            "Trait" or "TraitAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Property"),
                attributeSyntax.ArgumentList)],

            "InlineData" or "InlineDataAttribute" => [SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Arguments"),
                attributeSyntax.ArgumentList)],

            "MemberData" or "MemberDataAttribute" => [SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodDataSource"), attributeSyntax.ArgumentList)],

            "ClassData" or "ClassDataAttribute" => [SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodDataSource"),
                (attributeSyntax.ArgumentList ?? SyntaxFactory.AttributeArgumentList()).AddArguments(
                    SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("GetEnumerator")))))],

            "Collection" or "CollectionAttribute" => await ConvertCollection(editor, attributeSyntax),

            "CollectionDefinition" or "CollectionDefinitionAttribute" => [SyntaxFactory.Attribute(
                SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("Obsolete")))],

            _ => [attributeSyntax]
        };

        var list = (AttributeListSyntax)attributeSyntax.Parent!;
        
        var newAttributeList = list
            .RemoveNode(attributeSyntax, SyntaxRemoveOptions.AddElasticMarker)!
            .AddAttributes(newSyntaxes.ToArray());
        
        editor.ReplaceNode(list, newAttributeList);
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

        if (attributeSyntax.ArgumentList?.Arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ValueText == "Skip") is {} skip)
        {
            yield return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Skip"))
                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(skip.Expression));
        }
    }

    private static async Task<IEnumerable<AttributeSyntax>> ConvertCollection(DocumentEditor editor, AttributeSyntax attributeSyntax)
    {
        var compilation = await editor.GetChangedDocument().Project.GetCompilationAsync();

        if (compilation is null)
        {
            return [attributeSyntax];
        }
        
        var collectionDefinition = GetCollectionAttribute(compilation, attributeSyntax);

        if (collectionDefinition is null)
        {
            return [attributeSyntax];
        }

        var disableParallelism =
            collectionDefinition.ArgumentList?.Arguments.Any(
                x => x.NameEquals?.Name.Identifier.Text == "DisableParallelization"
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
                )
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

    private static TypeArgumentListSyntax GetGenericArguments(Compilation compilation, Document document,
        AttributeSyntax attributeSyntax)
    {
        var collectionAttribute = GetCollectionAttribute(compilation, attributeSyntax);

        if (collectionAttribute is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }

        var classDeclaration = collectionAttribute.Parent?.Parent;
        
        if (classDeclaration is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }
        
        var classSymbol = compilation.GetSemanticModel(classDeclaration.SyntaxTree).GetDeclaredSymbol(classDeclaration);
        
        if (classSymbol is not ITypeSymbol typeSymbol)
        {
            return SyntaxFactory.TypeArgumentList();
        }
        
        var interfaceType = typeSymbol.AllInterfaces.FirstOrDefault(x => x.Name == "ICollectionFixture");

        if (interfaceType is null)
        {
            return SyntaxFactory.TypeArgumentList();
        }
        
        return SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
            SyntaxFactory.IdentifierName(interfaceType.TypeArguments.First().Name)
        ));
    }

    private static AttributeSyntax? GetCollectionAttribute(Compilation compilation, AttributeSyntax attributeSyntax)
    {
        var firstToken = attributeSyntax.ArgumentList?.Arguments.FirstOrDefault()?.GetFirstToken();

        if (!firstToken.HasValue)
        {
            return null;
        }

        var collectionName = firstToken.Value.IsKind(SyntaxKind.NameOfKeyword)
            ? GetNameFromNameOfToken(firstToken.Value) : firstToken.Value.ValueText;

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
                    ? GetNameFromNameOfToken(syntaxToken.Value) : syntaxToken.Value.ValueText;
                
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
}