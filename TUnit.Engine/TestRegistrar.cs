﻿using System.Reflection;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Engine.Hooks;

namespace TUnit.Engine;

internal class TestRegistrar(AssemblyHookOrchestrator assemblyHookOrchestrator, ClassHookOrchestrator classHookOrchestrator)
{
	internal async Task RegisterInstance(TestContext testContext, Func<Exception, Task> onFailureToInitialize)
	{
		try
		{
			var testRegisteredEventsObjects = testContext.GetTestRegisteredEventsObjects();

			var classType = testContext.TestDetails.ClassType;
		
			InstanceTracker.Register(classType);
		
			RegisterTestContext(classType, testContext);

			foreach (var testRegisteredEventsObject in testRegisteredEventsObjects)
			{
				await testRegisteredEventsObject.OnTestRegistered(testContext);
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