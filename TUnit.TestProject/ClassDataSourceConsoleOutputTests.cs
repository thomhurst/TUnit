using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

/// <summary>
/// Tests for verifying that console output from ClassDataSource constructors
/// and IAsyncInitializer.InitializeAsync is properly captured and displayed.
/// </summary>
public class TestDataWithConstructorOutput
{
    public TestDataWithConstructorOutput()
    {
        Console.WriteLine("TestDataWithConstructorOutput: Constructor called");
    }
}

public class TestDataWithAsyncInitializer : IAsyncInitializer
{
    public TestDataWithAsyncInitializer()
    {
        Console.WriteLine("TestDataWithAsyncInitializer: Constructor called");
    }

    public Task InitializeAsync()
    {
        Console.WriteLine("TestDataWithAsyncInitializer: InitializeAsync called");
        return Task.CompletedTask;
    }
}

public class TestDataWithBothOutputs : IAsyncInitializer
{
    public TestDataWithBothOutputs()
    {
        Console.WriteLine("TestDataWithBothOutputs: Constructor called");
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("TestDataWithBothOutputs: InitializeAsync starting");
        await Task.Delay(10); // Simulate some async work
        Console.WriteLine("TestDataWithBothOutputs: InitializeAsync completed");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ClassDataSourceConsoleOutputTests
{
    [ClassDataSource<TestDataWithConstructorOutput>]
    public required TestDataWithConstructorOutput DataWithConstructorOutput { get; init; }

    [Test]
    public async Task ConstructorOutput_ShouldBeCaptured()
    {
        var output = TestContext.Current!.GetStandardOutput();
        Console.WriteLine($"Test captured output: {output}");
        
        await Assert.That(output).Contains("TestDataWithConstructorOutput: Constructor called");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ClassDataSourceAsyncInitializerOutputTests
{
    [ClassDataSource<TestDataWithAsyncInitializer>]
    public required TestDataWithAsyncInitializer DataWithAsyncInitializer { get; init; }

    [Test]
    public async Task AsyncInitializerOutput_ShouldBeCaptured()
    {
        var output = TestContext.Current!.GetStandardOutput();
        Console.WriteLine($"Test captured output: {output}");
        
        // Both constructor and InitializeAsync output should be captured
        await Assert.That(output).Contains("TestDataWithAsyncInitializer: Constructor called");
        await Assert.That(output).Contains("TestDataWithAsyncInitializer: InitializeAsync called");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ClassDataSourceBothOutputsTests
{
    [ClassDataSource<TestDataWithBothOutputs>]
    public required TestDataWithBothOutputs DataWithBothOutputs { get; init; }

    [Test]
    public async Task BothConstructorAndInitializeAsyncOutput_ShouldBeCaptured()
    {
        var output = TestContext.Current!.GetStandardOutput();
        Console.WriteLine($"Test captured output: {output}");
        
        // All console output should be captured in order
        await Assert.That(output).Contains("TestDataWithBothOutputs: Constructor called");
        await Assert.That(output).Contains("TestDataWithBothOutputs: InitializeAsync starting");
        await Assert.That(output).Contains("TestDataWithBothOutputs: InitializeAsync completed");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class ClassDataSourceSharedPerSessionOutputTests
{
    [ClassDataSource<TestDataWithBothOutputs>(Shared = SharedType.PerTestSession)]
    public required TestDataWithBothOutputs SharedData { get; init; }

    [Test]
    public async Task SharedDataOutput_ShouldBeCapturedInFirstTest()
    {
        var output = TestContext.Current!.GetStandardOutput();
        Console.WriteLine($"Test1 captured output: {output}");
        
        // Output should be captured even for shared instances
        await Assert.That(output).Contains("TestDataWithBothOutputs: Constructor called");
    }

    [Test]
    public async Task SharedDataOutput_ShouldAlsoBeCapturedInSecondTest()
    {
        var output = TestContext.Current!.GetStandardOutput();
        Console.WriteLine($"Test2 captured output: {output}");
        
        // Since the data is shared, the output should be in this test too
        await Assert.That(output).Contains("TestDataWithBothOutputs: Constructor called");
    }
}
