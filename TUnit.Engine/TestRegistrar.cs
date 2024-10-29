using System.Reflection;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Hooks;

namespace TUnit.Engine;

internal class TestRegistrar(InstanceTracker instanceTracker, AssemblyHookOrchestrator assemblyHookOrchestrator, ClassHookOrchestrator classHookOrchestrator)
{
	internal async Task RegisterInstance(DiscoveredTest discoveredTest, Func<Exception, ValueTask> onFailureToInitialize)
	{
		try
		{
			var testContext = discoveredTest.TestContext;
			
			var testRegisteredEventsObjects = testContext.GetTestRegisteredEventsObjects();

			var classType = testContext.TestDetails.ClassType;
		
			instanceTracker.Register(classType);
		
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