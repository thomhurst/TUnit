using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MSTestMigrationCodeFixProvider)), Shared]
public class MSTestMigrationCodeFixProvider : BaseMigrationCodeFixProvider
{
    protected override string FrameworkName => "MSTest";
    protected override string DiagnosticId => Rules.MSTestMigration.Id;
    protected override string CodeFixTitle => "Convert MSTest code to TUnit";
    
    protected override AttributeRewriter CreateAttributeRewriter(Compilation compilation)
    {
        return new MSTestAttributeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new MSTestAssertionRewriter(semanticModel);
    }

    protected override CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new MSTestBaseTypeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation)
    {
        return new MSTestLifecycleRewriter();
    }

    protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation)
    {
        // MSTest-specific conversions if needed
        return compilationUnit;
    }
}

public class MSTestAttributeRewriter : AttributeRewriter
{
    protected override string FrameworkName => "MSTest";
    
    protected override bool IsFrameworkAttribute(string attributeName)
    {
        return attributeName switch
        {
            "TestClass" or "TestMethod" or "DataRow" or "DynamicData" or
            "TestInitialize" or "TestCleanup" or "ClassInitialize" or "ClassCleanup" or
            "TestCategory" or "Ignore" or "Priority" or "Owner" => true,
            _ => false
        };
    }
    
    protected override AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName)
    {
        return attributeName switch
        {
            "DataRow" => argumentList, // Arguments attribute uses the same format
            "DynamicData" => ConvertDynamicDataArguments(argumentList),
            "TestCategory" => ConvertTestCategoryArguments(argumentList),
            "Priority" => ConvertPriorityArguments(argumentList),
            "ClassInitialize" or "ClassCleanup" => null, // These don't need arguments in TUnit
            _ => argumentList
        };
    }
    
    private AttributeArgumentListSyntax ConvertDynamicDataArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert DynamicData to MethodDataSource
        if (argumentList.Arguments.Count > 0)
        {
            var firstArg = argumentList.Arguments[0];
            
            // If it's a nameof expression, keep it as is
            if (firstArg.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } })
            {
                return SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(firstArg)
                );
            }
            
            // If it's a string literal, keep just the method name
            if (firstArg.Expression is LiteralExpressionSyntax literal)
            {
                return SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(firstArg)
                );
            }
        }
        
        return argumentList;
    }
    
    private AttributeArgumentListSyntax ConvertTestCategoryArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert TestCategory to Property
        var arguments = new List<AttributeArgumentSyntax>();
        
        arguments.Add(SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, 
                SyntaxFactory.Literal("Category"))
        ));
        
        if (argumentList.Arguments.Count > 0)
        {
            arguments.Add(argumentList.Arguments[0]);
        }
        
        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments));
    }
    
    private AttributeArgumentListSyntax ConvertPriorityArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert Priority to Property
        var arguments = new List<AttributeArgumentSyntax>();
        
        arguments.Add(SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, 
                SyntaxFactory.Literal("Priority"))
        ));
        
        if (argumentList.Arguments.Count > 0)
        {
            arguments.Add(SyntaxFactory.AttributeArgument(
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(argumentList.Arguments[0].Expression.ToString()))
            ));
        }
        
        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(arguments));
    }
    
    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        // Handle ClassInitialize and ClassCleanup specially - they need static context parameter removed
        var attributes = new List<AttributeSyntax>();
        
        foreach (var attribute in node.Attributes)
        {
            var attributeName = MigrationHelpers.GetAttributeName(attribute);
            
            if (attributeName is "ClassInitialize" or "ClassCleanup")
            {
                var hookType = attributeName == "ClassInitialize" ? "Before" : "After";
                var newAttribute = SyntaxFactory.Attribute(
                    SyntaxFactory.IdentifierName(hookType),
                    SyntaxFactory.AttributeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.AttributeArgument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("HookType"),
                                    SyntaxFactory.IdentifierName("Class")
                                )
                            )
                        )
                    )
                );
                return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(newAttribute));
            }
        }
        
        return base.VisitAttributeList(node);
    }
}

public class MSTestAssertionRewriter : AssertionRewriter
{
    protected override string FrameworkName => "MSTest";
    
    public MSTestAssertionRewriter(SemanticModel semanticModel) : base(semanticModel)
    {
    }
    
    protected override bool IsFrameworkAssertionNamespace(string namespaceName)
    {
        return namespaceName == "Microsoft.VisualStudio.TestTools.UnitTesting" || 
               namespaceName.StartsWith("Microsoft.VisualStudio.TestTools.UnitTesting.");
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
            return ConvertMSTestAssertion(invocation, memberAccess.Name.Identifier.Text);
        }
        
        // Handle CollectionAssert
        if (invocation.Expression is MemberAccessExpressionSyntax collectionAccess &&
            collectionAccess.Expression is IdentifierNameSyntax { Identifier.Text: "CollectionAssert" })
        {
            return ConvertCollectionAssertion(invocation, collectionAccess.Name.Identifier.Text);
        }
        
        // Handle StringAssert
        if (invocation.Expression is MemberAccessExpressionSyntax stringAccess &&
            stringAccess.Expression is IdentifierNameSyntax { Identifier.Text: "StringAssert" })
        {
            return ConvertStringAssertion(invocation, stringAccess.Name.Identifier.Text);
        }
        
