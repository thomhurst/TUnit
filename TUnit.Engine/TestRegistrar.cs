using System.Reflection;
using TUnit.Core;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Data;
using TUnit.Engine.Hooks;

namespace TUnit.Engine;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
public static class TestRegistrar
{
	private const int DefaultOrder = int.MaxValue / 2;

	public static void RegisterTest<TClassType>(TestMetadata<TClassType> testMetadata)
	{
		var testId = testMetadata.TestId;
		var methodInfo = testMetadata.MethodInfo;
		var classType = typeof(TClassType);

		var methodAttributes = methodInfo.GetCustomAttributes().ToArray();
		var typeAttributes = AttributeCache.Types.GetOrAdd(classType, _ => testMetadata.AttributeTypes.SelectMany(x => classType.GetCustomAttributes(x)).ToArray());
		var assemblyAttributes = AttributeCache.Assemblies.GetOrAdd(classType.Assembly, _ => testMetadata.AttributeTypes.SelectMany(x => classType.Assembly.GetCustomAttributes(x)).ToArray());
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
			ParallelLimit = testMetadata.ParallelLimit
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
	
	public static void RegisterInstance(TestContext testContext)
	{
		var classType = testContext.TestDetails.ClassType;
		
		InstanceTracker.Register(classType);
		
		ClassHookOrchestrator.RegisterTestContext(classType, testContext);
		
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
}

public record TestMetadata<TClassType>
{
    public required string TestId { get; init; }
    public required string DisplayName { get; init; }
    public required MethodInfo MethodInfo { get; init; }
    
    public required int RepeatLimit { get; init; }
    public required int CurrentRepeatAttempt { get; init; }
    
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }


    public required ResettableLazy<TClassType> ResettableClassFactory { get; init; }
    public required Func<TClassType, CancellationToken, Task> TestMethodFactory { get; init; }
    
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    
    public required TestData[] InternalTestClassArguments { internal get; init; }

    public required TestData[] InternalTestMethodArguments { internal get; init; }
    
    public required ITestExecutor TestExecutor { get; init; }
    
    public required IParallelLimit? ParallelLimit { get; init; }
    
    // Need to be referenced statically for AOT
    public required Type[] AttributeTypes { get; init; }
}