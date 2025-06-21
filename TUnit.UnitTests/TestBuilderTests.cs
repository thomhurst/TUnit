using System.Reflection;
using TUnit.Core.DataSources;

namespace TUnit.UnitTests;

public class TestBuilderTests
{
    private readonly TestBuilder _testBuilder = new();
    
    [Test]
    public async Task BuildTestsAsync_WithSimpleTest_CreatesSingleTestDefinition()
    {
        // Arrange
        var testMethod = typeof(TestClass).GetMethod(nameof(TestClass.SimpleTest))!;
        var metadata = new TestMetadata
        {
            TestIdTemplate = "TestClass.SimpleTest",
            TestClassType = typeof(TestClass),
            TestClassTypeReference = TypeReference.CreateConcrete($"{typeof(TestClass).FullName}, {typeof(TestClass).Assembly.GetName().Name}"),
            MethodMetadata = CreateMethodMetadata(testMethod),
            TestFilePath = "test.cs",
            TestLineNumber = 10,
            TestClassFactory = (_) => new TestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = Array.Empty<IDataSourceProvider>(),
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "SimpleTest",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>()
        };
        
        // Act
        var testDefinitions = await _testBuilder.BuildTestsAsync(metadata);
        
        // Assert
        await Assert.That(testDefinitions).HasCount().EqualTo(1);
        var testDef = testDefinitions.First();
        await Assert.That(testDef.TestId).IsEqualTo("TestClass.SimpleTest");
        await Assert.That(testDef.TestFilePath).IsEqualTo("test.cs");
        await Assert.That(testDef.TestLineNumber).IsEqualTo(10);
    }
    
    [Test]
    public async Task BuildTestsAsync_WithInlineArguments_CreatesMultipleTestDefinitions()
    {
        // Arrange
        var testMethod = typeof(TestClass).GetMethod(nameof(TestClass.ParameterizedTest))!;
        var metadata = new TestMetadata
        {
            TestIdTemplate = "TestClass.ParameterizedTest_{TestIndex}",
            TestClassType = typeof(TestClass),
            TestClassTypeReference = TypeReference.CreateConcrete($"{typeof(TestClass).FullName}, {typeof(TestClass).Assembly.GetName().Name}"),
            MethodMetadata = CreateMethodMetadata(testMethod),
            TestFilePath = "test.cs",
            TestLineNumber = 20,
            TestClassFactory = (_) => new TestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = new IDataSourceProvider[]
            {
                new InlineDataSourceProvider(1, "one"),
                new InlineDataSourceProvider(2, "two"),
                new InlineDataSourceProvider(3, "three")
            },
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "ParameterizedTest({0}, {1})",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>()
        };
        
        // Act
        var testDefinitions = await _testBuilder.BuildTestsAsync(metadata);
        
        // Assert
        await Assert.That(testDefinitions).HasCount().EqualTo(3);
        
        var testDefList = testDefinitions.ToList();
        await Assert.That(testDefList[0].TestId).IsEqualTo("TestClass.ParameterizedTest_0");
        await Assert.That(testDefList[1].TestId).IsEqualTo("TestClass.ParameterizedTest_1");
        await Assert.That(testDefList[2].TestId).IsEqualTo("TestClass.ParameterizedTest_2");
    }
    
    [Test]
    public async Task BuildTestsAsync_WithRepeatCount_CreatesRepeatedTests()
    {
        // Arrange
        var testMethod = typeof(TestClass).GetMethod(nameof(TestClass.SimpleTest))!;
        var metadata = new TestMetadata
        {
            TestIdTemplate = "TestClass.SimpleTest_{RepeatIndex}",
            TestClassType = typeof(TestClass),
            TestClassTypeReference = TypeReference.CreateConcrete($"{typeof(TestClass).FullName}, {typeof(TestClass).Assembly.GetName().Name}"),
            MethodMetadata = CreateMethodMetadata(testMethod),
            TestFilePath = "test.cs",
            TestLineNumber = 30,
            TestClassFactory = (_) => new TestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = Array.Empty<IDataSourceProvider>(),
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "SimpleTest",
            RepeatCount = 3,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>()
        };
        
        // Act
        var testDefinitions = await _testBuilder.BuildTestsAsync(metadata);
        
        // Assert
        await Assert.That(testDefinitions).HasCount().EqualTo(3);
        
        var testDefList = testDefinitions.ToList();
        await Assert.That(testDefList[0].TestId).IsEqualTo("TestClass.SimpleTest_0");
        await Assert.That(testDefList[1].TestId).IsEqualTo("TestClass.SimpleTest_1");
        await Assert.That(testDefList[2].TestId).IsEqualTo("TestClass.SimpleTest_2");
    }
    
