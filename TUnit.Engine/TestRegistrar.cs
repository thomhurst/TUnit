using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Data;
using TUnit.Engine.Hooks;
using TUnit.Engine.Services;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestRegistrar
{
	private const int DefaultOrder = int.MaxValue / 2;

	public static void RegisterTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TClassType>(TestMetadata<TClassType> testMetadata)
	{
		var testId = testMetadata.TestId;
		var methodInfo = testMetadata.MethodInfo;
		var classType = typeof(TClassType);

		var methodAttributes = testMetadata.AttributeTypes.SelectMany(x => methodInfo.GetCustomAttributes(x, false)).Distinct().OfType<Attribute>().ToArray();
		var typeAttributes = AttributeCache.Types.GetOrAdd(classType, _ => testMetadata.AttributeTypes.SelectMany(x => classType.GetCustomAttributes(x, false)).Distinct().OfType<Attribute>().ToArray());
		var assemblyAttributes = AttributeCache.Assemblies.GetOrAdd(classType.Assembly, _ => testMetadata.AttributeTypes.SelectMany(x => classType.Assembly.GetCustomAttributes(x, false)).Distinct().OfType<Attribute>().ToArray());
		Attribute[] attributes = [..methodAttributes, ..typeAttributes, ..assemblyAttributes];
		
		var testDetails = new TestDetails<TClassType>
		{
			TestId = testId,
			Categories = attributes.OfType<CategoryAttribute>().Select(x => x.Category).ToArray(),
			LazyClassInstance = testMetadata.ResettableClassFactory!,
			ClassType = classType,
			Timeout = AttributeHelper.GetAttribute<TimeoutAttribute>(attributes)?.Timeout,
			AssemblyAttributes = assemblyAttributes,
			ClassAttributes = typeAttributes,
			TestAttributes = methodAttributes,
			Attributes = attributes,
			TestClassArguments = testMetadata.TestClassArguments,
			TestMethodArguments = testMetadata.TestMethodArguments,
			InternalTestClassArguments = testMetadata.InternalTestClassArguments,
			InternalTestMethodArguments = testMetadata.InternalTestMethodArguments,
			TestClassParameterTypes = classType.GetConstructors().FirstOrDefault()?.GetParameters().Select(x => x.ParameterType).ToArray() ?? [],
			TestMethodParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray(),
			NotInParallelConstraintKeys = AttributeHelper.GetAttribute<NotInParallelAttribute>(attributes)?.ConstraintKeys,
			CurrentRepeatAttempt = testMetadata.CurrentRepeatAttempt,
			RepeatLimit = testMetadata.RepeatLimit,
			RetryLimit = AttributeHelper.GetAttribute<RetryAttribute>(attributes)?.Times ?? 0,
			MethodInfo = methodInfo,
			TestName = methodInfo.Name,
			DisplayName = testMetadata.DisplayName,
			InternalCustomProperties = attributes.OfType<PropertyAttribute>().ToDictionary(x => x.Name, x => x.Value),
			ReturnType = methodInfo.ReturnType,
			Order = AttributeHelper.GetAttribute<NotInParallelAttribute>(attributes)?.Order ?? DefaultOrder,
			TestFilePath = testMetadata.TestFilePath,
			TestLineNumber = testMetadata.TestLineNumber,
			ParallelLimit = testMetadata.ParallelLimit,
		};

		var testContext = new TestContext(testDetails);

		RunOnTestDiscoveryAttributes(attributes, testContext);
		
		var unInvokedTest = new DiscoveredTest<TClassType>(testMetadata.ResettableClassFactory)
		{
			TestContext = testContext,
			BeforeTestAttributes = attributes.OfType<IBeforeTestAttribute>().ToArray(),
			AfterTestAttributes = attributes.OfType<IAfterTestAttribute>().ToArray(),
			TestBody = (classInstance, cancellationToken) => testMetadata.TestMethodFactory(classInstance, cancellationToken),
			TestExecutor = testMetadata.TestExecutor,
			ClassConstructor = testMetadata.ClassConstructor
		};

		testContext.InternalDiscoveredTest = unInvokedTest;

		TestDictionary.AddTest(testId, unInvokedTest);
	}

	private static void RunOnTestDiscoveryAttributes(IEnumerable<Attribute> attributes, TestContext testContext)
	{
		DiscoveredTestContext? discoveredTestContext = null;
		foreach (var onTestDiscoveryAttribute in attributes.OfType<IOnTestDiscoveryAttribute>().Reverse()) // Reverse to run assembly, then class, then method
		{
			onTestDiscoveryAttribute.OnTestDiscovery(discoveredTestContext ??= new DiscoveredTestContext(testContext));
		}

		if (discoveredTestContext is null)
		{
			return;
		}
		
		foreach (var (key, value) in discoveredTestContext.Properties ?? [])
		{
			testContext.TestDetails.InternalCustomProperties.Add(key, value);
		}
	}

	public static void Failed(string testId, FailedInitializationTest failedInitializationTest)
	{
		TestDictionary.RegisterFailedTest(testId, failedInitializationTest);
	}
	
	internal static void RegisterInstance(TestContext testContext)
	{
		var classType = testContext.TestDetails.ClassType;
		
		InstanceTracker.Register(classType);
		
		RegisterTestContext(classType, testContext);
		
		var testInformation = testContext.TestDetails;
        
		foreach (var argument in testInformation.InternalTestClassArguments)
		{
			if (argument.InjectedDataType == InjectedDataType.SharedByKey)
			{
				TestDataContainer.IncrementKeyUsage(argument.StringKey!, argument.Type);
			}
            
			if (argument.InjectedDataType == InjectedDataType.SharedGlobally)
			{
				TestDataContainer.IncrementGlobalUsage(argument.Type);
			}
		}
        
		foreach (var argument in testInformation.InternalTestMethodArguments)
		{
			if (argument.InjectedDataType == InjectedDataType.SharedByKey)
			{
				TestDataContainer.IncrementKeyUsage(argument.StringKey!, argument.Type);
			}
            
			if (argument.InjectedDataType == InjectedDataType.SharedGlobally)
			{
				TestDataContainer.IncrementGlobalUsage(argument.Type);
			}
		}
	}
	
	public static void RegisterBeforeHook(Assembly assembly, StaticHookMethod<AssemblyHookContext> staticMethod)
	{
		var setups = TestDictionary.AssemblySetUps.GetOrAdd(assembly, _ => []);
		setups.Add((staticMethod.Name, staticMethod, new LazyHook<ExecuteRequestContext, HookMessagePublisher>(async (executeRequestContext, hookPublisher) =>
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
	
	private static void RegisterTestContext(Assembly assembly, ClassHookContext classHookContext)
	{
		var assemblyHookContext = TestDictionary.AssemblyHookContexts.GetOrAdd(assembly, _ => new AssemblyHookContext
		{
			Assembly = assembly
		});

		assemblyHookContext.TestClasses.Add(classHookContext);
	}
	
	private static void RegisterTestContext(Type type, TestContext testContext)
	{
		var classHookContext = TestDictionary.ClassHookContexts.GetOrAdd(type, _ => new ClassHookContext
		{
			ClassType = type
		});

		classHookContext.Tests.Add(testContext);
        
		RegisterTestContext(type.Assembly, classHookContext);
	}
}