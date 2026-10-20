namespace TUnit.TestProject.Bugs._7155;

// A null cannot be passed to a type parameter constrained to 'struct', so the row infers object, which breaks
// the constraint. Each row surfaces as one failed test with the constraint message, in both modes, instead of
// running as Run<int> with the null turned into 0. The generated code still compiles.
// No [EngineTest] attribute: the rows fail on purpose, and TUnit.Engine.Tests targets this class by name.
public class NullAndValueTypeStructConstraintMethod
{
    [Test]
    [Arguments(1, null)]
    [Arguments(null, 1)]
    public void Run<T>(T first, T second) where T : struct
    {
    }
}
