namespace TUnit.TestProject.Bugs._7222;

// A method row that breaks the constraints, with no [GenerateGenericTest], fails once for each class row in both
// modes: reflection mode keeps the class data sources on this entry, so source-generated mode must too (#7344).
//
// Not marked [EngineTest]: the failures are intentional. TUnit.Engine.Tests (Issue7222Tests) checks them.
[Arguments(1)]
[Arguments(2)]
public class ClassRowsConstraintViolationTests(int value)
{
    public int Value => value;

    [Test]
    [Arguments("x")]
    public void StructOnly<T>(T item) where T : struct
    {
    }
}