    [Test]
    public async Task BuildTestsAsync_WithTupleData_UnwrapsTuplesCorrectly()
    {
        // Arrange
        var testMethod = typeof(TestClass).GetMethod(nameof(TestClass.TupleTest))!;
        var metadata = new TestMetadata
        {
            TestIdTemplate = "TestClass.TupleTest",
            TestClassType = typeof(TestClass),
            TestClassTypeReference = TypeReference.CreateConcrete($"{typeof(TestClass).FullName}, {typeof(TestClass).Assembly.GetName().Name}"),
            MethodMetadata = CreateMethodMetadata(testMethod),
            TestFilePath = "test.cs",
            TestLineNumber = 40,
            TestClassFactory = (_) => new TestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = new IDataSourceProvider[]
            {
                new InlineDataSourceProvider((1, "one", true))
            },
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "TupleTest",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>()
        };
        
        // Act
        var testDefinitions = await _testBuilder.BuildTestsAsync(metadata);
        
        // Assert
        await Assert.That(testDefinitions).HasCount().EqualTo(1);
        
        var testDef = testDefinitions.First();
        var methodArgs = testDef.MethodArgumentsProvider();
        await Assert.That(methodArgs).HasCount().EqualTo(3);
        await Assert.That(methodArgs[0]).IsEqualTo(1);
        await Assert.That(methodArgs[1]).IsEqualTo("one");
        await Assert.That(methodArgs[2]).IsEqualTo(true);
    }
    
    [Test]
    public async Task BuildTestsAsync_WithPropertyInjection_SetsPropertiesCorrectly()
    {
        // Arrange
        var testMethod = typeof(TestClass).GetMethod(nameof(TestClass.PropertyTest))!;
        var testProperty = typeof(TestClass).GetProperty(nameof(TestClass.TestProperty))!;
        var metadata = new TestMetadata
        {
            TestIdTemplate = "TestClass.PropertyTest",
            TestClassType = typeof(TestClass),
            TestClassTypeReference = TypeReference.CreateConcrete($"{typeof(TestClass).FullName}, {typeof(TestClass).Assembly.GetName().Name}"),
            MethodMetadata = CreateMethodMetadata(testMethod),
            TestFilePath = "test.cs",
            TestLineNumber = 50,
            TestClassFactory = (_) => new TestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = Array.Empty<IDataSourceProvider>(),
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>
            {
                [testProperty] = new InlineDataSourceProvider("injected value")
            },
            DisplayNameTemplate = "PropertyTest",
            RepeatCount = 1,
            IsAsync = false,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>()
        };
        
        // Act
        var testDefinitions = await _testBuilder.BuildTestsAsync(metadata);
        
        // Assert
        await Assert.That(testDefinitions).HasCount().EqualTo(1);
        
        var testDef = testDefinitions.First();
        var properties = testDef.PropertiesProvider();
        await Assert.That(properties).HasCount().EqualTo(1);
        await Assert.That(properties["TestProperty"]).IsEqualTo("injected value");
        
        // Verify property is actually set on instance
        var instance = testDef.TestClassFactory() as TestClass;
        await Assert.That(instance).IsNotNull();
        await Assert.That(instance!.TestProperty).IsEqualTo("injected value");
    }
    
