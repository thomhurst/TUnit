namespace TUnit.PerformanceBenchmarks.Tests.Synthetic;

public class SyntheticTests_204
{
    [Arguments(1)] [Arguments(2)] [Arguments(3)] [Arguments(4)] [Arguments(5)]
    [Arguments(6)] [Arguments(7)] [Arguments(8)] [Arguments(9)] [Arguments(10)]
    [Category("Synthetic")] [Property("Group", "4")]
    [Test] public void DataDriven_A(int x) { _ = x; }

    [Arguments("a", 1)] [Arguments("b", 2)] [Arguments("c", 3)]
    [Arguments("d", 4)] [Arguments("e", 5)]
    [Test] public void DataDriven_B(string s, int n) { _ = (s, n); }

    [Repeat(3)]
    [Test] public void Repeating() { }

    [DependsOn(nameof(Simple))]
    [Test] public void Dependent() { }

    [Test] public void Simple() { }

    [Before(Test)] public void SetUp() { }
    [After(Test)] public void TearDown() { }
}
