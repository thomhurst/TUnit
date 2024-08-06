namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class MatrixAttribute : TUnitAttribute
{
    public object?[] Objects { get; }

    public MatrixAttribute(params object?[]? objects)
    {
        Objects = objects ?? [ null ];
    }
}