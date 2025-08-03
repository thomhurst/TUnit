using TUnit.Core;

namespace Playground;

// This class should trigger TUnit0052 warning about multiple constructors
public class MultipleConstructorsDemo
{
    public MultipleConstructorsDemo()
    {
    }

    public MultipleConstructorsDemo(string value)
    {
    }

    [Test]
    public void ExampleTest()
    {
        // This should trigger warning TUnit0052
    }
}

// This class should work fine with TestConstructor
public class TestConstructorDemo
{
    public TestConstructorDemo()
    {
    }

    [TestConstructor]
    public TestConstructorDemo(string value)
    {
    }

    [Test]
    public void ExampleTestWithConstructor()
    {
        // This should use the marked constructor without warnings
    }
}