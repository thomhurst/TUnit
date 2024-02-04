using System.Reflection;

namespace TUnit.Core;

public record TestDetails
{
    public TestDetails(MethodInfo MethodInfo,
        Type ClassType,
        SourceLocation SourceLocation,
        ParameterArgument[]? arguments)
    {
        this.MethodInfo = MethodInfo;
        this.ClassType = ClassType;
        this.SourceLocation = SourceLocation;
        
        ParameterTypes = arguments?.Select(x => x.Type).ToArray();
        ArgumentValues = arguments?.Select(x => x.Value).ToArray();
        
        TestName = MethodInfo.Name;
        SimpleMethodName = MethodInfo.Name;
        DisplayName = MethodInfo.Name + GetArgumentValues();
        ClassName = this.ClassType.Name;
        FullyQualifiedClassName = this.ClassType.FullName!;
        Assembly = this.ClassType.Assembly;
        Source = this.ClassType.Assembly.Location;
        FullyQualifiedName = $"{this.ClassType.FullName}.{MethodInfo.Name}{GetParameterTypes(ParameterTypes)}";

        var methodAndClassAttributes = MethodInfo.CustomAttributes
            .Concat(this.ClassType.CustomAttributes)
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
        
        FileName = SourceLocation.FileName;
        MinLineNumber = SourceLocation.MinLineNumber;
        MaxLineNumber = SourceLocation.MaxLineNumber;

        UniqueId = FullyQualifiedClassName + DisplayName + GetParameterTypes(ParameterTypes);
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