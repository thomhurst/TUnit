using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[RequiresUnreferencedCode("Reflection")]
public abstract record DynamicTest
{
    public abstract string TestId { get; }

    public string? TestName { get; init; }

    internal abstract MethodInfo TestBody { get; }
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                | DynamicallyAccessedMemberTypes.PublicMethods 
                                | DynamicallyAccessedMemberTypes.NonPublicMethods
                                | DynamicallyAccessedMemberTypes.PublicProperties)]
    public abstract Type TestClassType { get; }
    
    public object?[]? TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }
    
    public Dictionary<string, object?>? Properties { get; init; }
    
    public abstract IEnumerable<TestMetadata> BuildTestMetadatas();
    
    internal string TestFilePath { get; init; } = string.Empty;
    internal int TestLineNumber { get; init; } = 0;
    
    internal Exception? Exception { get; set; }

    public Attribute[] Attributes { get; init; } = [];
    
    public Attribute[] GetAttributes()
    {
        return
        [
            ..Attributes,
            ..TestBody.GetCustomAttributes(),
            ..TestClassType.GetCustomAttributes(),
            ..TestClassType.Assembly.GetCustomAttributes()
        ];
    }
    
    public static T Argument<T>() => default!;
    
    protected SourceGeneratedMethodInformation BuildTestMethod(MethodInfo methodInfo)
    {
        return new SourceGeneratedMethodInformation
        {
            Attributes = methodInfo.GetCustomAttributes().ToArray(),
            Class = GenerateClass(),
            Name = TestName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = TestClassType,
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType
        };
    }

    protected SourceGeneratedClassInformation GenerateClass()
    {
        return new SourceGeneratedClassInformation
        {
            Assembly = GenerateAssembly(),
            Attributes = TestClassType.GetCustomAttributes().ToArray(),
            Name = TestClassType.Name,
            Namespace = TestClassType.Namespace,
            Parameters = GetParameters(TestClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
            Properties = Properties?.Select(GenerateProperty).ToArray() ?? [],
            Type = TestClassType
        };
    }

    protected SourceGeneratedAssemblyInformation GenerateAssembly()
    {
        return new SourceGeneratedAssemblyInformation
        {
            Attributes = TestClassType.Assembly.GetCustomAttributes().ToArray(),
            Name = TestClassType.Assembly.GetName().Name ??
                   TestClassType.Assembly.GetName().FullName,
        };
    }

    protected static SourceGeneratedPropertyInformation GenerateProperty(KeyValuePair<string, object?> property)
    {
        return new SourceGeneratedPropertyInformation
        {
            Attributes = [], // TODO?
            Name = property.Key,
#pragma warning disable IL2072
            Type = property.Value?.GetType() ?? typeof(object),
#pragma warning restore IL2072
            IsStatic = false, // TODO?
        };
    }

    protected SourceGeneratedParameterInformation[] GetParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(GenerateParameter).ToArray();
    }

    protected SourceGeneratedParameterInformation GenerateParameter(ParameterInfo parameter)
    {
        return new SourceGeneratedParameterInformation(parameter.ParameterType)
        {
            Attributes = parameter.GetCustomAttributes().ToArray(),
            Name = parameter.Name ?? string.Empty,
        };
    }
}

[RequiresUnreferencedCode("Reflection")]
public record DynamicTest<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                | DynamicallyAccessedMemberTypes.PublicMethods
                                | DynamicallyAccessedMemberTypes.PublicProperties)]
    TClass> : DynamicTest where TClass : class
{
    // ReSharper disable once StaticMemberInGenericType
    // We want a new static instance for each type of test
    private static int _dynamicTestCounter;

    public DynamicTest()
    {
        _dynamicTestCounter = Interlocked.Increment(ref _dynamicTestCounter);
    }
    
    public override string TestId => $"DynamicTest-{typeof(TClass).FullName}-{TestBody.Name}-{_dynamicTestCounter}";

    public required Expression<Action<TClass>> TestMethod { get; init; }
    internal override MethodInfo TestBody => GetMethodInfo(TestMethod);
    
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                | DynamicallyAccessedMemberTypes.PublicMethods 
                                | DynamicallyAccessedMemberTypes.NonPublicMethods
                                | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type TestClassType { get; } = typeof(TClass);
    
    public override IEnumerable<TestMetadata> BuildTestMetadatas()
    {
        var attributes = GetAttributes();
        
        var repeatLimit = attributes.OfType<RepeatAttribute>()
            .FirstOrDefault()
            ?.Times ?? 0;

        for (var i = 0; i < repeatLimit + 1; i++)
        {
            yield return new TestMetadata<TClass>
            {
                TestId = $"{TestId}-{i}",
                TestClassArguments = TestClassArguments ?? [],
                TestMethodArguments = TestMethodArguments,
                CurrentRepeatAttempt = i,
                RepeatLimit = repeatLimit,
                TestMethod = BuildTestMethod(TestBody),
                ResettableClassFactory = new ResettableLazy<TClass>(() => (TClass)InstanceHelper.CreateInstance(
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
                TestClassProperties = Properties?.Select(x => x.Value).ToArray() ?? [],
                TestBuilderContext = new TestBuilderContext(),
                TestFilePath = TestFilePath,
                TestLineNumber = TestLineNumber,
                DynamicAttributes = Attributes,
            };
        }
    }

    private MethodInfo GetMethodInfo(Expression<Action<TClass>> expression)
    {
        if (expression.Body is MethodCallExpression methodCallExpression)
        {
            return methodCallExpression.Method;
        }
        
        throw new InvalidOperationException($"A method call expression was not passed to the TestMethod property. Received: {expression.Body.GetType()}.");
    }

    public record MethodBody
    {
        public Action<TClass>? SynchronousBody { get; init; }
        public Func<TClass, Task>? TaskBody { get; init; }
        
        public static implicit operator MethodBody(Action<TClass> action)
        {
            return new MethodBody
            {
                SynchronousBody = action
            };
        }
        
        public static implicit operator MethodBody(Func<TClass, Task> taskBody)
        {
            return new MethodBody
            {
                TaskBody = taskBody
            };
        }
    }
}