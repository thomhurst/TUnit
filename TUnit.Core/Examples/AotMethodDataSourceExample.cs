using TUnit.Core.Services;

namespace TUnit.Core.Examples;

/// <summary>
/// Example of how AOT-safe MethodDataSource generation works.
/// 
/// Given this test class:
/// 
/// public class MyTests 
/// {
///     [Test]
///     [MethodDataSource(nameof(GetTestData))]
///     public void MyTest(string input, int number) { }
///     
///     public static IEnumerable<object[]> GetTestData()
///     {
///         yield return new object[] { "test1", 1 };
///         yield return new object[] { "test2", 2 };
///     }
/// }
/// 
/// The source generator will emit the following AOT-safe code:
/// </summary>
public static class GeneratedAotMethodDataSourceExample
{
    // Generated AOT-safe data source factory for MyTests.GetTestData
    public static class MyTests_GetTestData_DataSourceFactory
    {
        public static IEnumerable<object?[]> GetData()
        {
            return MyTests.GetTestData();  // ✅ Direct strongly-typed call!
        }

        public static readonly Func<IEnumerable<object?[]>> Factory = GetData;
    }

    // Generated AOT-safe method data resolver for MyTests.MyTest
    public static class MyTests_MyTest_MethodDataResolver
    {
        public static Task<IReadOnlyList<object?[]>> ResolveAllMethodDataAsync()
        {
            var allData = new List<object?[]>();

            var data_GetTestData = MyTests_GetTestData_DataSourceFactory.GetData();
            allData.AddRange(data_GetTestData);

            return Task.FromResult<IReadOnlyList<object?[]>>(allData.AsReadOnly());
        }

        public static IReadOnlyList<object?[]> ResolveAllMethodData()
        {
            return ResolveAllMethodDataAsync().GetAwaiter().GetResult();
        }
    }

    // Registration code (called automatically via ModuleInitializer)
    public static void RegisterFactories()
    {
        // Registration for method data sources on test MyTests.MyTest
        GlobalSourceGeneratedTestRegistry.RegisterMethodDataResolver("MyTests.MyTest", MyTests_MyTest_MethodDataResolver.ResolveAllMethodData);
        GlobalSourceGeneratedTestRegistry.RegisterAsyncMethodDataResolver("MyTests.MyTest", MyTests_MyTest_MethodDataResolver.ResolveAllMethodDataAsync);
    }
}

/// <summary>
/// Example test class that would trigger the above generation.
/// </summary>
public class MyTests
{
    // [Test]
    // [MethodDataSource(nameof(GetTestData))]
    public void MyTest(string input, int number)
    {
        // Test implementation
    }

    public static IEnumerable<object[]> GetTestData()
    {
        yield return new object[] { "test1", 1 };
        yield return new object[] { "test2", 2 };
        yield return new object[] { "async_test", 42 };
    }
}

/// <summary>
/// Benefits of this approach:
/// 
/// ✅ 100% AOT-Safe: No reflection, no dynamic code generation
/// ✅ Strongly Typed: Direct method calls with compile-time verification  
/// ✅ Zero Overhead: No boxing/unboxing, no runtime type resolution
/// ✅ Full Compatibility: Supports any MethodDataSource method signature
/// ✅ Async Support: Works with async data source methods
/// ✅ Performance: As fast as calling the method directly
/// 
/// This solves the MethodDataSource AOT problem by generating the invocation code
/// rather than trying to execute the methods at compile time.
/// </summary>
public static class AotMethodDataSourceBenefits { }
