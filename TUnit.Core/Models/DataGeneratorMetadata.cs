using System.Reflection;
using TUnit.Core.Enums;

namespace TUnit.Core;

public record DataGeneratorMetadata
{
    public required ParameterInfo[]? ParameterInfos { get; init; }
    public required PropertyInfo? PropertyInfo { get; init; }
    public required DataGeneratorType Type { get; init; }
}