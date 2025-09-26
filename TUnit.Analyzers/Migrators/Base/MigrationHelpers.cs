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
                "TestCase" => "Arguments",
                "TestCaseSource" => "MethodDataSource",
                "SetUp" => "Before",
                "TearDown" => "After",
                "OneTimeSetUp" => "Before",
                "OneTimeTearDown" => "After",
                "TestFixture" => null!, // Remove
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
            "NUnit" => new[] { "NUnit.Framework" },
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
    
    public static CompilationUnitSyntax AddTUnitUsings(CompilationUnitSyntax compilationUnit)
    {
        var tunitUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Core"));
        var assertionsUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions"))
            .WithStaticKeyword(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
        var extensionsUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("TUnit.Assertions.Extensions"));
        
        var existingUsings = compilationUnit.Usings.ToList();
        
        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Core"))
        {
            existingUsings.Add(tunitUsing);
        }
        
        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Assertions" && u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)))
        {
            existingUsings.Add(assertionsUsing);
        }
        
        if (!existingUsings.Any(u => u.Name?.ToString() == "TUnit.Assertions.Extensions"))
        {
            existingUsings.Add(extensionsUsing);
        }

        return compilationUnit.WithUsings(SyntaxFactory.List(existingUsings));
    }
}