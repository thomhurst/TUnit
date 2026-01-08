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
        // Extract TestCase properties FIRST (before ExpectedResult conversion changes the attributes)
        // Maps: TestName → DisplayName, Category → Category, Description/Author → Property, Explicit → Explicit
        var testCasePropertyRewriter = new NUnitTestCasePropertyRewriter();
        compilationUnit = (CompilationUnitSyntax)testCasePropertyRewriter.Visit(compilationUnit);

        // Transform ExpectedResult patterns (TestCase with ExpectedResult → Arguments with assertion)
        var expectedResultRewriter = new NUnitExpectedResultRewriter(semanticModel);
        compilationUnit = (CompilationUnitSyntax)expectedResultRewriter.Visit(compilationUnit);

        return compilationUnit;
    }

    /// <summary>
    /// NUnit allows [TestCase] alone, but TUnit requires [Test] + [Arguments].
    /// </summary>
    protected override bool ShouldEnsureTestAttribute() => true;
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
            "TestFixture" or "Category" or "Ignore" or "Explicit" or "Apartment" => true,
            _ => false
        };
    }
    
    protected override AttributeSyntax? ConvertAttribute(AttributeSyntax attribute)
    {
        var attributeName = MigrationHelpers.GetAttributeName(attribute);
        
        // Special handling for [Apartment(ApartmentState.STA)] -> [STAThreadExecutor]
        if (attributeName == "Apartment")
        {
            return ConvertApartmentAttribute(attribute);
        }
        
        return base.ConvertAttribute(attribute);
    }
    
    private AttributeSyntax? ConvertApartmentAttribute(AttributeSyntax attribute)
    {
        // Check if the argument is ApartmentState.STA
        if (attribute.ArgumentList?.Arguments.Count > 0)
        {
            var arg = attribute.ArgumentList.Arguments[0].Expression;
            
            // Check for ApartmentState.STA pattern
            if (arg is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "STA")
            {
                // Convert to [STAThreadExecutor]
                return SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("STAThreadExecutor"));
            }
        }
        
        // For other ApartmentState values, we can't convert automatically - return null to remove
        // Add TODO comment would be ideal but can't easily add comments on attributes
        return null;
    }
    
    protected override AttributeArgumentListSyntax? ConvertAttributeArguments(AttributeArgumentListSyntax argumentList, string attributeName)
    {
        return attributeName switch
        {
            "TestCase" => ConvertTestCaseArguments(argumentList),
            "TestCaseSource" => ConvertTestCaseSourceArguments(argumentList),
            "Category" => ConvertCategoryArguments(argumentList),
            _ => argumentList
        };
    }

    private AttributeArgumentListSyntax ConvertTestCaseArguments(AttributeArgumentListSyntax argumentList)
    {
        var newArgs = new List<AttributeArgumentSyntax>();
        var categories = new List<ExpressionSyntax>();

        foreach (var arg in argumentList.Arguments)
        {
            var namedProperty = arg.NameEquals?.Name.Identifier.Text;

            if (namedProperty == null)
            {
                // Positional argument - keep it
                newArgs.Add(arg);
            }
            else if (namedProperty == "Ignore" || namedProperty == "IgnoreReason")
            {
                // Map NUnit's Ignore/IgnoreReason to TUnit's Skip
                var skipArg = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Skip")),
                    null,
                    arg.Expression);
                newArgs.Add(skipArg);
            }
            else if (namedProperty == "TestName")
            {
                // Map NUnit's TestName to TUnit's DisplayName inline on [Arguments]
                var displayNameArg = SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("DisplayName")),
                    null,
                    arg.Expression);
                newArgs.Add(displayNameArg);
            }
            else if (namedProperty == "Category")
            {
                // Collect categories to create a Categories array
                categories.Add(arg.Expression);
            }
            else if (namedProperty is "Description" or "Author" or "Explicit" or "ExplicitReason")
            {
                // These properties are converted to separate TUnit attributes by NUnitTestCasePropertyRewriter:
                // Description/Author → [Property], Explicit → [Explicit]
                // Skip them here - they don't belong in the [Arguments] attribute
            }
            else if (namedProperty == "ExpectedResult")
            {
                // ExpectedResult is handled by NUnitExpectedResultRewriter
                // If we get here, it's a case without the ExpectedResult transformation, skip it
            }
            else
            {
                // Other named arguments are preserved as-is
                newArgs.Add(arg);
            }
        }

        // Add Categories array if any categories were found
        if (categories.Count > 0)
        {
            var categoriesArray = SyntaxFactory.CollectionExpression(
                SyntaxFactory.SeparatedList(
                    categories.Select(c => (CollectionElementSyntax)SyntaxFactory.ExpressionElement(c))));

            var categoriesArg = SyntaxFactory.AttributeArgument(
                SyntaxFactory.NameEquals(SyntaxFactory.IdentifierName("Categories")),
                null,
                categoriesArray);
            newArgs.Add(categoriesArg);
        }

        return SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(newArgs));
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
        // TUnit has a native Category attribute with the same signature as NUnit
        // [Category("Unit")] in NUnit -> [Category("Unit")] in TUnit
        return argumentList;
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

        // Capture the optional message argument (3rd argument)
        ExpressionSyntax? message = null;
        if (arguments.Count >= 3)
        {
            message = arguments[2].Expression;
        }

        // Parse the constraint to determine the TUnit assertion method
        if (constraint is InvocationExpressionSyntax constraintInvocation)
        {
            return ConvertConstraintToTUnitWithMessage(actualValue, constraintInvocation, message);
        }

        if (constraint is MemberAccessExpressionSyntax constraintMember)
        {
            return ConvertConstraintMemberToTUnitWithMessage(actualValue, constraintMember, message);
        }

        return CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint));
    }

    private ExpressionSyntax ConvertConstraintToTUnitWithMessage(ExpressionSyntax actualValue, InvocationExpressionSyntax constraint, ExpressionSyntax? message)
    {
        if (constraint.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Get method name - handle both regular and generic method names
            var methodName = memberAccess.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.Text,
                IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
                _ => memberAccess.Name.ToString()
            };

            // Handle chained constraint modifiers like .Within(delta) on Is.EqualTo(x).Within(delta)
            if (methodName == "Within" && memberAccess.Expression is InvocationExpressionSyntax innerConstraint)
            {
                // Get the base assertion (e.g., IsEqualTo(5)) first
                var baseAssertion = ConvertConstraintToTUnitWithMessage(actualValue, innerConstraint, message);
                
                // Now chain .Within(delta) to it
                return ChainMethodCall(baseAssertion, "Within", constraint.ArgumentList.Arguments.ToArray());
            }

            // Handle generic type constraints: Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
            if (memberAccess.Name is GenericNameSyntax genericMethodName)
            {
                var typeArg = genericMethodName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg != null)
                {
                    // Handle Is.Not.TypeOf<T>(), Is.Not.InstanceOf<T>()
                    if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccessGeneric &&
                        chainedAccessGeneric.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                        chainedAccessGeneric.Name.Identifier.Text == "Not")
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsNotTypeOf", actualValue, typeArg, message),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, message),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, message),
                            _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                        };
                    }

                    // Handle Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
                    if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" })
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsTypeOf", actualValue, typeArg, message),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, message),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, message),
                            _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                        };
                    }
                }
            }

            // Handle Is.Not.EqualTo, Is.Not.GreaterThan, etc. (invocation patterns)
            if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccess &&
                chainedAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                chainedAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "EqualTo" => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThan" => CreateTUnitAssertionWithMessage("IsLessThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThan" => CreateTUnitAssertionWithMessage("IsGreaterThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsLessThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsGreaterThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "SameAs" => CreateTUnitAssertionWithMessage("IsNotSameReferenceAs", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "InstanceOf" => CreateTUnitAssertionWithMessage("IsNotAssignableTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "TypeOf" => CreateTUnitAssertionWithMessage("IsNotTypeOf", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Does.Not.StartWith, Does.Not.EndWith, Does.Not.Contain
            if (memberAccess.Expression is MemberAccessExpressionSyntax doesNotAccess &&
                doesNotAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" } &&
                doesNotAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertionWithMessage("DoesNotStartWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertionWithMessage("DoesNotEndWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "Contain" => CreateTUnitAssertionWithMessage("DoesNotContain", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("DoesNotContain", actualValue, message, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Has.Member(item) -> Contains(item)
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" })
            {
                return methodName switch
                {
                    "Member" => CreateTUnitAssertionWithMessage("Contains", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                };
            }

            // Handle Does.StartWith, Does.EndWith, Contains.Substring
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" or "Contains" })
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertionWithMessage("StartsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertionWithMessage("EndsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    "Substring" => CreateTUnitAssertionWithMessage("Contains", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
                };
            }

            return methodName switch
            {
                "EqualTo" => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThan" => CreateTUnitAssertionWithMessage("IsGreaterThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "LessThan" => CreateTUnitAssertionWithMessage("IsLessThan", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "GreaterThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsGreaterThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "LessThanOrEqualTo" => CreateTUnitAssertionWithMessage("IsLessThanOrEqualTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "Contains" => CreateTUnitAssertionWithMessage("Contains", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "StartsWith" => CreateTUnitAssertionWithMessage("StartsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "EndsWith" => CreateTUnitAssertionWithMessage("EndsWith", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "SameAs" => CreateTUnitAssertionWithMessage("IsSameReferenceAs", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "InstanceOf" => CreateTUnitAssertionWithMessage("IsAssignableTo", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                "TypeOf" => CreateTUnitAssertionWithMessage("IsTypeOf", actualValue, message, constraint.ArgumentList.Arguments.ToArray()),
                _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
            };
        }

        return CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint));
    }

    private ExpressionSyntax ConvertConstraintMemberToTUnitWithMessage(ExpressionSyntax actualValue, MemberAccessExpressionSyntax constraint, ExpressionSyntax? message)
    {
        var memberName = constraint.Name.Identifier.Text;

        // Handle Is.Not.X patterns (member access, not invocation)
        if (constraint.Expression is MemberAccessExpressionSyntax innerMemberAccess &&
            innerMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            innerMemberAccess.Name.Identifier.Text == "Not")
        {
            return memberName switch
            {
                "Null" => CreateTUnitAssertionWithMessage("IsNotNull", actualValue, message),
                "Empty" => CreateTUnitAssertionWithMessage("IsNotEmpty", actualValue, message),
                "True" => CreateTUnitAssertionWithMessage("IsFalse", actualValue, message),
                "False" => CreateTUnitAssertionWithMessage("IsTrue", actualValue, message),
                "Positive" => CreateTUnitAssertionWithMessage("IsLessThanOrEqualTo", actualValue, message, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Negative" => CreateTUnitAssertionWithMessage("IsGreaterThanOrEqualTo", actualValue, message, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Zero" => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                _ => CreateTUnitAssertionWithMessage("IsNotEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
            };
        }

        return memberName switch
        {
            "True" => CreateTUnitAssertionWithMessage("IsTrue", actualValue, message),
            "False" => CreateTUnitAssertionWithMessage("IsFalse", actualValue, message),
            "Null" => CreateTUnitAssertionWithMessage("IsNull", actualValue, message),
            "Empty" => CreateTUnitAssertionWithMessage("IsEmpty", actualValue, message),
            "Positive" => CreateTUnitAssertionWithMessage("IsPositive", actualValue, message),
            "Negative" => CreateTUnitAssertionWithMessage("IsNegative", actualValue, message),
            "Zero" => CreateTUnitAssertionWithMessage("IsZero", actualValue, message),
            _ => CreateTUnitAssertionWithMessage("IsEqualTo", actualValue, message, SyntaxFactory.Argument(constraint))
        };
    }

    private ExpressionSyntax ConvertConstraintToTUnit(ExpressionSyntax actualValue, InvocationExpressionSyntax constraint)
    {
        if (constraint.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Get method name - handle both regular and generic method names
            var methodName = memberAccess.Name switch
            {
                GenericNameSyntax genericName => genericName.Identifier.Text,
                IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
                _ => memberAccess.Name.ToString()
            };

            // Handle chained constraint modifiers like .Within(delta) on Is.EqualTo(x).Within(delta)
            if (methodName == "Within" && memberAccess.Expression is InvocationExpressionSyntax innerConstraint)
            {
                // Get the base assertion (e.g., IsEqualTo(5)) first
                var baseAssertion = ConvertConstraintToTUnit(actualValue, innerConstraint);
                
                // Now chain .Within(delta) to it
                return ChainMethodCall(baseAssertion, "Within", constraint.ArgumentList.Arguments.ToArray());
            }

            // Handle generic type constraints: Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
            if (memberAccess.Name is GenericNameSyntax genericMethodName)
            {
                var typeArg = genericMethodName.TypeArgumentList.Arguments.FirstOrDefault();
                if (typeArg != null)
                {
                    // Handle Is.Not.TypeOf<T>(), Is.Not.InstanceOf<T>()
                    if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccessGeneric &&
                        chainedAccessGeneric.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                        chainedAccessGeneric.Name.Identifier.Text == "Not")
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsNotTypeOf", actualValue, typeArg, null),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, null),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsNotAssignableTo", actualValue, typeArg, null),
                            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                        };
                    }

                    // Handle Is.TypeOf<T>(), Is.InstanceOf<T>(), Is.AssignableFrom<T>()
                    if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" })
                    {
                        return methodName switch
                        {
                            "TypeOf" => CreateTUnitGenericAssertion("IsTypeOf", actualValue, typeArg, null),
                            "InstanceOf" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, null),
                            "AssignableFrom" => CreateTUnitGenericAssertion("IsAssignableTo", actualValue, typeArg, null),
                            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                        };
                    }
                }
            }

            // Handle Is.Not.EqualTo, Is.Not.GreaterThan, etc. (invocation patterns)
            if (memberAccess.Expression is MemberAccessExpressionSyntax chainedAccess &&
                chainedAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
                chainedAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "EqualTo" => CreateTUnitAssertion("IsNotEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThan" => CreateTUnitAssertion("IsLessThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThan" => CreateTUnitAssertion("IsGreaterThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "GreaterThanOrEqualTo" => CreateTUnitAssertion("IsLessThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "LessThanOrEqualTo" => CreateTUnitAssertion("IsGreaterThan", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "SameAs" => CreateTUnitAssertion("IsNotSameReferenceAs", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "InstanceOf" => CreateTUnitAssertion("IsNotAssignableTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "TypeOf" => CreateTUnitAssertion("IsNotTypeOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("IsNotEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Does.Not.StartWith, Does.Not.EndWith, Does.Not.Contain
            if (memberAccess.Expression is MemberAccessExpressionSyntax doesNotAccess &&
                doesNotAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Does" } &&
                doesNotAccess.Name.Identifier.Text == "Not")
            {
                return methodName switch
                {
                    "StartWith" => CreateTUnitAssertion("DoesNotStartWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "EndWith" => CreateTUnitAssertion("DoesNotEndWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    "Contain" => CreateTUnitAssertion("DoesNotContain", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("DoesNotContain", actualValue, constraint.ArgumentList.Arguments.ToArray())
                };
            }

            // Handle Has.Member(item) -> Contains(item)
            if (memberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Has" })
            {
                return methodName switch
                {
                    "Member" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                    _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
                };
            }

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
                "GreaterThanOrEqualTo" => CreateTUnitAssertion("IsGreaterThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "LessThanOrEqualTo" => CreateTUnitAssertion("IsLessThanOrEqualTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "Contains" => CreateTUnitAssertion("Contains", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "StartsWith" => CreateTUnitAssertion("StartsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "EndsWith" => CreateTUnitAssertion("EndsWith", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "SameAs" => CreateTUnitAssertion("IsSameReferenceAs", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "InstanceOf" => CreateTUnitAssertion("IsAssignableTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "TypeOf" => CreateTUnitAssertion("IsTypeOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "SubsetOf" => CreateTUnitAssertion("IsSubsetOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "SupersetOf" => CreateTUnitAssertion("IsSupersetOf", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "EquivalentTo" => CreateTUnitAssertion("IsEquivalentTo", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "Matches" => CreateTUnitAssertion("Matches", actualValue, constraint.ArgumentList.Arguments.ToArray()),
                "InRange" => CreateInRangeAssertion(actualValue, constraint.ArgumentList.Arguments),
                _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
            };
        }

        return CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint));
    }
    
    private ExpressionSyntax ConvertConstraintMemberToTUnit(ExpressionSyntax actualValue, MemberAccessExpressionSyntax constraint)
    {
        var memberName = constraint.Name.Identifier.Text;

        // Handle Is.Not.X patterns (member access, not invocation)
        if (constraint.Expression is MemberAccessExpressionSyntax innerMemberAccess &&
            innerMemberAccess.Expression is IdentifierNameSyntax { Identifier.Text: "Is" } &&
            innerMemberAccess.Name.Identifier.Text == "Not")
        {
            return memberName switch
            {
                "Null" => CreateTUnitAssertion("IsNotNull", actualValue),
                "Empty" => CreateTUnitAssertion("IsNotEmpty", actualValue),
                "True" => CreateTUnitAssertion("IsFalse", actualValue),
                "False" => CreateTUnitAssertion("IsTrue", actualValue),
                "Positive" => CreateTUnitAssertion("IsLessThanOrEqualTo", actualValue, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Negative" => CreateTUnitAssertion("IsGreaterThanOrEqualTo", actualValue, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                "Zero" => CreateTUnitAssertion("IsNotEqualTo", actualValue, SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))),
                _ => CreateTUnitAssertion("IsNotEqualTo", actualValue, SyntaxFactory.Argument(constraint))
            };
        }

        return memberName switch
        {
            "True" => CreateTUnitAssertion("IsTrue", actualValue),
            "False" => CreateTUnitAssertion("IsFalse", actualValue),
            "Null" => CreateTUnitAssertion("IsNull", actualValue),
            "Empty" => CreateTUnitAssertion("IsEmpty", actualValue),
            "Positive" => CreateTUnitAssertion("IsPositive", actualValue),
            "Negative" => CreateTUnitAssertion("IsNegative", actualValue),
            "Zero" => CreateTUnitAssertion("IsZero", actualValue),
            "Unique" => CreateTUnitAssertion("HasDistinctItems", actualValue),
            "Ordered" => CreateTUnitAssertion("IsInAscendingOrder", actualValue),
            _ => CreateTUnitAssertion("IsEqualTo", actualValue, SyntaxFactory.Argument(constraint))
        };
    }

    private ExpressionSyntax CreateInRangeAssertion(ExpressionSyntax actualValue, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Is.InRange(low, high) -> IsInRange(low, high)
        if (arguments.Count >= 2)
        {
            return CreateTUnitAssertion("IsInRange", actualValue, arguments[0], arguments[1]);
        }
        return CreateTUnitAssertion("IsInRange", actualValue);
    }

    /// <summary>
    /// Chains a method call onto an existing await expression.
    /// For example: await Assert.That(x).IsEqualTo(5) becomes await Assert.That(x).IsEqualTo(5).Within(2)
    /// </summary>
    private ExpressionSyntax ChainMethodCall(ExpressionSyntax baseExpression, string methodName, params ArgumentSyntax[] arguments)
    {
        // The base expression is an AwaitExpression like: await Assert.That(x).IsEqualTo(5)
        // We need to extract the invocation, add .Within(2) to it, and re-wrap in await
        if (baseExpression is AwaitExpressionSyntax awaitExpr)
        {
            var innerInvocation = awaitExpr.Expression;
            
            // Create the chained method access: Assert.That(x).IsEqualTo(5).Within
            var chainedAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                innerInvocation,
                SyntaxFactory.IdentifierName(methodName)
            );
            
            // Create the invocation: Assert.That(x).IsEqualTo(5).Within(2)
            var chainedInvocation = SyntaxFactory.InvocationExpression(
                chainedAccess,
                arguments.Length > 0
                    ? SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))
                    : SyntaxFactory.ArgumentList()
            );
            
            // Re-wrap in await
            var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
                .WithTrailingTrivia(SyntaxFactory.Space);
            return SyntaxFactory.AwaitExpression(awaitKeyword, chainedInvocation);
        }
        
        // Fallback: just return the base expression if it's not the expected shape
        return baseExpression;
    }
    
    private ExpressionSyntax? ConvertClassicAssertion(InvocationExpressionSyntax invocation, string methodName)
    {
        var arguments = invocation.ArgumentList.Arguments;

        // Handle Assert.Throws<T> and Assert.ThrowsAsync<T> first (generic methods)
        if (methodName is "Throws" or "ThrowsAsync")
        {
            return ConvertNUnitThrows(invocation);
        }

        // Handle Assert.DoesNotThrow and Assert.DoesNotThrowAsync
        if (methodName is "DoesNotThrow" or "DoesNotThrowAsync")
        {
            return ConvertDoesNotThrow(arguments);
        }

        // Handle special assertions (Pass, Inconclusive, Fail, Warn)
        return methodName switch
        {
            // Pass and Fail
            "Pass" => CreatePassAssertion(arguments),
            "Fail" => CreateFailAssertion(arguments),
            "Inconclusive" => CreateSkipAssertion(arguments),
            "Ignore" => CreateSkipAssertion(arguments),

            // 2-arg assertions (expected, actual) with optional message at index 2
            "AreEqual" when arguments.Count >= 2 => ConvertAreEqualWithComparer(arguments),
            "AreNotEqual" when arguments.Count >= 2 => ConvertAreNotEqualWithMessage(arguments),
            "AreSame" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsSameReferenceAs", arguments),
            "AreNotSame" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsNotSameReferenceAs", arguments),
            "Greater" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsGreaterThan", arguments, swapArgs: false),
            "GreaterOrEqual" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsGreaterThanOrEqualTo", arguments, swapArgs: false),
            "Less" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsLessThan", arguments, swapArgs: false),
            "LessOrEqual" when arguments.Count >= 2 => ConvertTwoArgWithMessage("IsLessThanOrEqualTo", arguments, swapArgs: false),

            // 1-arg assertions with optional message at index 1
            "IsTrue" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsTrue", arguments),
            "IsFalse" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsFalse", arguments),
            "IsNull" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNull", arguments),
            "IsNotNull" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNotNull", arguments),
            "IsEmpty" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsEmpty", arguments),
            "IsNotEmpty" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNotEmpty", arguments),
            "IsNaN" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNaN", arguments),
            "IsInstanceOf" when arguments.Count >= 2 => ConvertInstanceOf(arguments, isNegated: false),
            "IsNotInstanceOf" when arguments.Count >= 2 => ConvertInstanceOf(arguments, isNegated: true),

            // Collection assertions
            "Contains" when arguments.Count >= 2 => ConvertTwoArgWithMessage("Contains", arguments, swapArgs: false),

            // Comparison assertions
            "Positive" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsPositive", arguments),
            "Negative" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNegative", arguments),
            "Zero" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsZero", arguments),
            "NotZero" when arguments.Count >= 1 => ConvertOneArgWithMessage("IsNotZero", arguments),

            _ => null
        };
    }

    private ExpressionSyntax ConvertOneArgWithMessage(string methodName, SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var actualValue = arguments[0].Expression;
        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 1);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage(methodName, actualValue, messageExpr);
    }

    private ExpressionSyntax ConvertTwoArgWithMessage(string methodName, SeparatedSyntaxList<ArgumentSyntax> arguments, bool swapArgs = true)
    {
        // For most NUnit assertions: expected is first, actual is second
        // For TUnit: actual is first, expected goes in the method call
        var actualValue = swapArgs ? arguments[1].Expression : arguments[0].Expression;
        var expectedArg = swapArgs ? arguments[0] : arguments[1];
        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage(methodName, actualValue, messageExpr, expectedArg);
    }

    private ExpressionSyntax ConvertAreEqualWithComparer(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var expected = arguments[0];
        var actual = arguments[1].Expression;

        // Check if 3rd argument is a comparer (not a string message)
        if (arguments.Count >= 3 && IsLikelyComparerArgument(arguments[2]) == true)
        {
            // Add TODO comment and skip the comparer
            var result = CreateTUnitAssertion("IsEqualTo", actual, expected);
            return result.WithLeadingTrivia(
                CreateTodoComment("custom comparer was used - consider using Assert.That(...).IsEquivalentTo() or a custom condition."),
                SyntaxFactory.EndOfLine("\n"),
                SyntaxFactory.Whitespace("                "));
        }

        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage("IsEqualTo", actual, messageExpr, expected);
    }

    private ExpressionSyntax ConvertAreNotEqualWithMessage(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var expected = arguments[0];
        var actual = arguments[1].Expression;

        // Check if 3rd argument is a comparer
        if (arguments.Count >= 3 && IsLikelyComparerArgument(arguments[2]) == true)
        {
            var result = CreateTUnitAssertion("IsNotEqualTo", actual, expected);
            return result.WithLeadingTrivia(
                CreateTodoComment("custom comparer was used - consider using a custom condition."),
                SyntaxFactory.EndOfLine("\n"),
                SyntaxFactory.Whitespace("                "));
        }

        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage("IsNotEqualTo", actual, messageExpr, expected);
    }

    private ExpressionSyntax ConvertInstanceOf(SeparatedSyntaxList<ArgumentSyntax> arguments, bool isNegated)
    {
        var actualValue = arguments[0].Expression;
        var expectedType = arguments[1];
        var methodName = isNegated ? "IsNotAssignableTo" : "IsAssignableTo";
        var (message, formatArgs) = ExtractMessageWithFormatArgs(arguments, 2);
        var messageExpr = message != null ? CreateMessageExpression(message, formatArgs) : null;
        return CreateTUnitAssertionWithMessage(methodName, actualValue, messageExpr, expectedType);
    }

    private ExpressionSyntax ConvertNUnitThrows(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            // Handle generic form: Assert.Throws<T>(() => ...) or Assert.ThrowsAsync<T>(() => ...)
            if (memberAccess.Name is GenericNameSyntax genericName)
            {
                var exceptionType = genericName.TypeArgumentList.Arguments[0];
                var action = invocation.ArgumentList.Arguments[0].Expression;

                var throwsAsyncInvocation = SyntaxFactory.InvocationExpression(
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

                return SyntaxFactory.AwaitExpression(throwsAsyncInvocation);
            }
            
            // Handle non-generic constraint-based form: Assert.Throws(constraint, () => ...) or Assert.ThrowsAsync(constraint, () => ...)
            // where constraint is typically Is.TypeOf(typeof(T))
            if (invocation.ArgumentList.Arguments.Count >= 2)
            {
                var constraint = invocation.ArgumentList.Arguments[0].Expression;
                var action = invocation.ArgumentList.Arguments[1].Expression;
                
                // Try to extract the exception type from the constraint
                var exceptionType = TryExtractTypeFromConstraint(constraint);
                
                if (exceptionType != null)
                {
                    // Convert to generic ThrowsAsync form: Assert.ThrowsAsync<T>(() => ...)
                    var throwsAsyncInvocation = SyntaxFactory.InvocationExpression(
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

                    return SyntaxFactory.AwaitExpression(throwsAsyncInvocation);
                }
            }
        }

        // Fallback for unsupported Throws patterns
        // If we have 2+ arguments, it's a constraint-based form where arg[1] is the action
        // Otherwise, it's a single-argument form where arg[0] is the action
        var fallbackArg = invocation.ArgumentList.Arguments.Count >= 2
            ? invocation.ArgumentList.Arguments[1].Expression
            : invocation.ArgumentList.Arguments[0].Expression;
        return CreateTUnitAssertion("Throws", fallbackArg);
    }

    private ExpressionSyntax ConvertDoesNotThrow(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // Assert.DoesNotThrow(() => action) -> await Assert.That(() => action).ThrowsNothing()
        if (arguments.Count == 0)
        {
            // Fallback - shouldn't happen but handle gracefully
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Assert"),
                    SyntaxFactory.IdentifierName("Pass")
                )
            );
        }

        var action = arguments[0].Expression;
        
        // Create Assert.That(() => action)
        var assertThatInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("That")
            ),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(action)
                )
            )
        );
        
        // Chain .ThrowsNothing()
        var throwsNothingInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                assertThatInvocation,
                SyntaxFactory.IdentifierName("ThrowsNothing")
            ),
            SyntaxFactory.ArgumentList()
        );
        
        // Wrap in await
        var awaitKeyword = SyntaxFactory.Token(SyntaxKind.AwaitKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);
        return SyntaxFactory.AwaitExpression(awaitKeyword, throwsNothingInvocation);
    }
    
    /// <summary>
    /// Attempts to extract the exception type from NUnit constraint expressions like Is.TypeOf(typeof(T)).
    /// Returns null if the type cannot be extracted.
    /// </summary>
    private TypeSyntax? TryExtractTypeFromConstraint(ExpressionSyntax constraint)
    {
        // Handle Is.TypeOf(typeof(T)) pattern
        if (constraint is InvocationExpressionSyntax invocation)
        {
            // Check if it's a method call like Is.TypeOf(...) or TypeOf(...)
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "TypeOf" &&
                invocation.ArgumentList.Arguments.Count > 0)
            {
                // Extract the argument to TypeOf - should be typeof(T)
                var typeofArg = invocation.ArgumentList.Arguments[0].Expression;
                return ExtractTypeFromTypeof(typeofArg);
            }
            
            // Handle standalone TypeOf(typeof(T)) calls
            if (invocation.Expression is IdentifierNameSyntax { Identifier.Text: "TypeOf" } &&
                invocation.ArgumentList.Arguments.Count > 0)
            {
                var typeofArg = invocation.ArgumentList.Arguments[0].Expression;
                return ExtractTypeFromTypeof(typeofArg);
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Extracts the type from a typeof(T) expression.
    /// </summary>
    private TypeSyntax? ExtractTypeFromTypeof(ExpressionSyntax expression)
    {
        if (expression is TypeOfExpressionSyntax typeofExpression)
        {
            return typeofExpression.Type;
        }
        
        return null;
    }

    private ExpressionSyntax CreatePassAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        var passInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Pass")
            ),
            arguments.Count > 0
                ? SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(arguments[0]))
                : SyntaxFactory.ArgumentList()
        );

        return SyntaxFactory.AwaitExpression(passInvocation);
    }

    private ExpressionSyntax CreateFailAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // TUnit: Fail.Test("reason") - not awaited, throws synchronously
        var reasonArg = arguments.Count > 0
            ? arguments[0]
            : SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Test failed")));

        var failInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Fail"),
                SyntaxFactory.IdentifierName("Test")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(reasonArg))
        );

        return failInvocation;
    }

    private ExpressionSyntax CreateSkipAssertion(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        // TUnit: Skip.Test("reason") - not awaited, throws SkipTestException
        var reasonArg = arguments.Count > 0
            ? arguments[0]
            : SyntaxFactory.Argument(
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal("Test skipped")));

        var skipInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Skip"),
                SyntaxFactory.IdentifierName("Test")
            ),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(reasonArg))
        );

        return skipInvocation;
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