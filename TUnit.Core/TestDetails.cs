using System.Reflection;

namespace TUnit.Core;

internal record TestDetails
{
    public TestDetails(MethodInfo methodInfo,
        Type classType,
        SourceLocation sourceLocation,
        object?[]? methodArguments, 
        object?[]? classArguments, 
        int count)
    {
        MethodInfo = methodInfo;
        ClassType = classType;
        SourceLocation = sourceLocation;
        Count = count;

        MethodParameterTypes = methodArguments?.Select(x => x?.GetType() ?? typeof(object)).ToArray();
        MethodParameterTypes = classArguments?.Select(x => x?.GetType() ?? typeof(object)).ToArray();

        MethodArgumentValues = methodArguments;
        ClassArgumentValues = classArguments;
        
        TestName = methodInfo.Name;
        DisplayName = methodInfo.Name + GetArgumentValues() + GetCountInBrackets();
        ClassName = ClassType.Name;
        FullyQualifiedClassName = ClassType.FullName!;
        Assembly = ClassType.Assembly;
        Source = sourceLocation.RawSource;

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

        if (RepeatCount > 0)
        {
            CurrentExecutionCount = count;
        }
        
        Order = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeType == typeof(RetryAttribute))
            ?.ConstructorArguments.FirstOrDefault().Value as int? ?? int.MaxValue;
        
        NotInParallelConstraintKey = GetNotInParallelConstraintKey(methodAndClassAttributes);
        
        AddCategories(methodAndClassAttributes);
        
        Timeout = GetTimeout(methodAndClassAttributes);
        
        FileName = sourceLocation.FileName;
        MinLineNumber = sourceLocation.MinLineNumber;
        MaxLineNumber = sourceLocation.MaxLineNumber;

        UniqueId = $"{FullyQualifiedClassName}.{DisplayName}";
    }

    private static string? GetNotInParallelConstraintKey(CustomAttributeData[] methodAndClassAttributes)
    {
        var notInParallelAttribute = methodAndClassAttributes
            .FirstOrDefault(x => x.AttributeType == typeof(NotInParallelAttribute));

        if (notInParallelAttribute is null)
        {
            return null;
        }

        return notInParallelAttribute.ConstructorArguments
                   .FirstOrDefault()
                   .Value as string
               ?? string.Empty;
    }

    public string? NotInParallelConstraintKey { get; }

    public int Order { get; }

    private string GetCountInBrackets()
    {
        return RepeatCount > 0 ? $" [{Count}]" : string.Empty;
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

    public string GetArgumentValues()
    {
        if (MethodArgumentValues == null)
        {
            return string.Empty;
        }
        
        return $"({string.Join(',', MethodArgumentValues.Select(StringifyArgument))})";
    }
    
    public string UniqueId { get; }
    
    public string TestName { get; }

    public string ClassName { get; }
    
    public string FullyQualifiedClassName { get; }

    public Assembly Assembly { get; }
    
    public string Source { get; }
    public MethodInfo MethodInfo { get; }
    public Type ClassType { get; }
    public string? FileName { get; }

    public TimeSpan Timeout { get; }

    public int CurrentExecutionCount { get; internal set; }
    
    public int MinLineNumber { get; }
    public int MaxLineNumber { get; }
    public Type[]? MethodParameterTypes { get; }
    public Type[]? ClassParameterTypes { get; }
    public object?[]? MethodArgumentValues { get; }
    public object?[]? ClassArgumentValues { get; }
    public SourceLocation SourceLocation { get; }
    public int Count { get; }

    public string? SkipReason { get; }
    public bool IsSkipped => !string.IsNullOrEmpty(SkipReason);
    public string DisplayName { get; }

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