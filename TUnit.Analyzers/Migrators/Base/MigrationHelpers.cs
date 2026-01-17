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
                "Theory" => "Test", // NUnit [Theory] is same as [Test]
                "TestCase" => "Arguments",
                "TestCaseSource" => "MethodDataSource",
                "SetUp" => "Before",
                "TearDown" => "After",
                "OneTimeSetUp" => "Before",
                "OneTimeTearDown" => "After",
                "TestFixture" => null!, // Remove
                "Ignore" => "Skip", // NUnit [Ignore] -> TUnit [Skip]
                "Description" => null!, // Remove - no direct equivalent, use [Property] if needed
                "Platform" => null!, // Remove - no direct equivalent, use runtime checks
                "Apartment" => "STAThreadExecutor", // Special handling in attribute rewriter
                // Parallelization attributes - special handling in code fixer
                "NonParallelizable" => "NotInParallel",
                "Parallelizable" => null!, // Remove by default, special handling for ParallelScope.None
                // Repeat is the same in TUnit
                "Repeat" => "Repeat",
                // Parameter-level data attributes
                "Values" => "Matrix",
                "Range" => "MatrixRange", // Note: requires generic type argument in code fixer
                "ValueSource" => "MatrixSourceMethod",
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
                // Metadata attributes - convert to [Property] with appropriate arguments
                "Priority" => "Property",
                "TestCategory" => "Property",
                "Owner" => "Property",
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
            // Note: Parallelizable is NOT listed here because it needs special handling in the code fixer
            // for ParallelScope.None -> [NotInParallel]. ConvertAttribute handles all cases.
            // Note: Platform is NOT listed here because it gets converted to RunOn/ExcludeOn.
            // Note: Description is NOT listed here because it gets converted to Property("Description", ...).
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
            "XUnit" => new[] { "Xunit", "Xunit.Abstractions", "Xunit.Sdk" },
            "NUnit" => new[] { "NUnit.Framework", "NUnit.Framework.Legacy" },
            "MSTest" => new[] { "Microsoft.VisualStudio.TestTools.UnitTesting" },
            _ => Array.Empty<string>()
        };

        // Preserve leading trivia from the first using directive (may contain license header)
        var leadingTrivia = compilationUnit.Usings.FirstOrDefault()?.GetLeadingTrivia() ?? default;

        var usingsToKeep = compilationUnit.Usings
            .Where(u =>
            {
                var nameString = u.Name?.ToString() ?? "";
                return !namespacesToRemove.Any(ns =>
                    nameString == ns || nameString.StartsWith(ns + "."));
            })
            .ToList();

        // Apply the preserved leading trivia to the new first using directive
        if (usingsToKeep.Count > 0 && leadingTrivia.Count > 0)
        {
            usingsToKeep[0] = usingsToKeep[0].WithLeadingTrivia(leadingTrivia);
        }

        var result = compilationUnit.WithUsings(SyntaxFactory.List(usingsToKeep));

        // If no usings remain but there was leading trivia, preserve it on the first member or EOF token
        if (usingsToKeep.Count == 0 && leadingTrivia.Count > 0)
        {
            if (result.Members.Count > 0)
            {
                var firstMember = result.Members[0];
                result = result.ReplaceNode(firstMember, firstMember.WithLeadingTrivia(leadingTrivia.AddRange(firstMember.GetLeadingTrivia())));
            }
            else
            {
                result = result.WithEndOfFileToken(result.EndOfFileToken.WithLeadingTrivia(leadingTrivia));
            }
        }

        return result;
    }
    
    /// <summary>
    /// Adds System.Threading.Tasks using directive if the code contains async methods or await expressions.
    /// This is called unconditionally for all migrations since async methods need the Tasks namespace.
    /// </summary>
    public static CompilationUnitSyntax AddSystemThreadingTasksUsing(CompilationUnitSyntax compilationUnit)
    {
        // Add System.Threading.Tasks only if the code has async methods or await expressions
        bool hasAsyncCode = compilationUnit.DescendantNodes()
            .Any(n => n is AwaitExpressionSyntax ||
                     (n is MethodDeclarationSyntax m && m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword))));

        if (!hasAsyncCode || compilationUnit.Usings.Any(u => u.Name?.ToString() == "System.Threading.Tasks"))
        {
            return compilationUnit;
        }

        // Preserve leading trivia from the first using directive (may contain license header)
        var leadingTrivia = compilationUnit.Usings.FirstOrDefault()?.GetLeadingTrivia() ?? default;

        var existingUsings = compilationUnit.Usings.ToList();

        // Strip leading trivia from first using since we'll add it back at the end
        if (existingUsings.Count > 0 && leadingTrivia.Count > 0)
        {
            existingUsings[0] = existingUsings[0].WithLeadingTrivia(SyntaxTriviaList.Empty);
        }

        var tasksUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Threading.Tasks"));
        existingUsings.Add(tasksUsing);

        // Restore leading trivia on the first using
        if (existingUsings.Count > 0 && leadingTrivia.Count > 0)
        {
            existingUsings[0] = existingUsings[0].WithLeadingTrivia(leadingTrivia);
        }

        return compilationUnit.WithUsings(SyntaxFactory.List(existingUsings));
    }

    public static CompilationUnitSyntax AddTUnitUsings(CompilationUnitSyntax compilationUnit)
    {
        // Note: TUnit package automatically sets up global usings via .targets files:
        // - TUnit.Core and static TUnit.Core.HookType (from TUnit.Core.targets)
        // - TUnit.Assertions and TUnit.Assertions.Extensions (from TUnit.Assertions.targets)
        //
        // We do NOT add explicit TUnit usings since they are already available via global usings.
        // This keeps the migrated code clean. We only add System.* usings that are not part of
        // the TUnit package's global usings.

        // Add System.Threading.Tasks if needed for async methods
        compilationUnit = AddSystemThreadingTasksUsing(compilationUnit);

        // Add System.IO if File. or Directory. is used (from FileAssert/DirectoryAssert conversion)
        compilationUnit = AddSystemIOUsing(compilationUnit);

        return compilationUnit;
    }

    /// <summary>
    /// Adds System.IO using directive if the code contains File, Directory, FileInfo, or DirectoryInfo references.
    /// This is needed when FileAssert or DirectoryAssert is converted to use System.IO classes.
    /// </summary>
    public static CompilationUnitSyntax AddSystemIOUsing(CompilationUnitSyntax compilationUnit)
    {
        // Check if code contains File. or Directory. member access
        bool hasFileOrDirectoryMemberAccess = compilationUnit.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Any(m => m.Expression is IdentifierNameSyntax { Identifier.Text: "File" or "Directory" });

        // Check if code contains new FileInfo(...) or new DirectoryInfo(...) object creation
        bool hasFileInfoOrDirectoryInfoCreation = compilationUnit.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Any(o => o.Type is IdentifierNameSyntax { Identifier.Text: "FileInfo" or "DirectoryInfo" });

        if ((!hasFileOrDirectoryMemberAccess && !hasFileInfoOrDirectoryInfoCreation) ||
            compilationUnit.Usings.Any(u => u.Name?.ToString() == "System.IO"))
        {
            return compilationUnit;
        }

        // Preserve leading trivia from the first using directive (may contain license header)
        var leadingTrivia = compilationUnit.Usings.FirstOrDefault()?.GetLeadingTrivia() ?? default;

        var existingUsings = compilationUnit.Usings.ToList();

        // Strip leading trivia from first using since we'll add it back at the end
        if (existingUsings.Count > 0 && leadingTrivia.Count > 0)
        {
            existingUsings[0] = existingUsings[0].WithLeadingTrivia(SyntaxTriviaList.Empty);
        }

        var ioUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.IO"));

        // Apply the leading trivia to the new System.IO using since it will be first
        if (leadingTrivia.Count > 0)
        {
            ioUsing = ioUsing.WithLeadingTrivia(leadingTrivia);
        }

        existingUsings.Insert(0, ioUsing); // Insert at beginning to keep System.* namespaces together
        return compilationUnit.WithUsings(SyntaxFactory.List(existingUsings));
    }
}