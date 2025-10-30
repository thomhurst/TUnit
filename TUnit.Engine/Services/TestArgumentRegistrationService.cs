using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Data;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Core.Interfaces.SourceGenerator;
using TUnit.Core.PropertyInjection;
using TUnit.Core.Tracking;

namespace TUnit.Engine.Services;

/// <summary>
/// Service that handles registration of test arguments (constructor args, method args) during test discovery.
/// Implements ITestRegisteredEventReceiver to register objects when tests are registered.
/// Renamed from TestArgumentTrackingService to clarify it's for the registration phase.
/// </summary>
internal sealed class TestArgumentRegistrationService : ITestRegisteredEventReceiver
{
    private readonly ObjectRegistrationService _objectRegistrationService;
    private readonly ObjectTracker _objectTracker;

    public TestArgumentRegistrationService(ObjectRegistrationService objectRegistrationService, ObjectTracker objectTracker)
    {
        _objectRegistrationService = objectRegistrationService;
        _objectTracker = objectTracker;
    }

    public int Order => int.MinValue; // Run first to ensure registration happens before other event receivers

    /// <summary>
    /// Called when a test is registered. This is the correct time to register constructor and method arguments
    /// for proper reference counting and disposal tracking.
    /// </summary>
    public async ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        var testContext = context.TestContext;
        var classArguments = testContext.Metadata.TestDetails.TestClassArguments;
        var methodArguments = testContext.Metadata.TestDetails.TestMethodArguments;

        // Register class arguments (registration phase - property injection + tracking, NO IAsyncInitializer)
        await _objectRegistrationService.RegisterArgumentsAsync(
            classArguments,
            testContext.StateBag.Items,
            testContext.Metadata.TestDetails.MethodMetadata,
            testContext.Events);

        // Register method arguments (registration phase)
        await _objectRegistrationService.RegisterArgumentsAsync(
            methodArguments,
            testContext.StateBag.Items,
            testContext.Metadata.TestDetails.MethodMetadata,
            testContext.Events);

        // Register properties that will be injected into the test class
        await RegisterPropertiesAsync(testContext);

        _objectTracker.TrackObjects(testContext);
    }

    /// <summary>
    /// Registers properties that will be injected into the test class instance.
    /// This ensures proper reference counting for all property-injected instances during discovery.
    /// Exceptions during data generation will be caught and associated with the test for reporting.
    /// </summary>
    private async ValueTask RegisterPropertiesAsync(TestContext testContext)
    {
        try
        {
            var classType = testContext.Metadata.TestDetails.ClassType;

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
                try
                {
                    // Create the data source for this property
                    var dataSource = metadata.CreateDataSource();

                    // Create minimal DataGeneratorMetadata for property resolution during registration
                    var testBuilderContext = new TestBuilderContext
                    {
                        TestMetadata = testContext.Metadata.TestDetails.MethodMetadata,
                        DataSourceAttribute = dataSource,
                        Events = testContext.Events,
                        ObjectBag = testContext.StateBag.Items
                    };

                    var dataGenMetadata = new DataGeneratorMetadata
                    {
                        TestBuilderContext = new TestBuilderContextAccessor(testBuilderContext),
                        MembersToGenerate = [], // Properties don't use member generation
                        TestInformation = testContext.Metadata.TestDetails.MethodMetadata,
                        Type = DataGeneratorType.Property,
                        TestSessionId = TestSessionContext.Current?.Id ?? "registration",
                        TestClassInstance = null, // Not available during registration
                        ClassInstanceArguments = testContext.Metadata.TestDetails.TestClassArguments
                    };

                    // Get the data rows from the data source
                    var dataRows = dataSource.GetDataRowsAsync(dataGenMetadata);

                    // Get the first data row (properties get single values, not multiple)
                    await foreach (var dataRowFunc in dataRows)
                    {
                        var dataRow = await dataRowFunc();
                        if (dataRow is { Length: > 0 })
                        {
                            var data = dataRow[0];

                            if (data != null)
                            {
                                // Store for later injection
                                testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments[metadata.PropertyName] = data;

                                // Register the ClassDataSource instance during registration phase
                                // This does: property injection + tracking (NO IAsyncInitializer - deferred to execution)
                                await _objectRegistrationService.RegisterObjectAsync(
                                    data,
                                    testContext.StateBag.Items,
                                    testContext.Metadata.TestDetails.MethodMetadata,
                                    testContext.Events);
                            }
                        }
                        break; // Only take the first result for property injection
                    }
                }
                catch (Exception ex)
                {
                    // Capture the exception for this property and re-throw
                    // The test building process will handle marking it as failed
                    var exceptionMessage = $"Failed to generate data for property '{metadata.PropertyName}': {ex.Message}";
                    var propertyException = new InvalidOperationException(exceptionMessage, ex);
                    throw propertyException;
                }
            }
        }
        catch (Exception ex)
        {
            // Capture any top-level exceptions (e.g., getting property source) and re-throw
            // The test building process will handle marking it as failed
            var exceptionMessage = $"Failed to register properties for test '{testContext.Metadata.TestDetails.TestName}': {ex.Message}";
            var registrationException = new InvalidOperationException(exceptionMessage, ex);
            throw registrationException;
        }
    }
}
