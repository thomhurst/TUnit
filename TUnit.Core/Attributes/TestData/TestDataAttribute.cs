namespace TUnit.Core;

public class TestDataAttribute : TUnitAttribute, IDataAttribute
{
    public bool AccessesInstanceData { get; init; }
}
