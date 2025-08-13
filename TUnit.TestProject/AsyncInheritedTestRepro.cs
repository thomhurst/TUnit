using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

// Base class with async test method that will be inherited
public class BaseTestWithAsyncMethod
{
    [Test]
    public async Task AsyncTestMethod()
    {
        await Task.Delay(10);
        // This test should pass when inherited
    }
    
    [Test]
    public void SyncTestMethod()
    {
        // This test should always work fine
    }
}

// Derived class that inherits the async test - this should now work
[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class DerivedClassWithAsyncInheritance : BaseTestWithAsyncMethod
{
    // Should inherit both AsyncTestMethod and SyncTestMethod
    // The AsyncTestMethod inheritance was previously broken for code generation
}