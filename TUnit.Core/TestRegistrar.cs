﻿using System.Diagnostics;
using System.Reflection;
using TUnit.Core.Exceptions;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
[StackTraceHidden]
public static class TestRegistrar
{
	public static void RegisterTest(TestMetadata testMetadata)
	{
		var testDetails = testMetadata.BuildTestDetails();

		var testContext = new TestContext(testDetails, testMetadata.ObjectBag);
		
		RunOnTestDiscoveryAttributeHooks([..testDetails.DataAttributes, ..testDetails.Attributes], testContext);

		var unInvokedTest = testMetadata.BuildDiscoveredTest(testContext);

		testContext.InternalDiscoveredTest = unInvokedTest;

		TestDictionary.AddTest(testDetails.TestId, unInvokedTest);
	}

	private static void RunOnTestDiscoveryAttributeHooks(IEnumerable<Attribute> attributes, TestContext testContext)
	{
		DiscoveredTestContext? discoveredTestContext = null;
		foreach (var onTestDiscoveryAttribute in attributes.OfType<ITestDiscoveryEvent>().Reverse()) // Reverse to run assembly, then class, then method
		{
			onTestDiscoveryAttribute.OnTestDiscovery(discoveredTestContext ??= new DiscoveredTestContext(testContext));
		}
	}

	public static void Failed(string testId, FailedInitializationTest failedInitializationTest)
	{
		TestDictionary.RegisterFailedTest(testId, failedInitializationTest);
	}
	
	internal static async Task RegisterInstance(TestContext testContext, Func<Exception, Task> onFailureToInitialize)
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
	
