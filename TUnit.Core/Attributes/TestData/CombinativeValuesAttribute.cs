namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class CombinativeValuesAttribute : TUnitAttribute
{
    public object?[] Objects { get; }

    public CombinativeValuesAttribute(params object?[] objects)
    {
        Objects = objects;
    }
}