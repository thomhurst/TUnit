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
        TestNameWithArguments = methodInfo.Name + GetArgumentValues(MethodArgumentValues) + GetCountInBrackets();
        TestNameWithParameterTypes = methodInfo.Name + GetParameterTypes(MethodParameterTypes);
        ClassName = ClassType.Name;
        FullyQualifiedClassName = ClassType.FullName!;
        Assembly = ClassType.Assembly;
        Source = sourceLocation.RawSource;

        var methodAndClassAttributes = methodInfo.GetCustomAttributes()
            .Concat(ClassType.GetCustomAttributes())
            .ToArray();

        IsSingleTest = Count > 1 || MethodParameterTypes?.Any() == true;
        
        SkipReason = methodAndClassAttributes
            .OfType<SkipAttribute>()
            .FirstOrDefault()
            ?.Reason;
        
        RetryCount = methodAndClassAttributes
            .OfType<RetryAttribute>()
            .FirstOrDefault()
            ?.Times ?? 0;
        
        RepeatCount = methodAndClassAttributes
            .OfType<RepeatAttribute>()
            .FirstOrDefault()
            ?.Times ?? 0;;

        if (RepeatCount > 0)
        {
            CurrentExecutionCount = count;
        }
        
        Order = methodAndClassAttributes
            .OfType<OrderAttribute>()
            .FirstOrDefault()
            ?.Order ?? int.MaxValue;
        
        NotInParallelConstraintKeys = GetNotInParallelConstraintKeys(methodAndClassAttributes);
        
        AddCategories(methodAndClassAttributes);
        
        Timeout = GetTimeout(methodAndClassAttributes);
        
        FileName = sourceLocation.FileName;
        MinLineNumber = sourceLocation.MinLineNumber;
        MaxLineNumber = sourceLocation.MaxLineNumber;

        UniqueId = $"{FullyQualifiedClassName}.{TestName}.{GetParameterTypes(ClassParameterTypes)}.{GetArgumentValues(ClassArgumentValues)}.{GetParameterTypes(MethodParameterTypes)}.{GetArgumentValues(MethodArgumentValues)}.{Count}";
    }

    private static string[]? GetNotInParallelConstraintKeys(IEnumerable<Attribute> methodAndClassAttributes)
    {
        var notInParallelAttribute = methodAndClassAttributes
            .OfType<NotInParallelAttribute>()
            .FirstOrDefault();

        return notInParallelAttribute?.ConstraintKeys;
    }

    public string[]? NotInParallelConstraintKeys { get; }

    public int Order { get; }

    private string GetCountInBrackets()
    {
        return RepeatCount > 0 ? $" [{Count}]" : string.Empty;
    }

    public bool IsSingleTest { get; }

    private void AddCategories(Attribute[] methodAndClassAttributes)
    {
        var categoryAttributes = methodAndClassAttributes
            .OfType<TestCategoryAttribute>();

        var categories = categoryAttributes
            .Select(x => x.Category);
        
        Categories.AddRange(categories);
    }

    public List<string> Categories { get; } = new();
    
    private static TimeSpan? GetTimeout(IEnumerable<Attribute> methodAndClassAttributes)
    {
        return methodAndClassAttributes
            .OfType<TimeoutAttribute>()
            .FirstOrDefault()
            ?.Timeout;
    }


    public int RetryCount { get; }
    public int RepeatCount { get; }

    public static string GetArgumentValues(object?[]? argumentValues)
    {
        if (argumentValues == null)
        {
            return string.Empty;
        }
        
        return $"({string.Join(',', argumentValues.Select(StringifyArgument))})";
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

    public TimeSpan? Timeout { get; }

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
    public string TestNameWithArguments { get; }
    public string TestNameWithParameterTypes { get; }

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