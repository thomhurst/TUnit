using System.Reflection;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Hooks;

namespace TUnit.Engine;

internal class TestRegistrar(InstanceTracker instanceTracker, AssemblyHookOrchestrator assemblyHookOrchestrator, ClassHookOrchestrator classHookOrchestrator)
{
	internal async ValueTask RegisterInstance(DiscoveredTest discoveredTest, Func<Exception, ValueTask> onFailureToInitialize)
	{
		try
		{
			var testContext = discoveredTest.TestContext;
			
			testContext.IsRegistered = true;
			
			var testRegisteredEventsObjects = testContext.GetTestRegisteredEventsObjects();

			var classType = testContext.TestDetails.TestClass.Type;
			
			RegisterTestContext(classType, testContext);

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

	private void RegisterTestContext(Type type, TestContext testContext)
	{
		instanceTracker.Register(type);

		var classHookContext = classHookOrchestrator.GetContext(type);

		classHookContext.Tests.Add(testContext);
        
		RegisterTestContext(type.Assembly, classHookContext);
	}
	
	private void RegisterTestContext(Assembly assembly, ClassHookContext classHookContext)
	{
		var assemblyHookContext = assemblyHookOrchestrator.GetContext(assembly);

		assemblyHookContext.TestClasses.Add(classHookContext);
	}
}