using TUnit.Core;

namespace TUnit.UnitTests.TestConstructorTests;

public class SingleConstructorTest
{
    [Test]
    public void SimpleTest()
    {
        // Single constructor should work without TestConstructor attribute
    }
}

[Arguments(true)]
public class MultipleConstructorsWithTestConstructorTest
{
    private readonly bool _usedPrimaryConstructor;

    public MultipleConstructorsWithTestConstructorTest()
    {
        _usedPrimaryConstructor = false;
    }

    [TestConstructor]
    public MultipleConstructorsWithTestConstructorTest(bool usedPrimary)
    {
        _usedPrimaryConstructor = usedPrimary;
    }

    [Test]
    public void TestWithMarkedConstructor()
    {
        // Should use the constructor marked with [TestConstructor]
        // This is just a placeholder test - the real test is whether the correct constructor is used
    }
}

public class MultipleConstructorsWithoutTestConstructorTest
{
    private readonly bool _usedFirstConstructor;

    public MultipleConstructorsWithoutTestConstructorTest()
    {
        _usedFirstConstructor = true;
    }

    public MultipleConstructorsWithoutTestConstructorTest(bool usedFirst)
    {
        _usedFirstConstructor = usedFirst;
    }

    [Test]
    public void TestWithoutMarkedConstructor()
    {
        // Should use first constructor and trigger analyzer warning TUnit0052
    }
}