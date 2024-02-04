using System.Reflection;

namespace TUnit.Core;

public record TestDetails
{
    public TestDetails(MethodInfo methodInfo,
        Type classType,
        SourceLocation sourceLocation,
        ParameterArgument[]? arguments, 
        int count)
    {
        MethodInfo = methodInfo;
        ClassType = classType;
        SourceLocation = sourceLocation;
        Count = count;

        ParameterTypes = arguments?.Select(x => x.Type).ToArray();
        ArgumentValues = arguments?.Select(x => x.Value).ToArray();
        
        TestName = methodInfo.Name;
        SimpleMethodName = methodInfo.Name;
        DisplayName = methodInfo.Name + GetCount() + GetArgumentValues();
        ClassName = ClassType.Name;
        FullyQualifiedClassName = ClassType.FullName!;
        Assembly = ClassType.Assembly;
        Source = ClassType.Assembly.Location;
        FullyQualifiedName = $"{ClassType.FullName}.{methodInfo.Name}{GetParameterTypes(ParameterTypes)}";

        var methodAndClassAttributes = methodInfo.CustomAttributes
            .Concat(ClassType.CustomAttributes)
            .ToArray();

        IsSingleTest = !methodAndClassAttributes.Any(x => x.AttributeType == typeof(TestWithDataAttribute)
                                                          || x.AttributeType == typeof(TestDataSourceAttribute));
        
        SkipReason = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeType == typeof(SkipAttribute))
            ?.ConstructorArguments.FirstOrDefault().Value as string;
        
        RetryCount = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeType == typeof(RetryAttribute))
            ?.ConstructorArguments.FirstOrDefault().Value as int? ?? 0;
        
        RepeatCount = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeType == typeof(RepeatAttribute))
            ?.ConstructorArguments.FirstOrDefault().Value as int? ?? 0;

        AddCategories(methodAndClassAttributes);
        
        Timeout = GetTimeout(methodAndClassAttributes);
        
        FileName = sourceLocation.FileName;
        MinLineNumber = sourceLocation.MinLineNumber;
        MaxLineNumber = sourceLocation.MaxLineNumber;

        UniqueId = FullyQualifiedClassName + DisplayName + Count + GetParameterTypes(ParameterTypes);
    }

    private string GetCount()
    {
        return Count == 1 ? string.Empty : Count.ToString();
    }

    public bool IsSingleTest { get; }

    private void AddCategories(CustomAttributeData[] methodAndClassAttributes)
    {
        var categoryAttributes = methodAndClassAttributes
            .Where(x => x.AttributeType == typeof(TestCategoryAttribute));

        var categories = categoryAttributes
            .Select(x => x.ConstructorArguments.FirstOrDefault().Value)
            .OfType<string>();
        
        Categories.AddRange(categories);
    }

    public List<string> Categories { get; } = new();

    public string SimpleMethodName { get; set; }

    private static TimeSpan GetTimeout(CustomAttributeData[] methodAndClassAttributes)
    {
        var timeoutMilliseconds = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeType == typeof(TimeoutAttribute))
            ?.ConstructorArguments.FirstOrDefault().Value as int?;

        if (timeoutMilliseconds is 0 or null)
        {
            return default;
        }
        
        return TimeSpan.FromMilliseconds(timeoutMilliseconds.Value);
    }


    public int RetryCount { get; }
    public int RepeatCount { get; }

    private string GetArgumentValues()
    {
        if (ArgumentValues == null)
        {
            return string.Empty;
        }
        
        return $"({string.Join(',', ArgumentValues.Select(StringifyArgument))})";
    }
    
    public string UniqueId { get; }
    
    public string TestName { get; }

    public string ClassName { get; }
    
    public string FullyQualifiedClassName { get; }

    public Assembly Assembly { get; }
    
    public string Source { get; }
    public string FullyQualifiedName { get; }
    public MethodInfo MethodInfo { get; }
    public Type ClassType { get; }
    public string? FileName { get; }

    public TimeSpan Timeout { get; }
    
    public int CurrentExecutionCount { get; internal set; }
    
    public int MinLineNumber { get; }
    public int MaxLineNumber { get; }
    public Type[]? ParameterTypes { get; }
    public object?[]? ArgumentValues { get; }
    public SourceLocation SourceLocation { get; }
    public int Count { get; }

    public string? SkipReason { get; }
    public bool IsSkipped => !string.IsNullOrEmpty(SkipReason);
    public string DisplayName { get; }

    private readonly TaskCompletionSource<TUnitTestResult> _completionSource = new();
    public Task<TUnitTestResult> GetResultAsync() => _completionSource.Task;

    public TUnitTestResult SetResult(TUnitTestResult unitTestResult)
    {
        _completionSource.SetResult(unitTestResult);
        return unitTestResult;
    }

    public static string GetParameterTypes(Type[]? types)
    {
        if (types is null)
        {
            return string.Empty;
        }

        var argsAsString = types.Select(arg => arg.FullName!);
        
        return $"({string.Join(',', argsAsString)})";
    }

    private static string StringifyArgument(object? obj)
    {
        return obj switch
        {
            null => "null",
            string stringValue => $"\"{stringValue}\"",
            _ => obj.ToString() ?? string.Empty
        };
    }
}