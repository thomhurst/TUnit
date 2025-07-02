using System.Threading.Tasks;
using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.UnitTests;

public class DependsOnTests
{
    [Test]
    public async Task TestDependency_FromMethodName_MatchesSameClass()
    {
        // Arrange
        var dependency = TestDependency.FromMethodName("TestMethod");
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "TestMethod",
            TestName = "TestMethod",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };
        var dependentTest = new TestMetadata
        {
            TestId = "test2",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "DependentTest",
            TestName = "DependentTest",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(string), // Different class
            TestMethodName = "TestMethod",
            TestName = "TestMethod",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };
        var dependentTest = new TestMetadata
        {
            TestId = "test2",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "DependentTest",
            TestName = "DependentTest",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "AnyTestMethod",
            TestName = "AnyTestMethod",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "TestMethod",
            TestName = "TestMethod",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "SpecificMethod",
            TestName = "SpecificMethod",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "OverloadedMethod",
            TestName = "OverloadedMethod",
            ParameterCount = 2,
            ParameterTypes = new[] { typeof(string), typeof(int) },
            TestMethodParameterTypes = new[] { "System.String", "System.Int32" }
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DependsOnTests),
            TestMethodName = "OverloadedMethod",
            TestName = "OverloadedMethod",
            ParameterCount = 1,
            ParameterTypes = new[] { typeof(string) },
            TestMethodParameterTypes = new[] { "System.String" }
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DerivedTestClass), // Derived class
            TestMethodName = "InheritedTest",
            TestName = "InheritedTest",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DerivedFromGenericClass), // Derived from GenericBaseClass<string>
            TestMethodName = "Test",
            TestName = "Test",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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
        var testMetadata = new TestMetadata
        {
            TestId = "test1",
            TestClassType = typeof(DerivedTestClass), // Method is inherited from base
            TestMethodName = "BaseMethod",
            TestName = "BaseMethod",
            ParameterCount = 0,
            ParameterTypes = [],
            TestMethodParameterTypes = []
        };

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