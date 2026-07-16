namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class MatrixExclusionAttribute(params object?[]? objects) : TUnitAttribute
{
    public object?[] Objects { get; } = objects ?? [null];
}
