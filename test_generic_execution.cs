using System;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Engine;
using TUnit.Engine.Building;
using TUnit.Engine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TestGenericExecution
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Testing generic test execution...");
            
            // Create test context
            var services = new ServiceCollection();
            services.AddSingleton<ITestBuilder, TestBuilder>();
            services.AddSingleton<IContextProvider, ContextProvider>();
            var serviceProvider = services.BuildServiceProvider();
            
            var testBuilder = serviceProvider.GetRequiredService<ITestBuilder>();
            
            // Create a simple test metadata
            var metadata = new TestMetadata<object>
            {
                TestName = "TestWithValue",
                TestClassType = typeof(object),
                TestMethodName = "TestWithValue",
                Categories = Array.Empty<string>(),
                TimeoutMs = null,
                RetryCount = 0,
                CanRunInParallel = true,
                Dependencies = Array.Empty<TestDependency>(),
                AttributeFactory = () => new Attribute[] { new TestAttribute() },
                DataSources = Array.Empty<TestDataSource>(),
                ClassDataSources = Array.Empty<TestDataSource>(),
                PropertyDataSources = Array.Empty<PropertyDataSource>(),
                ParameterTypes = new Type[] { typeof(object) },
                TestMethodParameterTypes = new string[] { "object" },
                MethodMetadata = new MethodMetadata
                {
                    Type = typeof(object),
                    TypeReference = TypeReference.CreateConcrete("System.Object, System.Private.CoreLib"),
                    Name = "TestWithValue",
                    GenericTypeCount = 0,
                    ReturnType = typeof(Task),
                    ReturnTypeReference = TypeReference.CreateConcrete("System.Threading.Tasks.Task, System.Private.CoreLib"),
                    Parameters = new ParameterMetadata[] { },
                    Class = new ClassMetadata
                    {
                        Type = typeof(object),
                        TypeReference = TypeReference.CreateConcrete("System.Object, System.Private.CoreLib"),
                        Name = "Object",
                        Namespace = "System",
                        Parameters = new ParameterMetadata[] { },
                        Properties = new PropertyMetadata[] { },
                        Assembly = new AssemblyMetadata { Name = "System.Private.CoreLib" },
                        Parent = null
                    }
                },
                Hooks = new TestHooks
                {
                    BeforeClass = Array.Empty<HookMetadata>(),
                    AfterClass = Array.Empty<HookMetadata>(),
                    BeforeTest = Array.Empty<HookMetadata>(),
                    AfterTest = Array.Empty<HookMetadata>()
                },
                CreateInstance = async (context) => { await Task.CompletedTask; return new object(); },
                InvokeTest = async (instance, args, context, ct) => { 
                    Console.WriteLine($"Test invoked with args: {string.Join(", ", args)}");
                    await Task.CompletedTask; 
                }
            };
            
            // Set up data combination generator
            metadata.SetDataCombinationGenerator(() => GenerateTestData());
            
            // Build tests
            var tests = await testBuilder.BuildTestsFromMetadataAsync(metadata);
            
            Console.WriteLine($"Built {tests.Count()} tests");
            
            foreach (var test in tests)
            {
                Console.WriteLine($"Test: {test.DisplayName}");
                Console.WriteLine($"  Arguments: {string.Join(", ", test.Arguments)}");
            }
        }
        
        static async IAsyncEnumerable<TestDataCombination> GenerateTestData()
        {
            yield return new TestDataCombination
            {
                MethodDataFactories = new[] { new Func<Task<object?>>(async () => { await Task.CompletedTask; return 42; }) },
                DisplayName = "TestWithValue(42)",
                ClassDataSourceIndex = 0,
                MethodDataSourceIndex = 0,
                ClassLoopIndex = 0,
                MethodLoopIndex = 0,
                PropertyValueFactories = new Dictionary<string, Func<Task<object?>>>(),
                ResolvedGenericTypes = new Dictionary<string, Type> { ["T"] = typeof(int) }
            };
        }
    }
}