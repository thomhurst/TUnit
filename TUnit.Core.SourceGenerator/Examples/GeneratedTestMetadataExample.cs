// This is an example of what the source generator would emit with the new TestMetadata approach
// instead of the current complex execution logic

using System;
using System.Collections.Generic;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.DataSources;

namespace TUnit.Generated
{
    /// <summary>
    /// Example of generated test metadata registry for a test class.
    /// The source generator would emit one of these for each assembly with tests.
    /// </summary>
    public static class TestMetadataRegistry
    {
        private static readonly List<TestMetadata> _allTestMetadata = new();
        
        /// <summary>
        /// All test metadata discovered at compile time.
        /// </summary>
        public static IReadOnlyList<TestMetadata> AllTestMetadata => _allTestMetadata;
        
        /// <summary>
        /// Static constructor that registers all test metadata.
        /// </summary>
        static TestMetadataRegistry()
        {
            // Example: Simple test without parameters
            RegisterSimpleTest();
            
            // Example: Test with method data source
            RegisterMethodDataSourceTest();
            
            // Example: Test with inline arguments
            RegisterInlineArgumentsTest();
            
            // Example: Test with properties
            RegisterPropertyTest();
        }
        
        private static void RegisterSimpleTest()
        {
            var metadata = new TestMetadata
            {
                TestIdTemplate = "MyTestClass.SimpleTest",
                TestClassType = typeof(MyTestClass),
                TestMethod = typeof(MyTestClass).GetMethod("SimpleTest", BindingFlags.Public | BindingFlags.Instance)!,
                MethodMetadata = new MethodMetadata
                {
                    Name = "SimpleTest",
                    Type = typeof(MyTestClass),
                    Parameters = Array.Empty<ParameterMetadata>(),
                    GenericTypeCount = 0,
                    Class = new ClassMetadata { /* ... */ },
                    ReturnType = typeof(Task),
                },
                TestFilePath = "/src/MyTestClass.cs",
                TestLineNumber = 42,
                TestClassFactory = (args) => new MyTestClass(),
                ClassDataSources = Array.Empty<IDataSourceProvider>(),
                MethodDataSources = Array.Empty<IDataSourceProvider>(),
                PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
                DisplayNameTemplate = "SimpleTest",
                RepeatCount = 1,
                IsAsync = true,
                IsSkipped = false,
                SkipReason = null,
                Attributes = new Attribute[] { new TestAttribute() },
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            _allTestMetadata.Add(metadata);
        }
        
        private static void RegisterMethodDataSourceTest()
        {
            var metadata = new TestMetadata
            {
                TestIdTemplate = "MyTestClass.DataDrivenTest_{TestIndex}",
                TestClassType = typeof(MyTestClass),
                TestMethod = typeof(MyTestClass).GetMethod("DataDrivenTest", BindingFlags.Public | BindingFlags.Instance)!,
                MethodMetadata = new MethodMetadata
                {
                    Name = "DataDrivenTest",
                    Type = typeof(MyTestClass),
                    Parameters = new[]
                    {
                        new ParameterMetadata { Name = "value", Type = typeof(int) },
                        new ParameterMetadata { Name = "expected", Type = typeof(string) }
                    },
                    GenericTypeCount = 0,
                    Class = new ClassMetadata { /* ... */ },
                    ReturnType = typeof(Task),
                },
                TestFilePath = "/src/MyTestClass.cs",
                TestLineNumber = 55,
                TestClassFactory = (args) => new MyTestClass(),
                ClassDataSources = Array.Empty<IDataSourceProvider>(),
                MethodDataSources = new[]
                {
                    new MethodDataSourceProvider(
                        typeof(MyTestClass).GetMethod("GetTestData", BindingFlags.Public | BindingFlags.Static)!,
                        instance: null,
                        isShared: true)
                },
                PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
                DisplayNameTemplate = "DataDrivenTest({0}, {1})",
                RepeatCount = 1,
                IsAsync = true,
                IsSkipped = false,
                SkipReason = null,
                Attributes = new Attribute[] { new TestAttribute() },
                Timeout = null
            };
            
            _allTestMetadata.Add(metadata);
        }
        
        private static void RegisterInlineArgumentsTest()
        {
            var metadata = new TestMetadata
            {
                TestIdTemplate = "MyTestClass.InlineTest_{TestIndex}",
                TestClassType = typeof(MyTestClass),
                TestMethod = typeof(MyTestClass).GetMethod("InlineTest", BindingFlags.Public | BindingFlags.Instance)!,
                MethodMetadata = new MethodMetadata
                {
                    Name = "InlineTest",
                    Type = typeof(MyTestClass),
                    Parameters = new[]
                    {
                        new ParameterMetadata { Name = "input", Type = typeof(string) }
                    },
                    GenericTypeCount = 0,
                    Class = new ClassMetadata { /* ... */ },
                    ReturnType = typeof(void),
                },
                TestFilePath = "/src/MyTestClass.cs",
                TestLineNumber = 70,
                TestClassFactory = (args) => new MyTestClass(),
                ClassDataSources = Array.Empty<IDataSourceProvider>(),
                MethodDataSources = new[]
                {
                    new InlineDataSourceProvider("test1"),
                    new InlineDataSourceProvider("test2"),
                    new InlineDataSourceProvider("test3")
                },
                PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
                DisplayNameTemplate = "InlineTest({0})",
                RepeatCount = 1,
                IsAsync = false,
                IsSkipped = false,
                SkipReason = null,
                Attributes = new Attribute[] { new TestAttribute() },
                Timeout = null
            };
            
            _allTestMetadata.Add(metadata);
        }
        
        private static void RegisterPropertyTest()
        {
            var testServiceProperty = typeof(MyTestClass).GetProperty("TestService")!;
            
            var metadata = new TestMetadata
            {
                TestIdTemplate = "MyTestClass.PropertyInjectionTest",
                TestClassType = typeof(MyTestClass),
                TestMethod = typeof(MyTestClass).GetMethod("PropertyInjectionTest", BindingFlags.Public | BindingFlags.Instance)!,
                MethodMetadata = new MethodMetadata
                {
                    Name = "PropertyInjectionTest",
                    Type = typeof(MyTestClass),
                    Parameters = Array.Empty<ParameterMetadata>(),
                    GenericTypeCount = 0,
                    Class = new ClassMetadata { /* ... */ },
                    ReturnType = typeof(Task),
                },
                TestFilePath = "/src/MyTestClass.cs",
                TestLineNumber = 85,
                TestClassFactory = (args) => new MyTestClass(),
                ClassDataSources = Array.Empty<IDataSourceProvider>(),
                MethodDataSources = Array.Empty<IDataSourceProvider>(),
                PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>
                {
                    [testServiceProperty] = new InlineDataSourceProvider(new TestService())
                },
                DisplayNameTemplate = "PropertyInjectionTest",
                RepeatCount = 1,
                IsAsync = true,
                IsSkipped = false,
                SkipReason = null,
                Attributes = new Attribute[] { new TestAttribute() },
                Timeout = null
            };
            
            _allTestMetadata.Add(metadata);
        }
    }
    
    // Example test class that would exist in user code
    internal class MyTestClass
    {
        public ITestService TestService { get; set; }
        
        [Test]
        public async Task SimpleTest()
        {
            await Task.Delay(1);
        }
        
        [Test]
        [MethodDataSource(nameof(GetTestData))]
        public async Task DataDrivenTest(int value, string expected)
        {
            await Task.Delay(1);
        }
        
        public static IEnumerable<(int, string)> GetTestData()
        {
            yield return (1, "one");
            yield return (2, "two");
            yield return (3, "three");
        }
        
        [Test]
        [Arguments("test1")]
        [Arguments("test2")]
        [Arguments("test3")]
        public void InlineTest(string input)
        {
        }
        
        [Test]
        public async Task PropertyInjectionTest()
        {
            await TestService.DoSomethingAsync();
        }
    }
    
    internal interface ITestService
    {
        Task DoSomethingAsync();
    }
    
    internal class TestService : ITestService
    {
        public Task DoSomethingAsync() => Task.CompletedTask;
    }
}