	public static void RegisterBeforeHook<TClassType>(InstanceHookMethod<TClassType> instanceMethod)
	{
		var taskFunctions = TestDictionary.TestSetUps.GetOrAdd(typeof(TClassType), _ => []);

		taskFunctions.Add((instanceMethod.Name, instanceMethod.Order, async (classInstance, discoveredTest) =>
		{
			var timeout = instanceMethod.Timeout;

			try
			{
				await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(instanceMethod, discoveredTest).ExecuteBeforeTestHook(instanceMethod.MethodInfo, discoveredTest.TestContext, () => instanceMethod.Body((TClassType) classInstance, discoveredTest.TestContext, token)), timeout);
			}
			catch (Exception e)
			{
				throw new BeforeTestException($"Error executing Before(Test) method: {instanceMethod.Name}", e);
			}
		}));
	}
    
	public static void RegisterAfterHook<TClassType>(InstanceHookMethod<TClassType> instanceMethod)
	{
		var taskFunctions = TestDictionary.TestCleanUps.GetOrAdd(typeof(TClassType), _ => []);

		taskFunctions.Add((instanceMethod.Name, instanceMethod.Order, async (classInstance, discoveredTest) =>
		{
			var timeout = instanceMethod.Timeout;

			try
			{
				await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(instanceMethod, discoveredTest).ExecuteAfterTestHook(instanceMethod.MethodInfo, discoveredTest.TestContext, () => instanceMethod.Body((TClassType) classInstance, discoveredTest.TestContext, token)), timeout);
			}
			catch (Exception e)
			{
				throw new AfterTestException($"Error executing After(Test) method: {instanceMethod.Name}", e);
			}
		}));
	}
	
	public static void RegisterBeforeHook(Type type, StaticHookMethod<ClassHookContext> staticMethod)
	{
		var taskFunctions = TestDictionary.ClassSetUps.GetOrAdd(type, _ => []);

		taskFunctions.Add((staticMethod.Name, staticMethod, new LazyHook<string, IHookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
		{
			var context = GetClassHookContext(type);
            
			var timeout = staticMethod.Timeout;
			await hookPublisher.Push(executeRequestContext, $"Before Class: {staticMethod.Name}", staticMethod, () =>
				RunHelpers.RunWithTimeoutAsync(
					token => staticMethod.HookExecutor.ExecuteBeforeClassHook(staticMethod.MethodInfo, context,
						() => staticMethod.Body(context, token)), timeout)
			);
		})));
	}
    
	public static void RegisterAfterHook(Type type, StaticHookMethod<ClassHookContext> staticMethod)
	{
		var taskFunctions = TestDictionary.ClassCleanUps.GetOrAdd(type, _ => []);

		taskFunctions.Add((staticMethod.Name, staticMethod, () =>
		{
			var context = GetClassHookContext(type);
            
			var timeout = staticMethod.Timeout;

			return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterClassHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
		}));
	}
	
	public static void RegisterBeforeHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
	{
		var setups = TestDictionary.AssemblySetUps.GetOrAdd(assembly, _ => []);
		setups.Add((staticMethod.Name, staticMethod, new LazyHook<string, IHookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
		{
			var context = GetAssemblyHookContext(assembly);
            
			var timeout = staticMethod.Timeout;

			await hookPublisher.Push(executeRequestContext, $"Before Assembly: {staticMethod.Name}", staticMethod, () =>
				RunHelpers.RunWithTimeoutAsync(
					token => staticMethod.HookExecutor.ExecuteBeforeAssemblyHook(staticMethod.MethodInfo, context,
						() => staticMethod.Body(context, token)), timeout)
			);
		})));
	}

	public static void RegisterAfterHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
	{
		var taskFunctions = TestDictionary.AssemblyCleanUps.GetOrAdd(assembly, _ => []);

		taskFunctions.Add((staticMethod.Name, staticMethod, () =>
		{
			var context = GetAssemblyHookContext(assembly);
            
			var timeout = staticMethod.Timeout;

			return RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterAssemblyHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
		}));
	}
	
	internal static AssemblyHookContext GetAssemblyHookContext(Assembly assembly)
	{
		lock (assembly)
		{
			return TestDictionary.AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
			{
				Assembly = assembly
			});
		}
	}
	
	public static void RegisterTestContext(Assembly assembly, ClassHookContext classHookContext)
	{
		var assemblyHookContext = TestDictionary.AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
		{
			Assembly = assembly
		});

		assemblyHookContext.TestClasses.Add(classHookContext);
	}
	
	public static void RegisterTestContext(Type type, TestContext testContext)
	{
		var classHookContext = TestDictionary.ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
		{
			ClassType = type
		});

		classHookContext.Tests.Add(testContext);
        
		RegisterTestContext(type.Assembly, classHookContext);
	}
	
	public static void RegisterBeforeHook(StaticHookMethod<TestContext> staticMethod)
	{
		TestDictionary.GlobalTestSetUps.Add((staticMethod.Name, staticMethod, async context =>
		{
			var timeout = staticMethod.Timeout;

			try
			{
				await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(staticMethod, context.InternalDiscoveredTest).ExecuteBeforeTestHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
			}
			catch (Exception e)
			{
				throw new BeforeTestException($"Error executing BeforeEvery(Test) method: {staticMethod.Name}", e);
			}
		}));
	}

	public static void RegisterAfterHook(StaticHookMethod<TestContext> staticMethod)
	{
		TestDictionary.GlobalTestCleanUps.Add((staticMethod.Name, staticMethod, async context =>
		{
			var timeout = staticMethod.Timeout;

			try
			{
				await RunHelpers.RunWithTimeoutAsync(token => HookExecutorProvider.GetHookExecutor(staticMethod, context.InternalDiscoveredTest).ExecuteAfterTestHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
			}
			catch (Exception e)
			{
				throw new AfterTestException($"Error executing AfterEvery(Test) method: {staticMethod.Name}", e);
			}
		}));
	}

	public static void RegisterBeforeHook(StaticHookMethod<ClassHookContext> staticMethod)
    {
        TestDictionary.GlobalClassSetUps.Add((staticMethod.Name, staticMethod, new LazyHook<string, IHookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
        {
            var timeout = staticMethod.Timeout;

            var classHookContext = GetClassHookContext(staticMethod.ClassType);

            try
            {
                ClassHookContext.Current = classHookContext;
                
                await hookPublisher.Push(executeRequestContext, $"Before Class: {staticMethod.Name}",
                    staticMethod, () =>
                        RunHelpers.RunWithTimeoutAsync(
                            token => staticMethod.HookExecutor.ExecuteBeforeClassHook(staticMethod.MethodInfo,
                                classHookContext,
                                () => staticMethod.Body(classHookContext, token)), timeout)
                );
            }
            catch (Exception e)
            {
                throw new BeforeClassException($"Error executing Before(Class) method: {staticMethod.Name}", e);
            }
            finally
            {
                ClassHookContext.Current = null;
            }
        })));
    }

    public static void RegisterAfterHook(StaticHookMethod<ClassHookContext> staticMethod)
    {
        TestDictionary.GlobalClassCleanUps.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                ClassHookContext.Current = context;
                
                await RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteAfterClassHook(staticMethod.MethodInfo, context,
                        () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterClassException($"Error executing After(Class) method: {staticMethod.Name}", e);
            }
            finally
            {
                ClassHookContext.Current = null;
            }
        }));
    }

    public static void RegisterBeforeHook(StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        TestDictionary.GlobalAssemblySetUps.Add((staticMethod.Name, staticMethod, new LazyHook<string, IHookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
        {
            var timeout = staticMethod.Timeout;
            var assemblyHookContext = GetAssemblyHookContext(staticMethod.Assembly);

            try
            {
                AssemblyHookContext.Current = assemblyHookContext;
                
                await hookPublisher.Push(executeRequestContext, $"Before Assembly: {staticMethod.Name}",
                    staticMethod, () =>
                        RunHelpers.RunWithTimeoutAsync(
                            token => staticMethod.HookExecutor.ExecuteBeforeAssemblyHook(staticMethod.MethodInfo,
                                assemblyHookContext,
                                () => staticMethod.Body(assemblyHookContext, token)), timeout)
                );
            }
            catch (Exception e)
            {
                throw new BeforeAssemblyException($"Error executing Before(Assembly) method: {staticMethod.Name}",
                    e);
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        })));
    }

    public static void RegisterAfterHook(StaticHookMethod<AssemblyHookContext> staticMethod)
    {
        TestDictionary.GlobalAssemblyCleanUps.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                AssemblyHookContext.Current = context;

                await RunHelpers.RunWithTimeoutAsync(
                    token => staticMethod.HookExecutor.ExecuteAfterAssemblyHook(staticMethod.MethodInfo, context,
                        () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterAssemblyException($"Error executing After(Assembly) method: {staticMethod.Name}", e);
            }
            finally
            {
                AssemblyHookContext.Current = null;
            }
        }));
    }
    
    public static void RegisterBeforeHook(StaticHookMethod<BeforeTestDiscoveryContext> staticMethod)
    {
	    TestDictionary.BeforeTestDiscovery.Add((staticMethod.Name, staticMethod, async context =>
	    {
		    var timeout = staticMethod.Timeout;

		    try
		    {
			    BeforeTestDiscoveryContext.Current = context;
                
			    await RunHelpers.RunWithTimeoutAsync(
				    token => staticMethod.HookExecutor.ExecuteBeforeTestDiscoveryHook(staticMethod.MethodInfo,
					    () => staticMethod.Body(context, token)), timeout);
		    }
		    catch (Exception e)
		    {
			    throw new BeforeTestDiscoveryException(
				    $"Error executing Before(TestDiscovery) method: {staticMethod.Name}", e);
		    }
		    finally
		    {
			    BeforeTestDiscoveryContext.Current = null;
		    }
	    }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestDiscoveryContext> staticMethod)
    {
	    TestDictionary.AfterTestDiscovery.Add((staticMethod.Name, staticMethod, async context =>
	    {
		    var timeout = staticMethod.Timeout;

		    try
		    {
			    TestDiscoveryContext.Current = context;
                
			    await RunHelpers.RunWithTimeoutAsync(
				    token => staticMethod.HookExecutor.ExecuteAfterTestDiscoveryHook(staticMethod.MethodInfo, context,
					    () => staticMethod.Body(context, token)), timeout);
		    }
		    catch (Exception e)
		    {
			    throw new AfterTestDiscoveryException(
				    $"Error executing After(TestDiscovery) method: {staticMethod.Name}", e);
		    }
		    finally
		    {
			    TestDiscoveryContext.Current = null;
		    }
	    }));
    }
    
	public static void RegisterBeforeHook(StaticHookMethod<TestSessionContext> staticMethod)
    {
        TestDictionary.BeforeTestSession.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteBeforeTestSessionHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new BeforeTestSessionException($"Error executing Before(TestSession) method: {staticMethod.Name}", e);
            }
        }));
    }

    public static void RegisterAfterHook(StaticHookMethod<TestSessionContext> staticMethod)
    {
        TestDictionary.AfterTestSession.Add((staticMethod.Name, staticMethod, async context =>
        {
            var timeout = staticMethod.Timeout;

            try
            {
                await RunHelpers.RunWithTimeoutAsync(token => staticMethod.HookExecutor.ExecuteAfterTestSessionHook(staticMethod.MethodInfo, context, () => staticMethod.Body(context, token)), timeout);
            }
            catch (Exception e)
            {
                throw new AfterTestSessionException($"Error executing After(TestSession) method: {staticMethod.Name}", e);
            }
        }));
    }
    
    internal static ClassHookContext GetClassHookContext(Type type)
    {
	    lock (type)
	    {
		    return TestDictionary.ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
		    {
			    ClassType = type
		    });
	    }
    }
}