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
