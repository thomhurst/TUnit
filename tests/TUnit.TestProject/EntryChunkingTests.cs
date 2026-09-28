using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.EntryChunking;

// Source-generator snapshot fixtures for how a class's Entries array is split into
// __FillEntriesN methods. Only entries with nested construction (data sources, parameters,
// dependencies, return types) count toward the 10-per-fill-method budget.

// 10 data-driven entries: fits one fill method, so emitted as a single array initializer.
[EngineTest(ExpectedResult.Pass)]
public class TenDataDrivenEntryTests
{
    [Test]
    [Arguments(1)]
    public void Data00(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data01(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data02(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data03(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data04(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data05(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data06(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data07(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data08(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data09(int value)
    {
    }
}

// 11 data-driven entries: exceeds the budget, so split into fill methods of 10 and 1.
[EngineTest(ExpectedResult.Pass)]
public class ElevenDataDrivenEntryTests
{
    [Test]
    [Arguments(1)]
    public void Data00(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data01(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data02(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data03(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data04(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data05(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data06(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data07(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data08(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data09(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data10(int value)
    {
    }
}

// 11 plain entries: no nested construction, so emitted as a single array initializer.
[EngineTest(ExpectedResult.Pass)]
public class ElevenPlainEntryTests
{
    [Test]
    public void Plain00()
    {
    }

    [Test]
    public void Plain01()
    {
    }

    [Test]
    public void Plain02()
    {
    }

    [Test]
    public void Plain03()
    {
    }

    [Test]
    public void Plain04()
    {
    }

    [Test]
    public void Plain05()
    {
    }

    [Test]
    public void Plain06()
    {
    }

    [Test]
    public void Plain07()
    {
    }

    [Test]
    public void Plain08()
    {
    }

    [Test]
    public void Plain09()
    {
    }

    [Test]
    public void Plain10()
    {
    }
}

// Many plain entries and one data-driven entry: plain entries never trigger chunking.
[EngineTest(ExpectedResult.Pass)]
public class ManyPlainOneDataDrivenEntryTests
{
    [Test]
    public void Plain00()
    {
    }

    [Test]
    public void Plain01()
    {
    }

    [Test]
    public void Plain02()
    {
    }

    [Test]
    public void Plain03()
    {
    }

    [Test]
    public void Plain04()
    {
    }

    [Test]
    public void Plain05()
    {
    }

    [Test]
    public void Plain06()
    {
    }

    [Test]
    public void Plain07()
    {
    }

    [Test]
    public void Plain08()
    {
    }

    [Test]
    public void Plain09()
    {
    }

    [Test]
    public void Plain10()
    {
    }

    [Test]
    public void Plain11()
    {
    }

    [Test]
    [Arguments(1)]
    public void Data12(int value)
    {
    }

    [Test]
    public void Plain13()
    {
    }

    [Test]
    public void Plain14()
    {
    }

    [Test]
    public void Plain15()
    {
    }

    [Test]
    public void Plain16()
    {
    }

    [Test]
    public void Plain17()
    {
    }

    [Test]
    public void Plain18()
    {
    }

    [Test]
    public void Plain19()
    {
    }

    [Test]
    public void Plain20()
    {
    }

    [Test]
    public void Plain21()
    {
    }

    [Test]
    public void Plain22()
    {
    }

    [Test]
    public void Plain23()
    {
    }

    [Test]
    public void Plain24()
    {
    }
}

// Plain runs between 12 data-driven entries: two fill methods, with each plain run kept in the current one.
[EngineTest(ExpectedResult.Pass)]
public class InterleavedEntryTests
{
    [Test]
    public void Plain00()
    {
    }

    [Test]
    public void Plain01()
    {
    }

    [Test]
    [Arguments(1)]
    public void Data02(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data03(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data04(int value)
    {
    }

    [Test]
    public void Plain05()
    {
    }

    [Test]
    [Arguments(1)]
    public void Data06(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data07(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data08(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data09(int value)
    {
    }

    [Test]
    public void Plain10()
    {
    }

    [Test]
    public void Plain11()
    {
    }

    [Test]
    [Arguments(1)]
    public void Data12(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data13(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data14(int value)
    {
    }

    [Test]
    public void Plain15()
    {
    }

    [Test]
    [Arguments(1)]
    public void Data16(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data17(int value)
    {
    }

    [Test]
    public void Plain18()
    {
    }

    [Test]
    public void Plain19()
    {
    }
}
