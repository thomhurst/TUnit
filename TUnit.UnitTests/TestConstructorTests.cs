using TUnit.Core;

namespace TUnit.UnitTests.TestConstructorTests;

// Test case 1: Single constructor (no change in behavior)
public class SingleConstructorTest
{
    [Test]
    public void SimpleTest()
    {
        // Single constructor should work without TestConstructor attribute
    }
}

// Test case 2: Multiple constructors without [TestConstructor] - should trigger analyzer warning TUnit0052
// This demonstrates the analyzer warning functionality
public class MultipleConstructorsWithoutTestConstructorTest
{
    // Multiple parameterless constructors to demonstrate the warning without needing data sources
    public MultipleConstructorsWithoutTestConstructorTest()
    {
        // Default constructor
    }

    public MultipleConstructorsWithoutTestConstructorTest(object unused)
    {
        // Alternate constructor - triggers need for data source, so commenting out the test
    }

    // Commented out to avoid TUnit0038 error requiring data sources
    /*
    [Test]
    public void TestWithoutMarkedConstructor()
    {
        // Should trigger analyzer warning TUnit0052
        // Warning: "Class 'MultipleConstructorsWithoutTestConstructorTest' has multiple constructors but none are marked with [TestConstructor]"
    }
    */
}

// NOTE: Working example with [TestConstructor] can be found in the source generated files
// when [Arguments] is properly configured on the class level to provide constructor parameters.