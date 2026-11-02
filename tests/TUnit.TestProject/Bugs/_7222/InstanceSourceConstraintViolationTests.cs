namespace TUnit.TestProject.Bugs._7222;

// An instance data source whose rows break the method's constraints still reads them from a test class instance,
// so the failing entry keeps the class data sources that build it. Each row fails with the constraint message in
// both modes, not with a constructor error in source-generated mode.
//
// Not marked [EngineTest]: the failures are intentional. TUnit.Engine.Tests (Issue7222Tests) checks them.
[Arguments(1)]
public class InstanceSourceConstraintViolationTests(int value)
{
    public IEnumerable<string> Rows()
    {
        yield return value.ToString();
        yield return "b";
    }

    [Test]
    [InstanceMethodDataSource(nameof(Rows))]
    public void StructOnly<T>(T item) where T : struct
    {
    }
}
