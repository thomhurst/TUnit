using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Engine.Services;

namespace TUnit.Engine;

internal class TestRegistrar(InstanceTracker instanceTracker, ObjectLifetimeManager objectLifetimeManager)
{
	internal async ValueTask RegisterInstance(DiscoveredTest discoveredTest, Func<Exception, ValueTask> onFailureToInitialize)
	{
		try
		{
			var testContext = discoveredTest.TestContext;

			testContext.IsRegistered = true;

			var testRegisteredEventsObjects = testContext.GetTestRegisteredEventsObjects();

			var classType = testContext.TestDetails.TestClass.Type;

			RegisterTestContext(classType);
            RegisterObjects(testContext);

			foreach (var testRegisteredEventsObject in testRegisteredEventsObjects)
			{
				await testRegisteredEventsObject.OnTestRegistered(new TestRegisteredContext(discoveredTest));
			}
		}
		catch (Exception e)
		{
			await onFailureToInitialize(e);
		}
	}

    private void RegisterObjects(TestContext testContext)
    {
        foreach (var eventObject in testContext.GetPossibleEventObjects())
        {
            objectLifetimeManager.RegisterObject(eventObject);

            testContext.Events.OnInitialize += (_, _) => ObjectInitializer.InitializeAsync(eventObject); ;
            testContext.Events.OnDispose += (_, _) => objectLifetimeManager.UnregisterObject(eventObject);
            testContext.Events.OnTestSkipped += (_, _) => objectLifetimeManager.UnregisterObject(eventObject);

            if (eventObject is ITestRegisteredEventReceiver testRegisteredEventReceiver)
            {
                testContext.Events.OnTestRegistered += (_, context) => testRegisteredEventReceiver.OnTestRegistered(context);
            }

            if (eventObject is ITestStartEventReceiver testStartEventReceiver)
            {
                testContext.Events.OnTestStart += (_, context) => testStartEventReceiver.OnTestStart(context);
            }

            if (eventObject is ITestEndEventReceiver testEndEventReceiver)
            {
                testContext.Events.OnTestEnd += (_, context) => testEndEventReceiver.OnTestEnd(context);
            }

            if (eventObject is ITestRetryEventReceiver testRetryEventReceiver)
            {
                testContext.Events.OnTestRetry += (_, context) => testRetryEventReceiver.OnTestRetry(context.Item1, context.RetryAttempt);
            }

            if (eventObject is ITestSkippedEventReceiver testSkippedEventReceiver)
            {
                testContext.Events.OnTestSkipped += (_, context) => testSkippedEventReceiver.OnTestSkipped(context);
            }

            if (eventObject is IFirstTestInTestSessionEventReceiver firstTestInTestSessionEventReceiver)
            {
                testContext.Events.OnFirstTestInTestSession += (_, context) => firstTestInTestSessionEventReceiver.OnFirstTestInTestSession(context.Item1, context.Item2);
            }

            if (eventObject is IFirstTestInAssemblyEventReceiver firstTestInAssemblyEventReceiver)
            {
                testContext.Events.OnFirstTestInAssembly += (_, context) => firstTestInAssemblyEventReceiver.OnFirstTestInAssembly(context.Item1, context.Item2);
            }

            if (eventObject is IFirstTestInClassEventReceiver firstTestInClassEventReceiver)
            {
                testContext.Events.OnFirstTestInClass += (_, context) => firstTestInClassEventReceiver.OnFirstTestInClass(context.Item1, context.Item2);
            }

            if (eventObject is ILastTestInClassEventReceiver lastTestInClassEventReceiver)
            {
                testContext.Events.OnLastTestInClass += (_, context) => lastTestInClassEventReceiver.OnLastTestInClass(context.Item1, context.Item2);
            }

            if (eventObject is ILastTestInAssemblyEventReceiver lastTestInAssemblyEventReceiver)
            {
                testContext.Events.OnLastTestInAssembly += (_, context) => lastTestInAssemblyEventReceiver.OnLastTestInAssembly(context.Item1, context.Item2);
            }

            if (eventObject is ILastTestInTestSessionEventReceiver lastTestInTestSessionEventReceiver)
            {
                testContext.Events.OnLastTestInTestSession += (_, context) => lastTestInTestSessionEventReceiver.OnLastTestInTestSession(context.Item1, context.Item2);
            }
        }
    }

    private void RegisterTestContext(Type type)
	{
		instanceTracker.Register(type);
	}
}
