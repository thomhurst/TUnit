using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Models;

public record CompilationContext(CSharpCompilation Compilation, AttributeWriter AttributeWriter, WellKnownTypes WellKnownTypes)
{
    private MethodDataSourceRowTypes? _methodDataSourceRowTypes;

    /// <summary>
    /// The generic definitions used to unwrap [MethodDataSource] row types, looked up once per compilation
    /// on first use. A race only repeats the lookups: both instances hold the same symbols.
    /// </summary>
    public MethodDataSourceRowTypes MethodDataSourceRowTypes => _methodDataSourceRowTypes ??= new MethodDataSourceRowTypes(Compilation);
}

/// <summary>
/// The generic type definitions that wrap a [MethodDataSource] row. It holds symbols from one compilation, so,
/// like <see cref="CompilationContext"/>, it must never flow through the incremental pipeline.
/// </summary>
public sealed class MethodDataSourceRowTypes(Compilation compilation)
{
    public INamedTypeSymbol? TaskType { get; } = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
    public INamedTypeSymbol? ValueTaskType { get; } = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
    public INamedTypeSymbol? FuncType { get; } = compilation.GetTypeByMetadataName("System.Func`1");
    public INamedTypeSymbol? AsyncEnumerableType { get; } = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
    public INamedTypeSymbol? TestDataRowType { get; } = compilation.GetTypeByMetadataName("TUnit.Core.TestDataRow`1");
}

/// <summary>
/// Contains all the metadata about a test method discovered by the source generator.
/// </summary>
/// <remarks>
/// This is a working object used while generating code for one test. It holds symbols from a single
/// compilation, so it must never be used as an incremental pipeline value: the pipeline carries the
/// equatable <see cref="TestMethodGenerationResult"/> produced from it instead.
/// </remarks>
public sealed class TestMethodMetadata
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required int StartColumnNumber { get; init; }
    public required int EndLineNumber { get; init; }
    public required int EndColumnNumber { get; init; }
    public required CompilationContext CompilationContext { get; init; }
    public bool IsGenericType { get; init; }
    public bool IsGenericMethod { get; init; }

    /// <summary>
    /// All attributes on the method, stored for later use during data combination generation
    /// </summary>
    public ImmutableArray<AttributeData> MethodAttributes { get; init; } = ImmutableArray<AttributeData>.Empty;

    /// <summary>
    /// The inheritance depth of this test method.
    /// 0 = method is declared directly in the test class
    /// 1 = method is inherited from immediate base class
    /// 2 = method is inherited from base's base class, etc.
    /// </summary>
    public int InheritanceDepth { get; init; } = 0;
}
