using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Analyzers.Migrators.Base;

public static class MigrationHelpers
{
    public static string ConvertTestAttributeName(string attributeName, string framework)
    {
        return framework switch
        {
            "XUnit" => attributeName switch
            {
                "Fact" => "Test",
                "Theory" => "Test",
                "InlineData" => "Arguments",
                "MemberData" => "MethodDataSource",
                "ClassData" => "MethodDataSource",
                _ => attributeName
            },
            "NUnit" => attributeName switch
            {
                "Test" => "Test",
                "Theory" => "Test",
                "TestCase" => "Arguments",
                "TestCaseSource" => "MethodDataSource",
                "SetUp" => "Before",
                "TearDown" => "After",
                "OneTimeSetUp" => "Before",
                "OneTimeTearDown" => "After",
                "TestFixture" => null!, // Remove
                "Ignore" => "Skip",
                "Description" => null!, // Remove - no direct TUnit equivalent
                "Apartment" => "STAThreadExecutor", // Special handling in attribute rewriter
                _ => attributeName
            },
            "MSTest" => attributeName switch
            {
                "TestMethod" => "Test",
                "DataRow" => "Arguments",
                "DynamicData" => "MethodDataSource",
                "TestInitialize" => "Before",
                "TestCleanup" => "After",
                "ClassInitialize" => "Before",
                "ClassCleanup" => "After",
                "TestClass" => null!, // Remove
                _ => attributeName
            },
            _ => attributeName
        };
    }

    public static AttributeListSyntax? ConvertHookAttribute(AttributeSyntax attribute, string framework)
    {
        var attributeName = GetAttributeName(attribute);
        
        var (newName, hookType) = framework switch
        {
            "NUnit" => attributeName switch
            {
                "SetUp" => ("Before", "Test"),
                "TearDown" => ("After", "Test"),
                "OneTimeSetUp" => ("Before", "Class"),
                "OneTimeTearDown" => ("After", "Class"),
                _ => (null, null)
            },
            "MSTest" => attributeName switch
            {
                "TestInitialize" => ("Before", "Test"),
                "TestCleanup" => ("After", "Test"),
                "ClassInitialize" => ("Before", "Class"),
                "ClassCleanup" => ("After", "Class"),
                _ => (null, null)
            },
            _ => (null, null)
        };

        if (newName == null || hookType == null)
        {
            return null;
        }

        var newAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(newName),
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("HookType"),
                            SyntaxFactory.IdentifierName(hookType)
                        )
                    )
                )
            )
        );

        return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(newAttribute));
    }

    public static string GetAttributeName(AttributeSyntax attribute)
    {
        return attribute.Name switch
        {
            SimpleNameSyntax simpleName => simpleName.Identifier.Text,
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.Text,
            _ => ""
        };
    }

    public static string GetSimpleName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        return lastDot >= 0 ? fullName.Substring(lastDot + 1) : fullName;
    }

    public static bool ShouldRemoveAttribute(string attributeName, string framework)
    {
        return framework switch
        {
            "NUnit" => attributeName is "TestFixture",
            "MSTest" => attributeName is "TestClass",
            _ => false
        };
    }

    public static bool IsTestAttribute(string attributeName, string framework)
    {
        return framework switch
        {
            "XUnit" => attributeName is "Fact" or "Theory",
            "NUnit" => attributeName is "Test" or "TestCase",
            "MSTest" => attributeName is "TestMethod",
            _ => false
        };
    }

    public static bool IsDataAttribute(string attributeName, string framework)
    {
        return framework switch
        {
            "XUnit" => attributeName is "InlineData" or "MemberData" or "ClassData",
            "NUnit" => attributeName is "TestCase" or "TestCaseSource",
            "MSTest" => attributeName is "DataRow" or "DynamicData",
            _ => false
        };
    }

    public static bool IsHookAttribute(string attributeName, string framework)
    {
        return framework switch
        {
            "XUnit" => false, // XUnit uses constructors and IDisposable
            "NUnit" => attributeName is "SetUp" or "TearDown" or "OneTimeSetUp" or "OneTimeTearDown",
            "MSTest" => attributeName is "TestInitialize" or "TestCleanup" or "ClassInitialize" or "ClassCleanup",
            _ => false
        };
    }

    public static CompilationUnitSyntax RemoveFrameworkUsings(CompilationUnitSyntax compilationUnit, string framework)
    {
        var namespacesToRemove = framework switch
        {
            "XUnit" => new[] { "Xunit", "Xunit.Abstractions" },
            "NUnit" => new[] { "NUnit.Framework", "NUnit.Framework.Legacy" },
            "MSTest" => new[] { "Microsoft.VisualStudio.TestTools.UnitTesting" },
            _ => Array.Empty<string>()
        };

        var usingsToKeep = compilationUnit.Usings
            .Where(u =>
            {
                var nameString = u.Name?.ToString() ?? "";
                return !namespacesToRemove.Any(ns =>
                    nameString == ns || nameString.StartsWith(ns + "."));
            })
            .ToArray();

        return compilationUnit.WithUsings(SyntaxFactory.List(usingsToKeep));
    }
    
    /// <summary>
    /// Adds System.Threading.Tasks using directive if the code contains async methods or await expressions.
    /// This is called unconditionally for all migrations since async methods need the Tasks namespace.
    /// </summary>
    public static CompilationUnitSyntax AddSystemThreadingTasksUsing(CompilationUnitSyntax compilationUnit)
    {
        var existingUsings = compilationUnit.Usings.ToList();

        // Add System.Threading.Tasks only if the code has async methods or await expressions
        bool hasAsyncCode = compilationUnit.DescendantNodes()
            .Any(n => n is AwaitExpressionSyntax ||
                     (n is MethodDeclarationSyntax m && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword))));

        if (hasAsyncCode && !existingUsings.Any(u => u.Name?.ToString() == "System.Threading.Tasks"))
        {
            var tasksUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks"));
            existingUsings.Add(tasksUsing);
            return compilationUnit.WithUsings(SyntaxFactory.List(existingUsings));
        }

        return compilationUnit;
    }

    public static CompilationUnitSyntax AddTUnitUsings(CompilationUnitSyntax compilationUnit)
    {
        var tunitUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Core"));
        // Add namespace using so Assert type name is available for Assert.That(...) syntax
        var assertionsNamespaceUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions"));
        var assertionsStaticUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions.Assert"))
            .WithStaticKeyword(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        var extensionsUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions.Extensions"));

        // First add System.Threading.Tasks if needed
        compilationUnit = AddSystemThreadingTasksUsing(compilationUnit);

        var existingUsings = compilationUnit.Usings.ToList();

        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Core"))
        {
            existingUsings.Add(tunitUsing);
        }

        // Add namespace using so Assert type name is resolvable
        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Assertions" && !u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)))
        {
            existingUsings.Add(assertionsNamespaceUsing);
        }

        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Assertions.Assert" && u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)))
        {
            existingUsings.Add(assertionsStaticUsing);
        }

        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Assertions.Extensions"))
        {
            existingUsings.Add(extensionsUsing);
        }

        return compilationUnit.WithUsings(SyntaxFactory.List(existingUsings));
    }
}