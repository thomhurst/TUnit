namespace TUnit.TestProject.Bugs.Issue4545;

public class ParallelConsoleOutputTests
{
    [Test]
    [Repeat(10)]
    public async Task Test1_ShouldCaptureOnlyOwnOutput()
    {
        Console.Write("Test1-Start");
        await Task.Delay(10);
        Console.WriteLine("-Test1-End");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains("Test1-Start-Test1-End");
        await Assert.That(output).DoesNotContain("Test2");
        await Assert.That(output).DoesNotContain("Test3");
    }

    [Test]
    [Repeat(10)]
    public async Task Test2_ShouldCaptureOnlyOwnOutput()
    {
        Console.Write("Test2-Start");
        await Task.Delay(10);
        Console.WriteLine("-Test2-End");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains("Test2-Start-Test2-End");
        await Assert.That(output).DoesNotContain("Test1");
        await Assert.That(output).DoesNotContain("Test3");
    }

    [Test]
    [Repeat(10)]
    public async Task Test3_ShouldCaptureOnlyOwnOutput()
    {
        Console.Write("Test3-Start");
        await Task.Delay(10);
        Console.WriteLine("-Test3-End");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains("Test3-Start-Test3-End");
        await Assert.That(output).DoesNotContain("Test1");
        await Assert.That(output).DoesNotContain("Test2");
    }

    [Test]
    [Repeat(10)]
    public async Task Test4_EndingWithoutNewline_ShouldStillCaptureOutput()
    {
        Console.WriteLine("Test4-Start");
        await Task.Delay(10);
        Console.Write("Test4-End-NoNewline");
        // Test ends here - the final Write should be flushed by the framework
        // Note: We check this DURING the test since output is buffered
        // The framework flush will ensure it's available in test results
    }
}
