using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Data;
using TUnit.Engine.Helpers;
using TUnit.Engine.Hooks;

namespace TUnit.Engine;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
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
		var typeAttributes = AttributeCache.Types.GetOrAdd(classType, _ => classType.GetCustomAttributes().ToArray());
		var assemblyAttributes = AttributeCache.Assemblies.GetOrAdd(classType.Assembly,
			_ => classType.Assembly.GetCustomAttributes().ToArray());
		Attribute[] attributes = [..methodAttributes, ..typeAttributes, ..assemblyAttributes];
		
		var testInformation = new TestInformation<TClassType>
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
			CustomProperties = attributes.OfType<PropertyAttribute>().ToDictionary(x => x.Name, x => x.Value),
			ReturnType = methodInfo.ReturnType,
			Order = AttributeHelper.GetAttribute<NotInParallelAttribute>(attributes)?.Order ?? DefaultOrder,
			TestFilePath = testMetadata.TestFilePath,
			TestLineNumber = testMetadata.TestLineNumber,
		};

		var testContext = new TestContext(testInformation);

		ClassHookOrchestrator.RegisterTestContext(classType, testContext);

		var unInvokedTest = new UnInvokedTest<TClassType>(testMetadata.ResettableClassFactory)
		{
			Id = testId,
			TestContext = testContext,
			BeforeTestAttributes = attributes.OfType<IBeforeTestAttribute>().ToArray(),
			AfterTestAttributes = attributes.OfType<IAfterTestAttribute>().ToArray(),
			BeforeEachTestSetUps = testMetadata.BeforeEachTestSetUps,
			TestBody = (classInstance, cancellationToken) => RunHelpers.RunAsync(() => testMetadata.TestMethodFactory(classInstance, cancellationToken)),
			AfterEachTestCleanUps = testMetadata.AfterEachTestCleanUps,
		};

		TestDictionary.AddTest(testId, unInvokedTest);
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
    
    public required List<Func<TClassType, Task>> BeforeEachTestSetUps { get; init; }
    public required List<Func<TClassType, Task>> AfterEachTestCleanUps { get; init; }

    
    public required TestData[] InternalTestClassArguments { internal get; init; }

    public required TestData[] InternalTestMethodArguments { internal get; init; }
}