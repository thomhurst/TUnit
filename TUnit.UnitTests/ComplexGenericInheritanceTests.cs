using TUnit.Core;

namespace TUnit.UnitTests;

public class ComplexGenericInheritanceTests
{
    private static TestMetadata<T> CreateTestMetadata<T>(string testId, string methodName) where T : class
    {
        return new TestMetadata<T>
        {
            TestClassType = typeof(T),
            TestMethodName = methodName,
            TestName = methodName,
            FilePath = "Unknown",
            LineNumber = 0,
            AttributeFactory = () => [],
            MethodMetadata = new MethodMetadata
            {
                Type = typeof(T),
                TypeReference = TypeReference.CreateConcrete(typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name),
                Name = methodName,
                GenericTypeCount = 0,
                ReturnType = typeof(void),
                ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName ?? "System.Void"),
                Parameters = [],
                Class = new ClassMetadata
                {
                    Type = typeof(T),
                    TypeReference = TypeReference.CreateConcrete(typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name),
                    Name = typeof(T).Name,
                    Namespace = typeof(T).Namespace ?? string.Empty,
                    Assembly = new AssemblyMetadata
                    {
                        Name = typeof(T).Assembly.GetName().Name ?? string.Empty
                    },
                    Parent = null,
                    Parameters = [],
                    Properties = []
                }
            },
            DataSources = [],
            ClassDataSources = [],
            PropertyDataSources = []
        };
    }

    [Test]
    public async Task TestDependency_ProblemStatement_FirstClassBaseDependency()
    {
        // Test that FirstClassBase dependency matches ComplexGenericDependsOn tests
        var dependency = TestDependency.FromClass(typeof(FirstClassBase));
        var testMetadata = CreateTestMetadata<ComplexGenericDependsOn>("test1", "Test300");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_ProblemStatement_SecondClassBaseDependency()
    {
        // Test that SecondClassBase<,> dependency matches ComplexGenericDependsOn tests
        var dependency = TestDependency.FromClass(typeof(SecondClassBase<,>));
        var testMetadata = CreateTestMetadata<ComplexGenericDependsOn>("test1", "Test300");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_ProblemStatement_ThirdClassBaseDependency()
    {
        // Test that ThirdClassBase<,> dependency matches ComplexGenericDependsOn tests
        var dependency = TestDependency.FromClass(typeof(ThirdClassBase<,>));
        var testMetadata = CreateTestMetadata<ComplexGenericDependsOn>("test1", "Test300");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_ProblemStatement_ConcreteClassDependency()
    {
        // Test that ComplexGenericDependsOn dependency matches its own tests
        var dependency = TestDependency.FromClass(typeof(ComplexGenericDependsOn));
        var testMetadata = CreateTestMetadata<ComplexGenericDependsOn>("test1", "Test300");
        
        var matches = dependency.Matches(testMetadata);
        
        await Assert.That(matches).IsTrue();
    }

    // Test classes from the problem statement  
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