using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.CodeFixers.Base;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NUnitMigrationCodeFixProvider)), Shared]
public class NUnitMigrationCodeFixProvider : BaseMigrationCodeFixProvider
{
    protected override string FrameworkName => "NUnit";
    protected override string DiagnosticId => Rules.NUnitMigration.Id;
    protected override string CodeFixTitle => "Convert NUnit code to TUnit";
    
    protected override AttributeRewriter CreateAttributeRewriter(Compilation compilation)
    {
        return new NUnitAttributeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateAssertionRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new NUnitAssertionRewriter(semanticModel);
    }

    protected override CSharpSyntaxRewriter CreateBaseTypeRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        return new NUnitBaseTypeRewriter();
    }

    protected override CSharpSyntaxRewriter CreateLifecycleRewriter(Compilation compilation)
    {
        return new NUnitLifecycleRewriter();
    }

    protected override CompilationUnitSyntax ApplyFrameworkSpecificConversions(CompilationUnitSyntax compilationUnit, SemanticModel semanticModel, Compilation compilation)
    {
        // NUnit-specific conversions if needed
        return compilationUnit;
    }
}

public class NUnitAttributeRewriter : AttributeRewriter
{
    protected override string FrameworkName => "NUnit";
    
    protected override bool IsFrameworkAttribute(string attributeName)
    {
        return attributeName switch
        {
            "Test" or "TestCase" or "TestCaseSource" or 
            "SetUp" or "TearDown" or "OneTimeSetUp" or "OneTimeTearDown" or
            "TestFixture" or "Category" or "Ignore" or "Explicit" => true,
            _ => false
        };
    }
    
    protected override AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName)
    {
        return attributeName switch
        {
            "TestCase" => argumentList, // Arguments attribute uses the same format
            "TestCaseSource" => ConvertTestCaseSourceArguments(argumentList),
            "Category" => ConvertCategoryArguments(argumentList),
            _ => argumentList
        };
    }
    
    private AttributeArgumentListSyntax ConvertTestCaseSourceArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert TestCaseSource to MethodDataSource
        if (argumentList.Arguments.Count > 0)
        {
            var firstArg = argumentList.Arguments[0];
            
            // If it's a nameof expression, keep it as is
            if (firstArg.Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } })
            {
                return argumentList;
            }
            
            // If it's a string literal, wrap it in quotes if needed
            if (firstArg.Expression is LiteralExpressionSyntax literal)
            {
                return argumentList;
            }
        }
        
        return argumentList;
    }
    
    private AttributeArgumentListSyntax ConvertCategoryArguments(AttributeArgumentListSyntax argumentList)
    {
        // Convert Category to Property
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
}

public class NUnitAssertionRewriter : AssertionRewriter
{
    protected override string FrameworkName => "NUnit";
    
    public NUnitAssertionRewriter(SemanticModel semanticModel) : base(semanticModel)
    {
    }
    
    protected override bool IsFrameworkAssertionNamespace(string namespaceName)
    {
        // Exclude NUnit.Framework.Legacy - ClassicAssert should not be converted
        return (namespaceName == "NUnit.Framework" || namespaceName.StartsWith("NUnit.Framework."))
               && namespaceName != "NUnit.Framework.Legacy";
    }
    
    protected override ExpressionSyntax? ConvertAssertionIfNeeded(InvocationExpressionSyntax invocation)
    {
        if (!IsFrameworkAssertion(invocation))
        {
            return null;
        }
        
        // Handle Assert.That(value, constraint)
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name.Identifier.Text == "That" &&
            invocation.ArgumentList.Arguments.Count >= 2)
        {
            return ConvertAssertThat(invocation);
        }
        
        // Handle classic assertions like Assert.AreEqual, ClassicAssert.AreEqual, etc.
        if (invocation.Expression is MemberAccessExpressionSyntax classicMemberAccess &&
            classicMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Assert" or "ClassicAssert" })
        {
            return ConvertClassicAssertion(invocation, classicMemberAccess.Name.Identifier.Text);
        }
        
        return null;
    }
    
    private ExpressionSyntax ConvertAssertThat(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        var actualValue = arguments[0].Expression;
        var constraint = arguments[1].Expression;
        
        // Parse the constraint to determine the TUnit assertion method
        if (constraint is InvocationExpressionSyntax constraintInvocation)
        {
            return ConvertConstraintToTUnit(actualValue, constraintInvocation);
        }
        
        if (constraint is MemberAccessExpressionSyntax constraintMember)
        {
            return ConvertConstraintMemberToTUnit(actualValue, constraintMember);
        }
        
        return CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint));
    }
    
    private ExpressionSyntax ConvertConstraintToTUnit(ExpressionSyntax actualValue, InvocationExpressionSyntax constraint)
    {
        if (constraint.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.Text;

            // Handle Does.StartWith, Does.EndWith, Contains.Substring
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" or "Contains" })
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertion("StartsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertion("EndsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "Substring" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                };
            }

            return methodName switch
            {
                "EqualTo" => CreateTUnitAssertion("IsEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThan" => CreateTUnitAssertion("IsGreaterThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "LessThan" => CreateTUnitAssertion("IsLessThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "Contains" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "StartsWith" => CreateTUnitAssertion("StartsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "EndsWith" => CreateTUnitAssertion("EndsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
            };
        }

        return CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint));
    }
    
    private ExpressionSyntax ConvertConstraintMemberToTUnit(ExpressionSyntax actualValue, MemberAccessExpressionSyntax constraint)
    {
        var memberName = constraint.Name.Identifier.Text;

        // Handle Is.Not.X patterns
        if (constraint.Expression is MemberAccessExpressionSyntax innerMemberAccess &&
            innerMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            innerMemberAccess.Name.Identifier.Text == "Not")
        {
            return memberName switch
            {
                "Null" => CreateTUnitAssertion("IsNotNull", actualValue),
                "Empty" => CreateTUnitAssertion("IsNotEmpty", actualValue),
                _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
            };
        }

        return memberName switch
        {
            "True" => CreateTUnitAssertion("IsTrue", actualValue),
            "False" => CreateTUnitAssertion("IsFalse", actualValue),
            "Null" => CreateTUnitAssertion("IsNull", actualValue),
            "Empty" => CreateTUnitAssertion("IsEmpty", actualValue),
            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
        };
    }
    
    private ExpressionSyntax? ConvertClassicAssertion(InvocationExpressionSyntax invocation, string methodName)
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
            "IsEmpty" when arguments.Count >= 1 => 
                CreateTUnitAssertion("IsEmpty", arguments[0].Expression),
            "IsNotEmpty" when arguments.Count >= 1 => 
                CreateTUnitAssertion("IsNotEmpty", arguments[0].Expression),
            "Greater" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsGreaterThan", arguments[0].Expression, arguments[1]),
            "Less" when arguments.Count >= 2 => 
                CreateTUnitAssertion("IsLessThan", arguments[0].Expression, arguments[1]),
            _ => null
        };
    }
}

public class NUnitBaseTypeRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // NUnit doesn't require specific base classes, but might have IDisposable for cleanup
        // For now, just return the node as is
        return base.VisitClassDeclaration(node);
    }
}

public class NUnitLifecycleRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // Lifecycle methods are handled by attribute conversion
        // Just ensure they're public and have correct signature
        var hasLifecycleAttribute = node.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(a => a.Name.ToString() is "Before" or "After");
        
        if (hasLifecycleAttribute && !node.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return node.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }
        
        return base.VisitMethodDeclaration(node);
    }
}