using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.Lifecycle;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Service that registers test arguments during test discovery.
/// ONLY tracks objects for disposal - does NOT initialize them.
/// All initialization happens in TestInitializer just before test execution.
/// </summary>
internal sealed class TestArgumentRegistrationService : ITestRegisteredEventReceiver
{
    private readonly IObjectLifecycleManager _lifecycleManager;
    private readonly ObjectTracker _objectTracker;

    public TestArgumentRegistrationService(
        IObjectLifecycleManager lifecycleManager,
        ObjectTracker objectTracker)
    {
        _lifecycleManager = lifecycleManager;
        _objectTracker = objectTracker;
    }

    public int Order => int.MinValue; // Run first

    /// <summary>
    /// Called when a test is registered. Tracks objects for disposal but does NOT initialize.
    /// </summary>
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var testContext = context.TestContext;

        // Register class and method arguments for tracking (no initialization)
        RegisterArgumentsForTracking(testContext.Metadata.TestDetails.TestClassArguments);
        RegisterArgumentsForTracking(testContext.Metadata.TestDetails.TestMethodArguments);

        // Resolve property data sources and store for later injection
        await ResolvePropertyDataSourcesAsync(testContext);

        // Track all objects for disposal
        _objectTracker.TrackObjects(testContext);
    }

    private void RegisterArgumentsForTracking(object?[] arguments)
    {
        foreach (var arg in arguments)
        {
            if (arg != null)
            {
                _lifecycleManager.IncrementReferenceCount(arg);
            }
        }
    }

    /// <summary>
    /// Resolves property data sources and stores the values for later injection.
    /// Does NOT initialize the values - that happens in TestInitializer.
    /// </summary>
    private async ValueTask ResolvePropertyDataSourcesAsync(TestContext testContext)
    {
        var classType = testContext.Metadata.TestDetails.ClassType;
        var propertySource = PropertySourceRegistry.GetSource(classType);

        if (propertySource?.ShouldInitialize != true)
        {
            return;
        }

        var propertyMetadata = propertySource.GetPropertyMetadata();

        foreach (var metadata in propertyMetadata)
        {
            try
            {
                var dataSource = metadata.CreateDataSource();

                var testBuilderContext = new TestBuilderContext
                {
                    TestMetadata = testContext.Metadata.TestDetails.MethodMetadata,
                    DataSourceAttribute = dataSource,
                    Events = testContext.InternalEvents,
                    StateBag = testContext.StateBag.Items
                };

                var dataGenMetadata = new DataGeneratorMetadata
                {
                    TestBuilderContext = new TestBuilderContextAccessor(testBuilderContext),
                    MembersToGenerate = [],
                    TestInformation = testContext.Metadata.TestDetails.MethodMetadata,
                    Type = DataGeneratorType.Property,
                    TestSessionId = TestSessionContext.Current?.Id ?? "registration",
                    TestClassInstance = null,
                    ClassInstanceArguments = testContext.Metadata.TestDetails.TestClassArguments
                };

                var dataRows = dataSource.GetDataRowsAsync(dataGenMetadata);

                await foreach (var dataRowFunc in dataRows)
                {
                    var dataRow = await dataRowFunc();
                    if (dataRow is { Length: > 0 } && dataRow[0] != null)
                    {
                        var data = dataRow[0]!;

                        // Store for later injection (in TestInitializer)
                        testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments[metadata.PropertyName] = data;

                        // Track for disposal
                        _lifecycleManager.IncrementReferenceCount(data);
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve data for property '{metadata.PropertyName}': {ex.Message}", ex);
            }
        }
    }
}
