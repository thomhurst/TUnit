namespace TUnit.UnitTests;

public class DependsOnTests
{
    private static TestMetadata<T> CreateTestMetadata<T>(string testId, string methodName, int parameterCount = 0, Type[]? parameterTypes = null) where T : class
    {
        if (parameterTypes == null && parameterCount > 0)
        {
            parameterTypes = parameterCount == 1 ? [typeof(string)] : [typeof(string), typeof(int)];
        }

        var parameters = parameterTypes?.Select((type, index) => 
            new ParameterMetadata(type)
            {
                Name = $"param{index}",
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
                ReflectionInfo = null!,
                IsNullable = false
            }).ToArray() ?? [];

        return new TestMetadata<T>
        {
            TestClassType = typeof(T),
            TestMethodName = methodName,
            TestName = methodName,
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () =>
            [
            ],
            MethodMetadata = new MethodMetadata
            {
                Type = typeof(T),
                TypeReference = TypeReference.CreateConcrete(typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name),
                Name = methodName,
                GenericTypeCount = 0,
                ReturnType = typeof(void),
                ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName ?? "System.Void"),
                Parameters = parameters,
                Class = new ClassMetadata
                {
                    Type = typeof(T),
                    TypeReference = TypeReference.CreateConcrete(typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name),
                    Name = typeof(T).Name,
                    Namespace = typeof(T).Namespace ?? string.Empty,
                    Assembly = new AssemblyMetadata
                    {
                        Name = typeof(T).Assembly.GetName()
                                .Name
                            ?? string.Empty
                    },
                    Parent = null,
                    Parameters =
                    [
                    ],
                    Properties =
                    [
                    ]
                }
            },
            DataSources = new IDataSourceAttribute[]
            {
            },
            ClassDataSources = new IDataSourceAttribute[]
            {
            },
            PropertyDataSources = new PropertyDataSource[]
            {
            }
        };
    }
    [Test]
    public async Task TestDependency_FromMethodName_MatchesSameClass()
    {
        var dependency = TestDependency.FromMethodName("TestMethod");
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "TestMethod");
        var dependentTest = CreateTestMetadata<DependsOnTests>("test2", "DependentTest");
        
        var matches = dependency.Matches(testMetadata, dependentTest);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_FromMethodName_DoesNotMatchDifferentClass()
    {
        var dependency = TestDependency.FromMethodName("TestMethod");
        var testMetadata = CreateTestMetadata<string>("test1", "TestMethod");
        var dependentTest = CreateTestMetadata<DependsOnTests>("test2", "DependentTest");
        
        var matches = dependency.Matches(testMetadata, dependentTest);
        
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task TestDependency_FromClass_MatchesAllTestsInClass()
    {
        var dependency = TestDependency.FromClass(typeof(DependsOnTests));
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "AnyTestMethod");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_FromClass_DoesNotMatchSelf()
    {
        var dependency = TestDependency.FromClass(typeof(DependsOnTests));
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "TestMethod");
        
        var matches = dependency.Matches(testMetadata, testMetadata);
        
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task TestDependency_FromClassAndMethod_MatchesSpecificTest()
    {
        var dependency = TestDependency.FromClassAndMethod(typeof(DependsOnTests), "SpecificMethod");
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "SpecificMethod");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithParameters_MatchesCorrectOverload()
    {
        var dependency = new TestDependency
        {
            ClassType = typeof(DependsOnTests),
            MethodName = "OverloadedMethod",
            MethodParameters = [typeof(string), typeof(int)]
        };
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "OverloadedMethod", 2);
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithParameters_DoesNotMatchWrongOverload()
    {
        var dependency = new TestDependency
        {
            ClassType = typeof(DependsOnTests),
            MethodName = "OverloadedMethod",
            MethodParameters = [typeof(string), typeof(int)]
        };
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "OverloadedMethod", 1);
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task DependsOnAttribute_ToTestDependency_CreatesCorrectDependency()
    {
        var attr = new DependsOnAttribute("TestMethod");
        
        var dependency = attr.ToTestDependency();
        
        await Assert.That(dependency.MethodName).IsEqualTo("TestMethod");
        await Assert.That(dependency.ClassType).IsNull();
    }

    [Test]
    public async Task GenericDependsOnAttribute_ToTestDependency_CreatesCorrectDependency()
    {
        var attr = new DependsOnAttribute<string>();
        
        var dependency = attr.ToTestDependency();
        
        await Assert.That(dependency.ClassType).IsEqualTo(typeof(string));
        await Assert.That(dependency.MethodName).IsNull();
    }

    [Test]
    public async Task TestDependency_FromClass_MatchesInheritedTests()
    {
        var dependency = TestDependency.FromClass(typeof(BaseTestClass));
        var testMetadata = CreateTestMetadata<DerivedTestClass>("test1", "InheritedTest");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithGenericBase_MatchesInheritedTests()
    {
        // Use the closed generic type instead of open generic
        var dependency = TestDependency.FromClass(typeof(GenericBaseClass<string>));
        var testMetadata = CreateTestMetadata<DerivedFromGenericClass>("test1", "Test");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithOpenGenericBase_MatchesInheritedTests()
    {
        // Test with open generic type (like in the problem statement)
        var dependency = TestDependency.FromClass(typeof(GenericBaseClass<>));
        var testMetadata = CreateTestMetadata<DerivedFromGenericClass>("test1", "Test");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_ComplexGenericInheritance_MatchesCorrectly()
    {
        // Test complex inheritance like in the problem statement
        // Testing if SecondClassBase<,> dependency matches ComplexGenericDependsOn tests
        var dependency = TestDependency.FromClass(typeof(SecondClassBase<,>));
        var testMetadata = CreateTestMetadata<ComplexGenericDependsOn>("test1", "Test300");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_FromClassAndMethod_MatchesInheritedMethod()
    {
        var dependency = TestDependency.FromClassAndMethod(typeof(BaseTestClass), "BaseMethod");
        var testMetadata = CreateTestMetadata<DerivedTestClass>("test1", "BaseMethod");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    public class BaseTestClass
    {
        [Test]
        public void BaseMethod() { }
    }

    [InheritsTests]
    public class DerivedTestClass : BaseTestClass
    {
        [Test]
        public void InheritedTest() { }
    }

    public class GenericBaseClass<T>
    {
        public void GenericTest() { }
    }

    [InheritsTests]
    public class DerivedFromGenericClass : GenericBaseClass<string>
    {
        [Test]
        public void Test() { }
    }

    // Add classes to reproduce the problem statement
    public abstract class FirstClassBase
    {
        public Task Test001() => Task.Delay(50);
        public Task Test002() => Task.Delay(50);
        public Task Test003() => Task.Delay(50);
    }

    public abstract class SecondClassBase<TContent, TValue> : FirstClassBase
        where TContent : class
    {
        public Task Test100() => Task.Delay(50);
        public Task Test101() => Task.Delay(50);
        public Task Test102() => Task.Delay(50);
    }

    public abstract class ThirdClassBase<TContent, TValue> : SecondClassBase<TContent, TValue>
        where TContent : class
    {
        public Task Test200() => Task.Delay(50);
        public Task Test201() => Task.Delay(50);
        public Task Test202() => Task.Delay(50);
    }

    public class ComplexGenericDependsOn : ThirdClassBase<string, int>
    {
        public Task Test300() => Task.Delay(50);
        public Task Test301() => Task.Delay(50);
        public Task Test302() => Task.Delay(50);
    }
}
