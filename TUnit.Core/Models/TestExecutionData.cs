using System.Collections.Concurrent;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Models;

/// <summary>
/// Holds all execution-related data for a test, including factories, invokers, and resolved data.
/// This replaces the multiple dictionaries in SourceGeneratedTestRegistry with a single cohesive data structure.
/// </summary>
public class TestExecutionData
{
    /// <summary>
    /// Factory for creating test class instances (strongly typed)
    /// </summary>
    public Delegate? ClassFactory { get; set; }

    /// <summary>
    /// Invoker for calling test methods (strongly typed)
    /// </summary>
    public Delegate? MethodInvoker { get; set; }

    /// <summary>
    /// Method data resolver for MethodDataSource support
    /// </summary>
    public Func<IReadOnlyList<object?[]>>? MethodDataResolver { get; set; }

    /// <summary>
    /// Async method data resolver for MethodDataSource support
    /// </summary>
    public Func<Task<IReadOnlyList<object?[]>>>? AsyncMethodDataResolver { get; set; }

    /// <summary>
    /// Async data source resolver for AsyncDataSourceGenerator support
    /// </summary>
    public Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>>? AsyncDataSourceResolver { get; set; }

    /// <summary>
    /// Async data executor for AsyncDataSourceGenerator support
    /// </summary>
    public Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<object?[]?>>>? AsyncDataExecutor { get; set; }

    /// <summary>
    /// Indicates if this test has AOT-optimized strongly typed delegates
    /// </summary>
    public bool HasStronglyTypedDelegates => ClassFactory != null && MethodInvoker != null;

    /// <summary>
    /// Indicates if this test has method data resolvers
    /// </summary>
    public bool HasMethodDataResolver => MethodDataResolver != null || AsyncMethodDataResolver != null;

    /// <summary>
    /// Indicates if this test has async data source support
    /// </summary>
    public bool HasAsyncDataSource => AsyncDataSourceResolver != null || AsyncDataExecutor != null;
}