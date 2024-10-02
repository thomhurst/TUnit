namespace TUnit.Core;

public class TestDataAttribute : TUnitAttribute, IDataAttribute
{
    public bool DisposeAfterTest { get; init; } = true;
}