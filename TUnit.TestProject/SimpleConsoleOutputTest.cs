using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class SimpleTestData
{
    public SimpleTestData()
    {
        Console.WriteLine("CONSTRUCTOR_OUTPUT");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class SimpleConsoleOutputTest
{
    [ClassDataSource<SimpleTestData>]
    public required SimpleTestData Data { get; init; }

    [Test]
    public async Task Test()
    {
        var output = TestContext.Current!.GetStandardOutput();
        Console.WriteLine($"Captured: '{output}'");
        await Assert.That(output).Contains("CONSTRUCTOR_OUTPUT");
    }
}
