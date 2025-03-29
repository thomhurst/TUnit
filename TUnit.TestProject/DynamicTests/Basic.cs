namespace TUnit.TestProject.DynamicTests;

public class Basic
{
    public void SomeMethod()
    {
    }
    
    [DynamicTestBuilder]
    public void BuildTests(DynamicTestBuilderContext context)
    {
        // This is a test builder method that generates dynamic tests.
        // It can be used to create tests based on certain conditions or data.
        // The generated tests will be executed by the TUnit framework.
        // You can use this method to create multiple tests with different parameters or configurations.
        // For example, you can create tests for different input values or scenarios.
        // The TUnit framework will automatically discover and execute these tests.
        // This is a simple example of a dynamic test builder method.
        // You can customize the test generation logic as per your requirements.

        context.AddTest(new DynamicTest<Basic>
        {
            TestMethod = @class => @class.SomeMethod,
            TestMethodArguments = [],
        });
    }
}