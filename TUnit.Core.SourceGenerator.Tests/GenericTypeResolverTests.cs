using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Tests.Options;
using Verifier = TUnit.Core.SourceGenerator.Tests.Verifier;

namespace TUnit.Core.SourceGenerator.Tests;

/// <summary>
/// Tests for the GenericTypeResolver functionality in the unified source generator
/// </summary>
internal class GenericTypeResolverTests : TestsBase<Generators.UnifiedTestMetadataGeneratorV2>
{
    [Test]
    public Task Test_GenericTestClass_WithExplicitInstantiation() => RunTest("""
        using TUnit.Core;

        namespace TUnit.TestProject;

        [GenerateGenericTest(typeof(int))]
        [GenerateGenericTest(typeof(string))]
        public class GenericTestClass<T>
        {
            [Test]
            public void TestMethod()
            {
                // Test implementation
            }
        }
        """);

    [Test]
    public Task Test_GenericTestMethod_WithExplicitInstantiation() => RunTest("""
        using TUnit.Core;

        namespace TUnit.TestProject;

        public class TestClass
        {
            [GenerateGenericTest(typeof(int))]
            [GenerateGenericTest(typeof(string))]
            [Test]
            public void GenericTestMethod<T>()
            {
                // Test implementation
            }
        }
        """);

    [Test]
    public Task Test_EmptyGenericRegistry_WhenNoGenericsFound() => RunTest("""
        using TUnit.Core;

        namespace TUnit.TestProject;

        public class SimpleTestClass
        {
            [Test]
            public void NonGenericTest()
            {
                // Test implementation
            }
        }
        """);

    [Test]
    public Task Test_MultipleGenericParameters() => RunTest("""
        using TUnit.Core;

        namespace TUnit.TestProject;

        [GenerateGenericTest(typeof(int), typeof(string))]
        [GenerateGenericTest(typeof(bool), typeof(double))]
        public class MultiGenericTestClass<T1, T2>
        {
            [Test]
            public void TestMethod()
            {
                // Test implementation with T1 and T2
            }
        }
        """);

    [Test]
    public Task Test_GenericConstraints_WithInstantiation() => RunTest("""
        using TUnit.Core;

        namespace TUnit.TestProject;

        [GenerateGenericTest(typeof(string))]
        [GenerateGenericTest(typeof(object))]
        public class ConstrainedGenericTestClass<T>
            where T : class, new()
        {
            [Test]
            public void TestMethod()
            {
                var instance = new T();
            }
        }
        """);

    [Test]
    public Task Test_NestedGenericTypes() => RunTest("""
        using TUnit.Core;
        using System.Collections.Generic;

        namespace TUnit.TestProject;

        [GenerateGenericTest(typeof(List<int>))]
        [GenerateGenericTest(typeof(Dictionary<string, int>))]
        public class NestedGenericTestClass<T>
        {
            [Test]
            public void TestMethod()
            {
                // Test with nested generic type T
            }
        }
        """);

    private static async Task RunTest(string source)
    {
        var (compilation, diagnostics) = await Verifier.GetGeneratedOutput<Generators.UnifiedTestMetadataGeneratorV2>(
            source,
            new RunTestOptions()
        );

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException($"Compilation errors: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
        }

        // Verify source generation output
        var testName = System.Diagnostics.StackTrace.GetFrame(1)?.GetMethod()?.Name ?? "UnknownTest";
        await Verifier.Verify(compilation, $"{nameof(GenericTypeResolverTests)}.{testName}");
    }
}