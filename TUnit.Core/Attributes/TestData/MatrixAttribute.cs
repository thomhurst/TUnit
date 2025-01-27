using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute(params object?[]? objects) : TestDataAttribute
{
    public object?[] Objects { get; } = objects ?? [ null ];

    public object?[]? Excluding { get; init; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(params T?[]? objects) : MatrixAttribute(objects?.Cast<object>().ToArray());