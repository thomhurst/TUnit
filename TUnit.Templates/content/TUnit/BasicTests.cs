namespace TestProject;

public class BasicTests
{
    [Before(Class)]
    public static Task BeforeClass(ClassHookContext context)
    {
        // Runs once before all tests in this class
        return Task.CompletedTask;
    }

    [After(Class)]
    public static Task AfterClass(ClassHookContext context)
    {
        // Runs once after all tests in this class
        return Task.CompletedTask;
    }

    [Before(Test)]
    public Task BeforeTest(TestContext context)
    {
        // Runs before each test in this class
        return Task.CompletedTask;
    }

    [After(Test)]
    public Task AfterTest(TestContext context)
    {
        // Runs after each test in this class
        return Task.CompletedTask;
    }

    [Test]
    public async Task Add_ReturnsSum()
    {
        var calculator = new Calculator();

        var result = calculator.Add(1, 2);

        await Assert.That(result).IsEqualTo(3);
    }

    [Test]
    public async Task Divide_ByZero_ThrowsException()
    {
        var calculator = new Calculator();

        var action = () => calculator.Divide(1, 0);

        await Assert.That(action).ThrowsException()
            .WithMessage("Attempted to divide by zero.");
    }
}
