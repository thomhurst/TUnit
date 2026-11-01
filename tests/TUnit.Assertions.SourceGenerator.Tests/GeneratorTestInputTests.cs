using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Tests.Shared;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// The harness must bind fixtures the way a real build does (#7190): newer syntax has to parse and bind on the
/// Roslyn this project hosts, and input a real build would reject has to fail the test instead of being snapshotted.
/// </summary>
public class GeneratorTestInputTests
{
    [Test]
    public async Task Newer_Syntax_Is_Accepted()
    {
        var compilation = Compile(
            """
            using System;
            using System.Collections.Generic;

            [AttributeUsage(AttributeTargets.Class)]
            public sealed class ValuesAttribute(int[] values) : Attribute
            {
                public int[] Values { get; } = values;
            }

            [Values([1, 2, 3])]
            public class UsesCollectionExpressionInAttribute
            {
                public static int Sum(params IEnumerable<int> values)
                {
                    var total = 0;
                    foreach (var value in values)
                    {
                        total += value;
                    }

                    return total;
                }
            }
            """);

        await Assert.That(() => GeneratorTestInput.ThrowIfInputCannotBeParsed(compilation)).ThrowsNothing();
    }

    [Test]
    public async Task Syntax_Errors_Are_Rejected()
    {
        var compilation = Compile("public class Broken {");

        await Assert.That(() => GeneratorTestInput.ThrowIfInputCannotBeParsed(compilation))
            .Throws<InvalidOperationException>();
    }

    private static CSharpCompilation Compile(string source)
    {
        return CSharpCompilation.Create(
            nameof(GeneratorTestInputTests),
            [GeneratorTestInput.Parse(source)],
            ReferencesHelper.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
