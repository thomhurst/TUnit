namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MatrixAttribute(params object?[]? objects) : TestDataAttribute
{
    public object?[] Objects { get; } = objects ?? [ null ];
}