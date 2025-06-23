using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[RequiresDynamicCode("Reflection")]
[RequiresUnreferencedCode("Reflection")]
public abstract record DynamicTest
{
    public abstract string TestId { get; }

    public string? TestName { get; init; }

    internal abstract MethodInfo TestBody { get; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
                                | DynamicallyAccessedMemberTypes.PublicMethods
                                | DynamicallyAccessedMemberTypes.PublicProperties
                                | DynamicallyAccessedMemberTypes.PublicProperties)]
    public abstract Type TestClassType { get; }

    public object?[]? TestClassArguments { get; init; }
    public required object?[] TestMethodArguments { get; init; }

    public Dictionary<string, object?>? Properties { get; init; }

    public abstract DiscoveryResult BuildTests();

    internal string TestFilePath { get; init; } = string.Empty;
    internal int TestLineNumber { get; init; } = 0;

    internal Exception? Exception { get; set; }

    public Attribute[] Attributes { get; init; } = [];

    public Attribute[] GetAttributes()
    {
        return
        [
            ..Attributes,
            ..TestBody.GetCustomAttributesSafe(),
            ..TestClassType.GetCustomAttributesSafe(),
            ..TestClassType.Assembly.GetCustomAttributesSafe()
        ];
    }

    public static T Argument<T>() => default!;

    protected MethodMetadata BuildTestMethod(MethodInfo methodInfo)
    {
        return new MethodMetadata
        {
            Attributes = ConvertToAttributeMetadata(methodInfo.GetCustomAttributesSafe().ToArray(), TestAttributeTarget.Method, TestName ?? methodInfo.Name, TestClassType),
            Class = GenerateClass(),
            Name = TestName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = TestClassType,
            TypeReference = TypeReference.CreateConcrete(TestClassType.AssemblyQualifiedName ?? TestClassType.FullName ?? TestClassType.Name),
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType,
            ReturnTypeReference = TypeReference.CreateConcrete(methodInfo.ReturnType.AssemblyQualifiedName ?? methodInfo.ReturnType.FullName ?? methodInfo.ReturnType.Name)
        };
    }

    protected ClassMetadata GenerateClass()
    {
        return new ClassMetadata
        {
            Parent = ReflectionToSourceModelHelpers.GetParent(TestClassType),
            Assembly = GenerateAssembly(),
            Attributes = ConvertToAttributeMetadata(TestClassType.GetCustomAttributesSafe().ToArray(), TestAttributeTarget.Class, TestClassType.Name, TestClassType),
            Name = TestClassType.Name,
            Namespace = TestClassType.Namespace,
            Parameters = GetParameters(TestClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
            Properties = Properties?.Select(GenerateProperty).ToArray() ?? [],
            Type = TestClassType,
            TypeReference = TypeReference.CreateConcrete(TestClassType.AssemblyQualifiedName ?? TestClassType.FullName ?? TestClassType.Name)
        };
    }

    protected AssemblyMetadata GenerateAssembly()
    {
        return new AssemblyMetadata
        {
            Attributes = ConvertToAttributeMetadata(TestClassType.Assembly.GetCustomAttributesSafe().ToArray(), TestAttributeTarget.Assembly, TestClassType.Assembly.GetName().Name),
            Name = TestClassType.Assembly.GetName().Name ??
                   TestClassType.Assembly.GetName().FullName,
        };
    }

    protected static PropertyMetadata GenerateProperty(KeyValuePair<string, object?> property)
    {
        return new PropertyMetadata
        {
            Attributes = [], // TODO?
            ReflectionInfo = null!, // TODO?
            Name = property.Key,
            Type = property.Value?.GetType() ?? typeof(object),
            Getter = _ => property.Value,
            IsStatic = false, // TODO?
        };
    }

    protected ParameterMetadata[] GetParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(GenerateParameter).ToArray();
    }

    protected ParameterMetadata GenerateParameter(ParameterInfo parameter)
    {
        return ReflectionToSourceModelHelpers.GenerateParameter(parameter);
    }

    private static AttributeMetadata[] ConvertToAttributeMetadata(
        Attribute[] attributes,
        TestAttributeTarget targetElement,
        string? targetMemberName = null,
        Type? targetType = null)
    {
        return attributes.Select(attr => new AttributeMetadata
        {
            Instance = attr,
            TargetElement = targetElement,
            TargetMemberName = targetMemberName,
            TargetType = targetType
        }).ToArray();
    }
}

[RequiresDynamicCode("Reflection")]
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
                                | DynamicallyAccessedMemberTypes.PublicProperties
                                | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type TestClassType { get; } = typeof(TClass);

    public override DiscoveryResult BuildTests()
    {
        var testDefinitions = new List<TestDefinition<TClass>>();
        var discoveryFailures = new List<DiscoveryFailure>();

        try
        {
            var attributes = GetAttributes();
            var repeatLimit = attributes.OfType<RepeatAttribute>()
                .FirstOrDefault()
                ?.Times ?? 0;

            var testMethodInformation = BuildTestMethod(TestBody);
            var testBuilderContext = new TestBuilderContext();

            var testDefinition = new TestDefinition<TClass>
            {
                TestId = TestId,
                MethodMetadata = testMethodInformation,
                TestFilePath = TestFilePath,
                TestLineNumber = TestLineNumber,
                TestClassFactory = () => (TClass)InstanceHelper.CreateInstance(
                    testMethodInformation,
                    TestClassArguments, Properties, testBuilderContext),
                TestMethodInvoker = (@class, token) =>
                {
                    var arguments = TestMethodArguments;

                    if (TestBody.GetParameters().LastOrDefault()?.ParameterType == typeof(CancellationToken))
                    {
                        arguments = TestMethodArguments.Append(token).ToArray();
                    }

                    return AsyncConvert.ConvertObject(TestBody.Invoke(@class, arguments));
                },
                PropertiesProvider = () => Properties ?? new Dictionary<string, object?>(),
                ClassDataProvider = new ArgumentsDataProvider(TestClassArguments ?? Array.Empty<object?>()),
                MethodDataProvider = new ArgumentsDataProvider(TestMethodArguments)
            };

            if (Exception != null)
            {
                discoveryFailures.Add(new DiscoveryFailure
                {
                    TestId = TestId,
                    Exception = Exception,
                    TestFilePath = TestFilePath,
                    TestLineNumber = TestLineNumber,
                    TestClassName = TestClassType.Name,
                    TestMethodName = TestName ?? TestBody.Name
                });
            }
            else
            {
                testDefinitions.Add(testDefinition);
            }
        }
        catch (Exception ex)
        {
            discoveryFailures.Add(new DiscoveryFailure
            {
                TestId = TestId,
                Exception = ex,
                TestFilePath = TestFilePath,
                TestLineNumber = TestLineNumber,
                TestClassName = TestClassType.Name,
                TestMethodName = TestName ?? TestBody.Name
            });
        }

        return new DiscoveryResult
        {
            TestDefinitions = testDefinitions,
            DiscoveryFailures = discoveryFailures
        };
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
