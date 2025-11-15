using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

/// <summary>
/// Tests to verify that console output during test building (data source construction/initialization)
/// is properly captured and included in test results.
/// Issue #3833: https://github.com/thomhurst/TUnit/issues/3833
/// </summary>
public class TestBuildContextOutputCaptureTests
{
    /// <summary>
    /// Data source that writes to console in constructor
    /// </summary>
    public class DataSourceWithConstructorOutput
    {
        public string Value { get; }

        public DataSourceWithConstructorOutput()
        {
            Console.WriteLine("DataSource constructor: Creating instance");
            Console.WriteLine("DataSource constructor: Initializing value");
            Value = "TestValue";
            Console.WriteLine("DataSource constructor: Instance created successfully");
        }
    }

    /// <summary>
    /// Data source that writes to console in async initializer
    /// </summary>
    public class DataSourceWithAsyncInitOutput : IAsyncInitializer
    {
        public string Value { get; private set; } = "Uninitialized";

        public DataSourceWithAsyncInitOutput()
        {
            Console.WriteLine("AsyncDataSource constructor: Creating instance");
        }

        public async Task InitializeAsync()
        {
            Console.WriteLine("AsyncDataSource.InitializeAsync: Starting initialization");
            await Task.Delay(10); // Simulate async work
            Console.WriteLine("AsyncDataSource.InitializeAsync: Setting value");
            Value = "InitializedValue";
            Console.WriteLine("AsyncDataSource.InitializeAsync: Initialization complete");
        }
    }

    /// <summary>
    /// Data source that writes to error output
    /// </summary>
    public class DataSourceWithErrorOutput
    {
        public string Value { get; }

        public DataSourceWithErrorOutput()
        {
            Console.Error.WriteLine("DataSource error: This is an error message during construction");
            Value = "ErrorTestValue";
        }
    }

    [Test]
    [ClassDataSource<DataSourceWithConstructorOutput>]
    public async Task Test_CapturesConstructorOutput_InTestResults(DataSourceWithConstructorOutput data)
    {
        // The constructor output should be captured during test building
        // and included in the test's output

        // Get the test output
        var output = TestContext.Current!.GetStandardOutput();

        // Verify constructor output is present
        await Assert.That(output).Contains("DataSource constructor: Creating instance");
        await Assert.That(output).Contains("DataSource constructor: Initializing value");
        await Assert.That(output).Contains("DataSource constructor: Instance created successfully");

        // Verify the data source was created correctly
        await Assert.That(data.Value).IsEqualTo("TestValue");
    }

    [Test]
    [ClassDataSource<DataSourceWithAsyncInitOutput>]
    public async Task Test_CapturesAsyncInitializerOutput_InTestResults(DataSourceWithAsyncInitOutput data)
    {
        // The InitializeAsync output should be captured during test building
        // and included in the test's output

        // Get the test output
        var output = TestContext.Current!.GetStandardOutput();

        // Verify constructor and InitializeAsync output is present
        await Assert.That(output).Contains("AsyncDataSource constructor: Creating instance");
        await Assert.That(output).Contains("AsyncDataSource.InitializeAsync: Starting initialization");
        await Assert.That(output).Contains("AsyncDataSource.InitializeAsync: Setting value");
        await Assert.That(output).Contains("AsyncDataSource.InitializeAsync: Initialization complete");

        // Verify the data source was initialized correctly
        await Assert.That(data.Value).IsEqualTo("InitializedValue");
    }

    [Test]
    [ClassDataSource<DataSourceWithErrorOutput>]
    public async Task Test_CapturesErrorOutput_InTestResults(DataSourceWithErrorOutput data)
    {
        // Error output during construction should be captured
        var errorOutput = TestContext.Current!.GetErrorOutput();

        // Verify error output is present
        await Assert.That(errorOutput).Contains("DataSource error: This is an error message during construction");

        // Verify the data source was created correctly
        await Assert.That(data.Value).IsEqualTo("ErrorTestValue");
    }

    /// <summary>
    /// Shared data source (PerTestSession) - output should appear in all tests that use it
    /// </summary>
    public class SharedDataSource
    {
        public string Value { get; }

        public SharedDataSource()
        {
            Console.WriteLine("SharedDataSource constructor: This should appear in all tests using this data source");
            Value = "SharedValue";
        }
    }

    [Test]
    [ClassDataSource<SharedDataSource>(Shared = SharedType.PerTestSession)]
    public async Task Test_SharedDataSource_FirstTest(SharedDataSource data)
    {
        var output = TestContext.Current!.GetStandardOutput();

        // Should contain the shared data source construction output
        await Assert.That(output).Contains("SharedDataSource constructor:");
        await Assert.That(data.Value).IsEqualTo("SharedValue");
    }

    [Test]
    [ClassDataSource<SharedDataSource>(Shared = SharedType.PerTestSession)]
    public async Task Test_SharedDataSource_SecondTest(SharedDataSource data)
    {
        // NOTE: For PerTestSession shared data sources, the constructor only runs once
        // when the first test triggers creation. Subsequent tests use the already-created
        // instance, so they won't have the constructor output in their build context.
        // This is expected behavior.

        // Just verify the data source works
        await Assert.That(data.Value).IsEqualTo("SharedValue");
    }

    [Test]
    public async Task Test_ExecutionOutput_StillCaptured()
    {
        // Verify that execution-time output is still captured correctly
        Console.WriteLine("This is execution-time output");

        var output = TestContext.Current!.GetStandardOutput();
        await Assert.That(output).Contains("This is execution-time output");
    }

    [Test]
    [ClassDataSource<DataSourceWithConstructorOutput>]
    public async Task Test_BothBuildAndExecutionOutput_AreCombined(DataSourceWithConstructorOutput data)
    {
        // Write output during execution
        Console.WriteLine("This is execution-time output");

        var output = TestContext.Current!.GetStandardOutput();

        // Should contain both build-time and execution-time output
        await Assert.That(output).Contains("DataSource constructor: Creating instance");
        await Assert.That(output).Contains("This is execution-time output");

        // Build-time output should come before execution-time output
        var buildIndex = output.IndexOf("DataSource constructor: Creating instance", StringComparison.Ordinal);
        var execIndex = output.IndexOf("This is execution-time output", StringComparison.Ordinal);
        await Assert.That(buildIndex).IsLessThan(execIndex);
    }
}
