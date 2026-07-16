namespace TUnit.TestProject;

public class SkipConstructorTest : IAsyncDisposable
{
    public static bool ConstructorCalled { get; set; }
    public static bool DisposeCalled { get; set; }

    public SkipConstructorTest()
    {
        ConstructorCalled = true;
        Console.WriteLine("SkipConstructorTest constructor called");
    }

    [Test]
    [Skip("Test should be skipped")]
    public void SkippedTestShouldNotCallConstructor()
    {
        Console.WriteLine("This test method should not run");
    }

    public ValueTask DisposeAsync()
    {
        DisposeCalled = true;
        Console.WriteLine("SkipConstructorTest dispose called");
        return new ValueTask();
    }
}

public class SkipConstructorTestValidation
{
    [Test]
    public async Task IfConstructorCalledThenDisposeShouldBeCalled()
    {
        // This test validates that if a constructor was called for a skipped test,
        // the corresponding disposal method should also be called
        if (SkipConstructorTest.ConstructorCalled)
        {
            if (!SkipConstructorTest.DisposeCalled)
            {
                throw new Exception("BUG: Constructor was called for skipped test but DisposeAsync was not called. This creates a resource leak.");
            }
            await Assert.That(SkipConstructorTest.DisposeCalled).IsTrue();
        }
        else
        {
            // Constructor not called is also valid behavior according to SkippedTestInstance design
            Console.WriteLine("Constructor was not called for skipped test (expected behavior)");
        }
    }
}
