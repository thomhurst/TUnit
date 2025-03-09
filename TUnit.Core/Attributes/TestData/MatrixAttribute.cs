namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute(params object?[]? objects) : TestDataAttribute
{
    protected MatrixAttribute() : this(null)
    {
    }

    public virtual object?[] GetObjects(object? instance) => objects ?? [ null ];

    public object?[]? Excluding { get; init; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute<T>(params T?[]? objects) : MatrixAttribute(objects?.Cast<object>().ToArray());