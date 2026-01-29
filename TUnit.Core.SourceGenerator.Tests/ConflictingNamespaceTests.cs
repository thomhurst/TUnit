using TUnit.Core.SourceGenerator.Tests.Options;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Tests to verify that generated code compiles correctly when user namespaces
/// contain "TUnit" as a segment (e.g., MySolution.TUnit.Tests, Reqnroll.Retry.TUnit).
/// This validates that all type references use the global:: prefix correctly.
/// See: https://github.com/thomhurst/TUnit/issues/4602
/// </summary>
internal class ConflictingNamespaceTests : TestsBase
{
    [Test]
    public Task BasicTest_WithConflictingNamespace() => RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "BasicTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                // Add a conflicting namespace that contains "TUnit" as a segment
                """
                namespace MySolution.TUnit.Core
                {
                    // This empty namespace would cause compilation errors if generated code
                    // uses "TUnit.Core.SomeType" instead of "global::TUnit.Core.SomeType"
                    public class Placeholder { }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            // If we get here without compilation errors, the test passes
            await Assert.That(generatedFiles).IsNotEmpty();
        });

    [Test]
    public Task DataDrivenTest_WithConflictingNamespace() => RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "DataDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalFiles =
            [
                Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "TestEnum.cs")
            ],
            AdditionalSyntaxes =
            [
                // Add conflicting namespaces that mirror TUnit's structure
                """
                namespace Reqnroll.Retry.TUnit.Core
                {
                    public class Placeholder { }
                }

                namespace Reqnroll.Retry.TUnit.Core.Helpers
                {
                    public class CastHelper { }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });

    [Test]
    public Task HooksTest_WithConflictingNamespace() => HooksGenerator.RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "BeforeTests", "BeforeTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                """
                namespace MyCompany.TUnit.Core
                {
                    public class Placeholder { }
                }

                namespace MyCompany.TUnit.Core.Hooks
                {
                    public class BeforeTestHookMethod { }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });

    [Test]
    public Task MethodDataSource_WithConflictingNamespace() => RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "MethodDataSourceDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                """
                namespace Test.TUnit.Core.Helpers
                {
                    // This would conflict with TUnit.Core.Helpers.CastHelper
                    public static class CastHelper
                    {
                        public static T Cast<T>(object value) => throw new System.NotImplementedException();
                    }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });

    [Test]
    public Task MatrixTest_WithConflictingNamespace() => RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "MatrixTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                """
                namespace Integration.TUnit
                {
                    namespace Core
                    {
                        public class TestMetadata { }
                        public class ParameterMetadata { }
                    }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });

    [Test]
    public Task TupleDataSource_WithConflictingNamespace() => RunTest(
        Path.Combine(Git.RootDirectory.FullName, "TUnit.TestProject", "TupleDataSourceDrivenTests.cs"),
        new RunTestOptions
        {
            AdditionalSyntaxes =
            [
                """
                namespace MyApp.TUnit.Core.Helpers
                {
                    public static class CastHelper
                    {
                        public static T Cast<T>(object value) => default!;
                    }

                    public static class DataSourceHelpers
                    {
                        public static bool IsTuple(object value) => false;
                    }
                }
                """
            ],
            VerifyConfigurator = verify => verify.UniqueForTargetFrameworkAndVersion()
        },
        async generatedFiles =>
        {
            await Assert.That(generatedFiles).IsNotEmpty();
        });
}
