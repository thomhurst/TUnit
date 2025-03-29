using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

public abstract record DynamicTest
{
    public abstract string TestId { get; }

    public string? TestName { get; init; }

    public required MethodInfo TestBody { get; init; }
    
    public abstract Type TestClassType { get; }
    
    public required object?[] TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    
    public required Dictionary<string, object?> Properties { get; init; }
    
    public abstract IEnumerable<TestMetadata> BuildTestMetadatas();

    [field: AllowNull, MaybeNull]
    public Attribute[] Attributes => field ??=
        [..TestBody.GetCustomAttributes(), ..TestClassType.GetCustomAttributes(), ..TestClassType.Assembly.GetCustomAttributes()];
}

public record DynamicTest<TClass> : DynamicTest where TClass : class
{
    // ReSharper disable once StaticMemberInGenericType
    // We want a new static instance for each type of test
    private static int _dynamicTestCounter;

    public DynamicTest()
    {
        _dynamicTestCounter = Interlocked.Increment(ref _dynamicTestCounter);
    }

    public override string TestId => $"DynamicTest-{typeof(TClass).FullName}-{TestBody.Name}-{_dynamicTestCounter}";
    
    public override Type TestClassType { get; } = typeof(TClass);
    
    public override IEnumerable<TestMetadata<TClass>> BuildTestMetadatas()
    {
        var repeatLimit = Attributes.OfType<RepeatAttribute>()
            .FirstOrDefault()
            ?.Times ?? 0;

        for (var i = 0; i < repeatLimit + 1; i++)
        {
            yield return new TestMetadata<TClass>
            {
                TestId = TestId,
                TestClassArguments = TestClassArguments,
                TestMethodArguments = TestMethodArguments,
                CurrentRepeatAttempt = 0,
                RepeatLimit = repeatLimit,
                TestMethod = BuildTestMethod(TestBody),
                ResettableClassFactory = new ResettableLazy<TClass>(() => (TClass)Activator.CreateInstance(
                        typeof(TClass),
                        TestClassArguments)!,
                    TestSessionContext.Current?.Id ?? "Unknown",
                    new TestBuilderContext()),
                TestMethodFactory = (@class, token) =>
                {
                    var arguments = TestMethodArguments;

                    if (TestBody.GetParameters().LastOrDefault()?.ParameterType == typeof(CancellationToken))
                    {
                        arguments = TestMethodArguments.Append(token).ToArray();
                    }

                    return AsyncConvert.ConvertObject(TestBody.Invoke(@class, arguments));
                },
                TestClassProperties = Properties.Select(x => x.Value).ToArray(),
                TestBuilderContext = new TestBuilderContext(),
                TestFilePath = "", // TODO
                TestLineNumber = 0, // TODO
            };
        }
    }

    private SourceGeneratedMethodInformation BuildTestMethod(MethodInfo methodInfo)
    {
        return new SourceGeneratedMethodInformation
        {
            Attributes = methodInfo.GetCustomAttributes().ToArray(),
            Class = GenerateClass(),
            Name = TestName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = methodInfo.DeclaringType ?? typeof(TClass),
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType
        };
    }

    private SourceGeneratedClassInformation GenerateClass()
    {
        return new SourceGeneratedClassInformation
        {
            Assembly = GenerateAssembly(),
            Attributes = TestClassType.GetCustomAttributes().ToArray(),
            Name = TestClassType.Name,
            Namespace = TestClassType.Namespace,
            Parameters = GetParameters(TestClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
            Properties = Properties.Select(GenerateProperty).ToArray(),
            Type = TestClassType
        };
    }

    private SourceGeneratedAssemblyInformation GenerateAssembly()
    {
        return new SourceGeneratedAssemblyInformation
        {
            Attributes = TestClassType.Assembly.GetCustomAttributes().ToArray(),
            Name = TestClassType.Assembly.GetName().Name ??
                   TestClassType.Assembly.GetName().FullName,
        };
    }

    private static SourceGeneratedPropertyInformation GenerateProperty(KeyValuePair<string, object?> property)
    {
        return new SourceGeneratedPropertyInformation
        {
            Attributes = [], // TODO?
            Name = property.Key,
            Type = property.Value?.GetType() ?? typeof(object),
            IsStatic = false, // TODO?
        };
    }

    private SourceGeneratedParameterInformation[] GetParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(GenerateParameter).ToArray();
    }

    private SourceGeneratedParameterInformation GenerateParameter(ParameterInfo parameter)
    {
        return new SourceGeneratedParameterInformation(parameter.ParameterType)
        {
            Attributes = parameter.GetCustomAttributes().ToArray(),
            Name = parameter.Name ?? string.Empty,
        };
    }
}