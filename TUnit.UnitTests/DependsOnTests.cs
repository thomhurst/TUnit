using TUnit.Core;

namespace TUnit.UnitTests;

public class DependsOnTests
{
    private static TestMetadata<T> CreateTestMetadata<T>(string testId, string methodName, int parameterCount = 0, Type[]? parameterTypes = null, string[]? parameterTypeNames = null) where T : class
    {
        if (parameterTypes == null && parameterCount > 0)
        {
            parameterTypes = parameterCount == 1 ? new[] { typeof(string) } : new[] { typeof(string), typeof(int) };
            parameterTypeNames = parameterCount == 1 ? new[] { "System.String" } : new[] { "System.String", "System.Int32" };
        }

        return new TestMetadata<T>
        {
            TestClassType = typeof(T),
            TestMethodName = methodName,
            TestName = methodName,
            ParameterCount = parameterCount,
            ParameterTypes = parameterTypes ?? new Type[0],
            TestMethodParameterTypes = parameterTypeNames ?? new string[0],
            AttributeFactory = () => [],
            MethodMetadata = new MethodMetadata
            {
                Type = typeof(T),
                TypeReference = TypeReference.CreateConcrete(typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name),
                Name = methodName,
                GenericTypeCount = 0,
                ReturnType = typeof(void),
                ReturnTypeReference = TypeReference.CreateConcrete(typeof(void).AssemblyQualifiedName ?? "System.Void"),
                Parameters = Array.Empty<ParameterMetadata>(),
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
                    Parameters = Array.Empty<ParameterMetadata>(),
                    Properties = Array.Empty<PropertyMetadata>()
                }
            }
        };
    }
    [Test]
    public async Task TestDependency_FromMethodName_MatchesSameClass()
    {
        // Arrange
        var dependency = TestDependency.FromMethodName("TestMethod");
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "TestMethod");
        var dependentTest = CreateTestMetadata<DependsOnTests>("test2", "DependentTest");

        // Act
        var matches = dependency.Matches(testMetadata, dependentTest);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_FromMethodName_DoesNotMatchDifferentClass()
    {
        // Arrange
        var dependency = TestDependency.FromMethodName("TestMethod");
        var testMetadata = CreateTestMetadata<string>("test1", "TestMethod");
        var dependentTest = CreateTestMetadata<DependsOnTests>("test2", "DependentTest");

        // Act
        var matches = dependency.Matches(testMetadata, dependentTest);

        // Assert
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task TestDependency_FromClass_MatchesAllTestsInClass()
    {
        // Arrange
        var dependency = TestDependency.FromClass(typeof(DependsOnTests));
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "AnyTestMethod");

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_FromClass_DoesNotMatchSelf()
    {
        // Arrange
        var dependency = TestDependency.FromClass(typeof(DependsOnTests));
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "TestMethod");

        // Act - same test as dependent
        var matches = dependency.Matches(testMetadata, testMetadata);

        // Assert
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task TestDependency_FromClassAndMethod_MatchesSpecificTest()
    {
        // Arrange
        var dependency = TestDependency.FromClassAndMethod(typeof(DependsOnTests), "SpecificMethod");
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "SpecificMethod");

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithParameters_MatchesCorrectOverload()
    {
        // Arrange
        var dependency = new TestDependency
        {
            ClassType = typeof(DependsOnTests),
            MethodName = "OverloadedMethod",
            MethodParameters = new[] { typeof(string), typeof(int) }
        };
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "OverloadedMethod", 2);

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithParameters_DoesNotMatchWrongOverload()
    {
        // Arrange
        var dependency = new TestDependency
        {
            ClassType = typeof(DependsOnTests),
            MethodName = "OverloadedMethod",
            MethodParameters = new[] { typeof(string), typeof(int) }
        };
        var testMetadata = CreateTestMetadata<DependsOnTests>("test1", "OverloadedMethod", 1);

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsFalse();
    }

    [Test]
    public async Task DependsOnAttribute_ToTestDependency_CreatesCorrectDependency()
    {
        // Arrange
        var attr = new DependsOnAttribute("TestMethod");

        // Act
        var dependency = attr.ToTestDependency();

        // Assert
        await Assert.That(dependency.MethodName).IsEqualTo("TestMethod");
        await Assert.That(dependency.ClassType).IsNull();
    }

    [Test]
    public async Task GenericDependsOnAttribute_ToTestDependency_CreatesCorrectDependency()
    {
        // Arrange
        var attr = new DependsOnAttribute<string>(); // Depends on all tests in string class

        // Act
        var dependency = attr.ToTestDependency();

        // Assert
        await Assert.That(dependency.ClassType).IsEqualTo(typeof(string));
        await Assert.That(dependency.MethodName).IsNull();
    }

    [Test]
    public async Task TestDependency_FromClass_MatchesInheritedTests()
    {
        // Arrange
        var dependency = TestDependency.FromClass(typeof(BaseTestClass));
        var testMetadata = CreateTestMetadata<DerivedTestClass>("test1", "InheritedTest");

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_WithGenericBase_MatchesInheritedTests()
    {
        // Arrange
        var dependency = TestDependency.FromClass(typeof(GenericBaseClass<>));
        var testMetadata = CreateTestMetadata<DerivedFromGenericClass>("test1", "Test");

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    [Test]
    public async Task TestDependency_FromClassAndMethod_MatchesInheritedMethod()
    {
        // Arrange
        var dependency = TestDependency.FromClassAndMethod(typeof(BaseTestClass), "BaseMethod");
        var testMetadata = CreateTestMetadata<DerivedTestClass>("test1", "BaseMethod");

        // Act
        var matches = dependency.Matches(testMetadata);

        // Assert
        await Assert.That(matches).IsTrue();
    }

    // Test classes for inheritance scenarios
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
        // Not marking with [Test] since it's a generic class
        public void GenericTest() { }
    }

    [InheritsTests]
    public class DerivedFromGenericClass : GenericBaseClass<string>
    {
        [Test]
        public void Test() { }
    }
}
