using System.Reflection;
using TUnit.Core.Attributes;

namespace TUnit.Core;

public record Test
{
    public Test(MethodInfo MethodInfo,
        SourceLocation SourceLocation,
        object?[]? Arguments)
    {
        var classType = MethodInfo.DeclaringType!;
        
        this.MethodInfo = MethodInfo;
        this.Arguments = Arguments;
        this.SourceLocation = SourceLocation;
        
        TestName = MethodInfo.Name;
        ClassName = classType.Name;
        FullyQualifiedClassName = classType.FullName!;
        Assembly = classType.Assembly;
        Source = classType.Assembly.Location;
        FullName = $"{classType.FullName}.{MethodInfo.Name}{GetArguments(Arguments)}";
        IsSkipped = MethodInfo.CustomAttributes
            .Concat(classType.CustomAttributes)
            .Any(x => x.AttributeType == typeof(SkipAttribute));

        FileName = SourceLocation.FileName;
        MinLineNumber = SourceLocation.MinLineNumber;
        MaxLineNumber = SourceLocation.MaxLineNumber;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string TestName { get; }

    public string ClassName { get; }
    
    public string FullyQualifiedClassName { get; set; }

    public Assembly Assembly { get; }
    
    public string Source { get; }
    public string FullName { get; }
    public MethodInfo MethodInfo { get; init; }
    public string? FileName { get; set; }
    public int MinLineNumber { get; set; }
    public int MaxLineNumber { get; set; }
    public object?[]? Arguments { get; init; }
    public SourceLocation SourceLocation { get; }
    
    public bool IsSkipped { get; }

    public void Deconstruct(out MethodInfo methodInfo, out object?[]? arguments)
    {
        methodInfo = MethodInfo;
        arguments = Arguments;
    }
    
    private static string GetArguments(object?[]? arguments)
    {
        if (arguments is null)
        {
            return string.Empty;
        }

        var argsAsString = arguments.Select(StringifyArgument);
        
        return $"({string.Join(", ", argsAsString)})";
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