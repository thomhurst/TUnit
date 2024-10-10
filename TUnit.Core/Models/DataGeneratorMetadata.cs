using System.Reflection;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record DataGeneratorMetadata
{
    public required Type TestClassType { get; init; }
    public required Dictionary<string, object?> TestObjectBag { get; init; }
    public required ParameterInfo[]? ParameterInfos { get; init; }
    public required PropertyInfo? PropertyInfo { get; init; }
    public required DataGeneratorType Type { get; init; }
}