    [Test]
    public async Task BuildTestsAsync_WithAsyncMethod_HandlesAsyncCorrectly()
    {
        // Arrange
        var testMethod = typeof(TestClass).GetMethod(nameof(TestClass.AsyncTest))!;
        var metadata = new TestMetadata
        {
            TestIdTemplate = "TestClass.AsyncTest",
            TestClassType = typeof(TestClass),
            TestClassTypeReference = TypeReference.CreateConcrete($"{typeof(TestClass).FullName}, {typeof(TestClass).Assembly.GetName().Name}"),
            MethodMetadata = CreateMethodMetadata(testMethod),
            TestFilePath = "test.cs",
            TestLineNumber = 60,
            TestClassFactory = (_) => new TestClass(),
            ClassDataSources = Array.Empty<IDataSourceProvider>(),
            MethodDataSources = Array.Empty<IDataSourceProvider>(),
            PropertyDataSources = new Dictionary<PropertyInfo, IDataSourceProvider>(),
            DisplayNameTemplate = "AsyncTest",
            RepeatCount = 1,
            IsAsync = true,
            IsSkipped = false,
            Attributes = Array.Empty<Attribute>()
        };
        
        // Act
        var testDefinitions = await _testBuilder.BuildTestsAsync(metadata);
        
        // Assert
        await Assert.That(testDefinitions).HasCount().EqualTo(1);
        
        var testDef = testDefinitions.First();
        var instance = testDef.TestClassFactory();
        
        // Verify the method can be invoked without throwing
        try
        {
            await testDef.TestMethodInvoker(instance, CancellationToken.None);
            // Test passes if no exception is thrown
        }
        catch (Exception ex)
        {
            await Assert.That(ex).IsNull(); // This will fail and show the exception
        }
    }
    
    private static MethodMetadata CreateMethodMetadata(MethodInfo method)
    {
        // Create minimal metadata for testing - in real usage this would be generated by source generator
        var classMetadata = new ClassMetadata
        {
            Name = method.DeclaringType!.Name,
            Type = method.DeclaringType!,
            TypeReference = TypeReference.CreateConcrete($"{method.DeclaringType!.FullName}, {method.DeclaringType!.Assembly.GetName().Name}"),
            Attributes = Array.Empty<AttributeMetadata>(),
            Namespace = method.DeclaringType!.Namespace,
            Assembly = new AssemblyMetadata 
            { 
                Name = method.DeclaringType!.Assembly.GetName().Name!,
                Attributes = Array.Empty<AttributeMetadata>()
            },
            Parameters = Array.Empty<ParameterMetadata>(),
            Properties = Array.Empty<PropertyMetadata>(),
            Constructors = Array.Empty<ConstructorMetadata>(),
            Parent = null
        };
        
        return new MethodMetadata
        {
            Name = method.Name,
            Type = method.DeclaringType!,
            TypeReference = TypeReference.CreateConcrete($"{method.DeclaringType!.FullName}, {method.DeclaringType!.Assembly.GetName().Name}"),
            Parameters = method.GetParameters().Select(p => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name!,
                TypeReference = TypeReference.CreateConcrete($"{p.ParameterType.FullName}, {p.ParameterType.Assembly.GetName().Name}"),
                ReflectionInfo = p,
                Attributes = Array.Empty<AttributeMetadata>()
            }).ToArray(),
            GenericTypeCount = 0,
            Class = classMetadata,
            ReturnType = method.ReturnType,
            ReturnTypeReference = TypeReference.CreateConcrete($"{method.ReturnType.FullName}, {method.ReturnType.Assembly.GetName().Name}"),
            Attributes = Array.Empty<AttributeMetadata>()
        };
    }
    
    // Test class for testing
    private class TestClass
    {
        public string? TestProperty { get; set; }
        
        public void SimpleTest()
        {
        }
        
        public void ParameterizedTest(int value, string text)
        {
        }
        
        public void TupleTest(int a, string b, bool c)
        {
        }
        
        public void PropertyTest()
        {
            // Uses TestProperty
        }
        
        public async Task AsyncTest()
        {
            await Task.Delay(1);
        }
    }
}