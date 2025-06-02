using TUnit.Core;
using TUnit.Core.Extensions;

namespace TUnit.Engine;

internal class TestRegistrar(InstanceTracker instanceTracker)
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

	private void RegisterTestContext(Type type)
	{
		instanceTracker.Register(type);
	}
}