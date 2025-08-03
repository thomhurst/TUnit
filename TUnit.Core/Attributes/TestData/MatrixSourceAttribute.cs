using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute(params object?[]? objects) : TUnitAttribute
{
    protected MatrixAttribute() : this(null)
    {
    }

    public virtual object?[] GetObjects(DataGeneratorMetadata dataGeneratorMetadata) => objects ?? [null];

    public object?[]? Excluding { get; init; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class MatrixAttribute<T>(params T?[]? objects) : MatrixAttribute(objects?.Cast<object>().ToArray()), IInfersType<T>;
