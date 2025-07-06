using System.Diagnostics;
using System.Runtime.CompilerServices;
using TUnit.Core.Services;

namespace TUnit.Core.Examples;

/// <summary>
/// How AOT-safe AsyncDataSourceGenerator generation works.
///
/// Given this test class with custom AsyncDataSourceGenerators:
///
/// public class MyAsyncApiDataSource : AsyncDataSourceGeneratorAttribute<string>
/// {
///     protected override async IAsyncEnumerable<Func<Task<string>>> GenerateDataSourcesAsync(DataGeneratorMetadata metadata)
///     {
///         yield return async () => await CallExternalApi("endpoint1");
///         yield return async () => await CallExternalApi("endpoint2");
///     }
/// }
///
/// public class MyTests
/// {
///     [Test]
///     [MyAsyncApiDataSource]
///     public async Task MyTest(string apiResult) { }
/// }
///
/// The source generator will emit the following AOT-safe code:
/// </summary>
public static class GeneratedAotAsyncDataSourceExample
{
    // Generated AOT-safe async data source factory for MyAsyncApiDataSource
    public static class MyAsyncApiDataSource_AotFactory
    {
        public static async IAsyncEnumerable<Func<Task<object?[]?>>> GenerateDataAsync(
            DataGeneratorMetadata metadata,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var generator = new MyAsyncApiDataSource();  // ✅ Direct strongly-typed instantiation!

            await foreach (var dataSourceFunc in generator.GenerateAsync(metadata).WithCancellation(cancellationToken))
            {
                yield return async () =>
            {
                var result = await dataSourceFunc();
                return [result];
            };  // ✅ Direct async enumeration!
            }
        }

        public static async Task<IReadOnlyList<Func<Task<object?[]?>>>> GenerateDataListAsync(
            DataGeneratorMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            var results = new List<Func<Task<object?[]?>>>();
            await foreach (var dataSourceFunc in GenerateDataAsync(metadata, cancellationToken))
            {
                results.Add(dataSourceFunc);
            }
            return results.AsReadOnly();
        }

        public static readonly Func<DataGeneratorMetadata, CancellationToken, IAsyncEnumerable<Func<Task<object?[]?>>>> AsyncFactory = GenerateDataAsync;
        public static readonly Func<DataGeneratorMetadata, CancellationToken, Task<IReadOnlyList<Func<Task<object?[]?>>>>> ListFactory = GenerateDataListAsync;
    }

    // Generated AOT-safe async data resolver for MyAsyncTests.MyAsyncTest
    public static class MyAsyncTests_MyAsyncTest_AsyncDataResolver
    {
        public static async Task<IReadOnlyList<Func<Task<object?[]?>>>> ResolveAllAsyncDataAsync(
            DataGeneratorMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            var allDataSources = new List<Func<Task<object?[]?>>>();

            // Generate data from MyAsyncApiDataSource
            var data_MyAsyncApiDataSource = await MyAsyncApiDataSource_AotFactory.GenerateDataListAsync(metadata, cancellationToken);
            allDataSources.AddRange(data_MyAsyncApiDataSource);

            return allDataSources.AsReadOnly();
        }

        public static async Task<IReadOnlyList<object?[]?>> ResolveAndExecuteAllAsyncDataAsync(
            DataGeneratorMetadata metadata,
            CancellationToken cancellationToken = default)
        {
            var dataSources = await ResolveAllAsyncDataAsync(metadata, cancellationToken);
            var results = new List<object?[]?>();

            foreach (var dataSource in dataSources)
            {
                try
                {
                    var result = await dataSource();  // ✅ Execute async data source
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    // Log error and continue with other data sources
                    Debug.WriteLine($"Error executing async data source: {ex.Message}");
                }
            }

            return results.AsReadOnly();
        }
    }

    // Registration code (called automatically via ModuleInitializer)
    public static void RegisterFactories()
    {
        // Registration for async data sources on test MyAsyncTests.MyAsyncTest
        TestExecutionRegistry.Instance.RegisterAsyncDataSourceResolver("MyAsyncTests.MyAsyncTest", MyAsyncTests_MyAsyncTest_AsyncDataResolver.ResolveAllAsyncDataAsync);
        TestExecutionRegistry.Instance.RegisterAsyncDataExecutor("MyAsyncTests.MyAsyncTest", MyAsyncTests_MyAsyncTest_AsyncDataResolver.ResolveAndExecuteAllAsyncDataAsync);
    }
}

/// <summary>
/// AsyncDataSourceGenerator that would trigger the above generation.
/// </summary>
public class MyAsyncApiDataSource : AsyncDataSourceGeneratorAttribute<string>
{
    protected override async IAsyncEnumerable<Func<Task<string>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        // Simulate API calls that generate test data
        await Task.Delay(1); // Make method truly async to avoid warning

        yield return async () =>
        {
            await Task.Delay(10); // Simulate network call
            return "API Result 1";
        };

        yield return async () =>
        {
            await Task.Delay(10); // Simulate network call
            return "API Result 2";
        };
    }
}

/// <summary>
/// Test class that would trigger the above generation.
/// </summary>
public class MyAsyncTests
{
    // [Test]
    // [MyAsyncApiDataSource]
    public async Task MyAsyncTest(string apiResult)
    {
        // Test implementation that uses async-generated data
        await Task.Delay(1);
    }
}

/// <summary>
/// Benefits of this AOT-safe AsyncDataSourceGenerator approach:
///
/// ✅ 100% AOT-Safe: No reflection, no dynamic instantiation
/// ✅ Strongly Typed: Direct instantiation and method calls with compile-time verification
/// ✅ Full Async Support: Preserves async/await patterns throughout
/// ✅ Zero Overhead: As fast as instantiating and calling the generator directly
/// ✅ Complex Logic Support: No limitations on async generator complexity
/// ✅ Cancellation Support: Full CancellationToken propagation
/// ✅ Error Handling: Graceful handling of async data source failures
/// ✅ Lazy Evaluation: Data sources executed only when needed
///
/// This solves the AsyncDataSourceGenerator AOT problem by generating the instantiation
/// and invocation code rather than trying to execute generators at compile time.
/// </summary>
public static class AotAsyncDataSourceBenefits { }