        return null;
    }
    
    private ExpressionSyntax? ConvertMSTestAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;
        
        return methodName switch
        {
            "AreEqual" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsEqualTo", arguments[1].Expression, arguments[0]),
            "AreNotEqual" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsNotEqualTo", arguments[1].Expression, arguments[0]),
            "IsTrue" when arguments.Count >= 1 => 
                CreateTUnitAssertion("IsTrue", arguments[0].Expression),
            "IsFalse" when arguments.Count >= 1 => 
                CreateTUnitAssertion("IsFalse", arguments[0].Expression),
            "IsNull" when arguments.Count >= 1 => 
                CreateTUnitAssertion("IsNull", arguments[0].Expression),
            "IsNotNull" when arguments.Count >= 1 => 
                CreateTUnitAssertion("IsNotNull", arguments[0].Expression),
            "AreSame" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsSameReference", arguments[1].Expression, arguments[0]),
            "AreNotSame" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsNotSameReference", arguments[1].Expression, arguments[0]),
            "IsInstanceOfType" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsAssignableTo", arguments[0].Expression, arguments[1]),
            "IsNotInstanceOfType" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsNotAssignableTo", arguments[0].Expression, arguments[1]),
            "ThrowsException" when arguments.Count >= 1 =>
                CreateThrowsAssertion(invocation),
            "ThrowsExceptionAsync" when arguments.Count >= 1 =>
                CreateThrowsAsyncAssertion(invocation),
            "Fail" => CreateFailAssertion(arguments),
            _ => null
        };
    }
    
    private ExpressionSyntax? ConvertCollectionAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;
        
        return methodName switch
        {
            "AreEqual" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsEquivalentTo", arguments[1].Expression, arguments[0]),
            "AreNotEqual" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsNotEquivalentTo", arguments[1].Expression, arguments[0]),
            "Contains" when arguments.Count >= 2 => 
                CreateTUnitAssertion("Contains", arguments[0].Expression, arguments[1]),
            "DoesNotContain" when arguments.Count >= 2 => 
                CreateTUnitAssertion("DoesNotContain", arguments[0].Expression, arguments[1]),
            "AllItemsAreNotNull" when arguments.Count >= 1 => 
                CreateTUnitAssertion("AllSatisfy", arguments[0].Expression, 
                    SyntaxFactory.Argument(
                        SyntaxFactory.SimpleLambdaExpression(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.NotEqualsExpression,
                                SyntaxFactory.IdentifierName("x"),
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                            )
                        )
                    )),
            _ => null
        };
    }
    
    private ExpressionSyntax? ConvertStringAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;
        
        return methodName switch
        {
            "Contains" when arguments.Count >= 2 => 
                CreateTUnitAssertion("Contains", arguments[0].Expression, arguments[1]),
            "DoesNotMatch" when arguments.Count >= 2 => 
                CreateTUnitAssertion("DoesNotMatch", arguments[0].Expression, arguments[1]),
            "EndsWith" when arguments.Count >= 2 => 
                CreateTUnitAssertion("EndsWith", arguments[0].Expression, arguments[1]),
            "Matches" when arguments.Count >= 2 => 
                CreateTUnitAssertion("Matches", arguments[0].Expression, arguments[1]),
            "StartsWith" when arguments.Count >= 2 => 
                CreateTUnitAssertion("StartsWith", arguments[0].Expression, arguments[1]),
            _ => null
        };
    }
    
    private ExpressionSyntax CreateThrowsAssertion(InvocationExpressionSyntax invocation)
    {
        // Convert Assert.ThrowsException<T>(action) to await Assert.ThrowsAsync<T>(action)
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
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

            return SyntaxFactory.AwaitExpression(invocationExpression);
        }

        return CreateTUnitAssertion("Throws", invocation.ArgumentList.Arguments[0].Expression);
    }
    
    private ExpressionSyntax CreateThrowsAsyncAssertion(InvocationExpressionSyntax invocation)
    {
        // Similar to CreateThrowsAssertion but for async
        return CreateThrowsAssertion(invocation);
    }
    
    private ExpressionSyntax CreateFailAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Convert Assert.Fail(message) to await Assert.Fail(message)
        var failInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Fail")
            ),
            arguments.Count > 0
                ? SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(arguments[0]))
                : SyntaxFactory.ArgumentList()
        );

        return SyntaxFactory.AwaitExpression(failInvocation);
    }
}

public class MSTestBaseTypeRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // MSTest doesn't require specific base classes
        return base.VisitClassDeclaration(node);
    }
}

public class MSTestLifecycleRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Handle ClassInitialize, ClassCleanup, TestInitialize, TestCleanup - remove TestContext parameter where applicable
        var lifecycleAttributes = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Select(a => MigrationHelpers.GetAttributeName(a))
            .ToList();

        var hasClassLifecycle = lifecycleAttributes.Any(name => name is "ClassInitialize" or "ClassCleanup");
        var hasTestLifecycle = lifecycleAttributes.Any(name => name is "TestInitialize" or "TestCleanup");

        if (hasClassLifecycle || hasTestLifecycle)
        {
            // Remove TestContext parameter if present
            var parameters = node.ParameterList?.Parameters ?? default;
            if (parameters.Count == 1 && parameters[0].Type?.ToString().Contains("TestContext") == true)
            {
                node = node.WithParameterList(SyntaxFactory.ParameterList());
            }

            // Make sure method is public
            if (!node.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                node = node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            }

            // Make sure ClassInitialize/ClassCleanup are static
            if (hasClassLifecycle && !node.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                node = node.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            }
        }

        return base.VisitMethodDeclaration(node);
    }
}