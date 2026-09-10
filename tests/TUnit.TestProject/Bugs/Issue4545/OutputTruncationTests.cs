namespace TUnit.TestProject.Bugs.Issue4545;

/// <summary>
/// Tests to reproduce the issue where console output is truncated/missing
/// when tests complete, especially when they end with Console.Write (no newline).
/// </summary>
[NotInParallel]
public class OutputTruncationTests
{
    [Test]
    public async Task Test1_EndsWithWrite_ShouldCaptureAllOutput()
    {
        Console.WriteLine("Test1: Start");
        Console.WriteLine("Test1: Middle");
        Console.Write("Test1: End (no newline)"); // This may be lost!

        // Small delay to simulate real work
        await Task.Delay(10);
    }

    [Test]
    public async Task Test2_EndsWithWrite_ShouldCaptureAllOutput()
    {
        Console.WriteLine("Test2: Start");
        Console.WriteLine("Test2: Middle");
        Console.Write("Test2: End (no newline)"); // This may be lost!

        await Task.Delay(10);
    }

    [Test]
    public async Task Test3_EndsWithWriteLine_ShouldCaptureAllOutput()
    {
        Console.WriteLine("Test3: Start");
        Console.WriteLine("Test3: Middle");
        Console.WriteLine("Test3: End (with newline)"); // This should be captured

        await Task.Delay(10);
    }
}
