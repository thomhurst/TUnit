// using System.Collections.Immutable;
// using System.Composition;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CodeActions;
// using Microsoft.CodeAnalysis.CodeFixes;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using TUnit.Analyzers.CodeFixers.Extensions;
//
// namespace TUnit.Analyzers.CodeFixers;
//
// [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XUnitClassFixtureCodeFixProvider)), Shared]
// public class XUnitClassFixtureCodeFixProvider : CodeFixProvider
// {
//     public override sealed ImmutableArray<string> FixableDiagnosticIds { get; } =
//         ImmutableArray.Create(Rules.XunitClassFixtures.Id);
//
//     public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
//
//     public override sealed async Task RegisterCodeFixesAsync(CodeFixContext context)
//     {
//         foreach (var diagnostic in context.Diagnostics)
//         {
//             var diagnosticSpan = diagnostic.Location.SourceSpan;
//
//             var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
//
//             context.RegisterCodeFix(
//                 CodeAction.Create(
//                     title: Rules.XunitClassFixtures.Title.ToString(),
//                     createChangedDocument: c => ConvertAttributesAsync(context.Document, root?.FindNode(diagnosticSpan), c),
//                     equivalenceKey: Rules.XunitClassFixtures.Title.ToString()),
//                 diagnostic);
//         }
//     }
//
//     private static async Task<Document> ConvertAttributesAsync(Document document, SyntaxNode? node, CancellationToken cancellationToken)
//     {
//         if (node is not SimpleBaseTypeSyntax simpleBaseTypeSyntax)
//         {
//             return document;
//         }
//
//         var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
//
//         if (root is null)
//         {
//             return document;
//         }
//         
//         var newExpression = GetNewExpression(simpleBaseTypeSyntax);
//
//         if (newExpression != null)
//         {
//             var classDeclaration = GetClassDeclaration(simpleBaseTypeSyntax)!;
//
//             SyntaxNode toRemove = classDeclaration.BaseList?.ChildNodes().Count() > 1
//                 ? simpleBaseTypeSyntax
//                 : classDeclaration.BaseList!;
//
//             root = root.ReplaceNode(classDeclaration,
//                 classDeclaration
//                     .RemoveNode(toRemove, SyntaxRemoveOptions.AddElasticMarker)!
//                     .AddAttributeLists(
//                         SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(newExpression))
//                     )!
//             );
//             
//             var compilationUnit = root as CompilationUnitSyntax;
//
//             if (compilationUnit is null)
//             {
//                 return document.WithSyntaxRoot(root);
//             }
//         }
//
//         return document.WithSyntaxRoot(root);
//     }
//
//     private static ClassDeclarationSyntax? GetClassDeclaration(SimpleBaseTypeSyntax simpleBaseTypeSyntax)
//     {
//         var parent = simpleBaseTypeSyntax.Parent;
//
//         while (parent != null && !parent.IsKind(SyntaxKind.ClassDeclaration))
//         {
//             parent = parent.Parent;
//         }
//
//         return parent as ClassDeclarationSyntax;
//     }
//
//     private static AttributeSyntax? GetNewExpression(SimpleBaseTypeSyntax simpleBaseTypeSyntax)
//     {
//         if (simpleBaseTypeSyntax.Type is not GenericNameSyntax genericNameSyntax
//             || !genericNameSyntax.TypeArgumentList.Arguments.Any())
//         {
//             return null;
//         }
//
//         return SyntaxFactory.Attribute(
//             SyntaxFactory.GenericName(SyntaxFactory.ParseToken("ClassDataSource"), genericNameSyntax.TypeArgumentList).WithoutTrailingTrivia(),
//             SyntaxFactory.AttributeArgumentList()
//                 .AddArguments(
//                     SyntaxFactory.AttributeArgument(
//                         nameEquals: SyntaxFactory.NameEquals("Shared"),
//                         nameColon: null,
//                         expression: SyntaxFactory.ParseExpression("SharedType.PerClass")
//                     )
//                 )
//         ).WithLeadingTrivia(SyntaxFactory.ElasticMarker);
//     }
// }