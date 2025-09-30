using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.DataSources;
using TUnit.Core.Enums;
using TUnit.Core.Initialization;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.PropertyInjection;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Service that handles tracking of test arguments (constructor args, method args) for disposal.
/// Implements ITestRegisteredEventReceiver to track objects when tests are registered.
/// </summary>
internal sealed class TestArgumentTrackingService : ITestRegisteredEventReceiver
{
    private readonly TestObjectInitializer _testObjectInitializer;

    public TestArgumentTrackingService(TestObjectInitializer testObjectInitializer)
    {
        _testObjectInitializer = testObjectInitializer;
    }

    public int Order => int.MinValue; // Run first to ensure tracking happens before other event receivers

    /// <summary>
    /// Called when a test is registered. This is the correct time to track constructor and method arguments
    /// for shared instances, as per the ObjectTracker reference counting approach.
    /// </summary>
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var testContext = context.TestContext;
        var classArguments = testContext.TestDetails.TestClassArguments;
        var methodArguments = testContext.TestDetails.TestMethodArguments;
        
        // Initialize class arguments (registration phase)
        await _testObjectInitializer.InitializeArgumentsAsync(
            classArguments,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata,
            testContext.Events,
            isRegistrationPhase: true);

        // Initialize method arguments (registration phase)
        await _testObjectInitializer.InitializeArgumentsAsync(
            methodArguments,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata,
            testContext.Events,
            isRegistrationPhase: true);
        
        // Track all constructor and method arguments
        // Note: TestObjectInitializer already handles tracking, but we ensure it here for clarity
        var allArguments = classArguments.Concat(methodArguments);
        
        foreach (var obj in allArguments)
        {
            if (obj != null)
            {
                // Track each argument - for shared instances, this increments the reference count
                // When the test ends, the count will be decremented via the test's Events.OnTestFinalized
                ObjectTracker.TrackObject(testContext.Events, obj);
            }
        }

        // Track properties that will be injected into the test class
        await TrackPropertiesAsync(testContext);
    }

    /// <summary>
    /// Tracks properties that will be injected into the test class instance.
    /// This ensures proper reference counting for all property-injected instances.
    /// </summary>
    private async ValueTask TrackPropertiesAsync(TestContext testContext)
    {
        // Track properties during registration for proper reference counting
        // This is critical for SharedType.PerTestSession properties to have the right count

        var classType = testContext.TestDetails.ClassType;
        if (classType == null)
        {
            return;
        }

        // Get the property source for the class
        var propertySource = PropertySourceRegistry.GetSource(classType);
        if (propertySource?.ShouldInitialize != true)
        {
            // No properties to inject for this class
            return;
        }

        // Get all properties that need injection
        var propertyMetadata = propertySource.GetPropertyMetadata();

        foreach (var metadata in propertyMetadata)
        {
            // Create the data source for this property
            var dataSource = metadata.CreateDataSource();

            // Check if this is a ClassDataSource (generic or untyped)
            var dataSourceType = dataSource.GetType();

            // Skip MethodDataSource and other dynamic sources - they'll be handled during execution
            if (dataSourceType.Name.Contains("MethodDataSource"))
            {
                continue;
            }

            // For ClassDataSource properties, we need to resolve and track them
            if (dataSourceType.Name.Contains("ClassDataSource"))
            {
                try
                {
                    // Create minimal DataGeneratorMetadata for property resolution during registration
                    var testBuilderContext = new TestBuilderContext
                    {
                        TestMetadata = testContext.TestDetails.MethodMetadata,
                        DataSourceAttribute = dataSource,
                        Events = testContext.Events,
                        ObjectBag = testContext.ObjectBag
                    };

                    var dataGenMetadata = new DataGeneratorMetadata
                    {
                        TestBuilderContext = new TestBuilderContextAccessor(testBuilderContext),
                        MembersToGenerate = [], // Properties don't use member generation
                        TestInformation = testContext.TestDetails.MethodMetadata,
                        Type = DataGeneratorType.Property,
                        TestSessionId = TestSessionContext.Current?.Id ?? "registration",
                        TestClassInstance = null, // Not available during registration
                        ClassInstanceArguments = testContext.TestDetails.TestClassArguments
                    };

                    // Get the data rows from the data source
                    var dataRows = dataSource.GetDataRowsAsync(dataGenMetadata);

                    // Get the first data row (properties get single values, not multiple)
                    await foreach (var dataRowFunc in dataRows)
                    {
                        var dataRow = await dataRowFunc();
                        if (dataRow != null && dataRow.Length > 0)
                        {
                            var data = dataRow[0];

                            if (data != null)
                            {
                                // Store for later injection (before initialization)
                                testContext.TestDetails.TestClassInjectedPropertyArguments[metadata.PropertyName] = data;

                                // Track the object - this increments the reference count
                                ObjectTracker.TrackObject(testContext.Events, data);

                                // Initialize the ClassDataSource instance (including property injection and IAsyncInitializer)
                                // This must be done AFTER storing to avoid recursive lookups failing
                                await _testObjectInitializer.InitializeAsync(data, testContext);
                            }
                        }
                        break; // Only take the first result for property injection
                    }
                }
                catch
                {
                    // If we can't resolve during registration, it will be resolved during execution
                    // This is OK for dynamic data sources
                }
            }
        }
    }
}