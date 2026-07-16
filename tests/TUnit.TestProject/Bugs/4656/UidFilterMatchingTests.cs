using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._4656;

/// <summary>
/// Test fixtures for GitHub issue #4656 follow-up: UID filter matching for VS Test Explorer.
/// These test various scenarios that the MetadataFilterMatcher.CouldMatchUidFilter must handle:
/// - Nested classes (Outer+Inner format)
/// - Generic classes (MyClass&lt;T&gt; format)
/// - Classes with constructor parameters (MyClass(params) format)
/// - Nested generic classes (Outer&lt;T&gt;+Inner format)
/// </summary>

#region Nested Classes Tests

[EngineTest(ExpectedResult.Pass)]
public class OuterClass
{
    [Test]
    public async Task OuterMethod()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [EngineTest(ExpectedResult.Pass)]
    public class InnerClass
    {
        [Test]
        public async Task InnerMethod()
        {
            await Assert.That(true).IsEqualTo(true);
        }
    }
}

#endregion

#region Generic Classes Tests

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class GenericTestClass<T>
{
    [Test]
    public async Task GenericClassMethod()
    {
        await Assert.That(typeof(T)).IsNotNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int), typeof(string))]
public class MultiGenericTestClass<T1, T2>
{
    [Test]
    public async Task MultiGenericClassMethod()
    {
        await Assert.That(typeof(T1)).IsNotNull();
        await Assert.That(typeof(T2)).IsNotNull();
    }
}

#endregion

#region Classes With Constructor Parameters Tests

[EngineTest(ExpectedResult.Pass)]
[Arguments("test-value")]
public class ClassWithStringParam(string value)
{
    [Test]
    public async Task MethodWithParam()
    {
        await Assert.That(value).IsNotNull();
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("test", 42)]
public class ClassWithMultipleParams(string name, int count)
{
    [Test]
    public async Task MethodWithMultipleParams()
    {
        await Assert.That(name).IsNotNull();
        await Assert.That(count).IsEqualTo(42);
    }
}

#endregion

#region Nested Generic Classes Tests

[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class OuterGenericClass<T>
{
    [Test]
    public async Task OuterGenericMethod()
    {
        await Assert.That(typeof(T)).IsNotNull();
    }
}

#endregion

#region Overlapping Names With Different Suffixes

/// <summary>
/// Tests for ensuring "Test" doesn't match "TestHelper" or "Testing"
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class FilterTest
{
    [Test]
    public async Task Method1()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class FilterTestHelper
{
    [Test]
    public async Task Method1()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class FilterTesting
{
    [Test]
    public async Task Method1()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}

#endregion

#region Method Name Boundary Tests

/// <summary>
/// Tests for ensuring method name "Test" doesn't match "TestMethod" or "MyTest"
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class MethodNameBoundaryTests
{
    [Test]
    public async Task Test()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test]
    public async Task TestMethod()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test]
    public async Task MyTest()
    {
        await Assert.That(true).IsEqualTo(true);
    }

    [Test]
    public async Task TestingMethod()
    {
        await Assert.That(true).IsEqualTo(true);
    }
}

#endregion
