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

// 11 data-driven entries around a run of 100 plain entries: the first fill method closes at
// the 100-entry total cap (5 data-driven + 95 plain), the second holds 5 plain + 6 data-driven.
[EngineTest(ExpectedResult.Pass)]
public class LongPlainRunEntryTests
{
    [Test]
    [Arguments(1)]
    public void Data000(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data001(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data002(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data003(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data004(int value)
    {
    }

    [Test]
    public void Plain005()
    {
    }

    [Test]
    public void Plain006()
    {
    }

    [Test]
    public void Plain007()
    {
    }

    [Test]
    public void Plain008()
    {
    }

    [Test]
    public void Plain009()
    {
    }

    [Test]
    public void Plain010()
    {
    }

    [Test]
    public void Plain011()
    {
    }

    [Test]
    public void Plain012()
    {
    }

    [Test]
    public void Plain013()
    {
    }

    [Test]
    public void Plain014()
    {
    }

    [Test]
    public void Plain015()
    {
    }

    [Test]
    public void Plain016()
    {
    }

    [Test]
    public void Plain017()
    {
    }

    [Test]
    public void Plain018()
    {
    }

    [Test]
    public void Plain019()
    {
    }

    [Test]
    public void Plain020()
    {
    }

    [Test]
    public void Plain021()
    {
    }

    [Test]
    public void Plain022()
    {
    }

    [Test]
    public void Plain023()
    {
    }

    [Test]
    public void Plain024()
    {
    }

    [Test]
    public void Plain025()
    {
    }

    [Test]
    public void Plain026()
    {
    }

    [Test]
    public void Plain027()
    {
    }

    [Test]
    public void Plain028()
    {
    }

    [Test]
    public void Plain029()
    {
    }

    [Test]
    public void Plain030()
    {
    }

    [Test]
    public void Plain031()
    {
    }

    [Test]
    public void Plain032()
    {
    }

    [Test]
    public void Plain033()
    {
    }

    [Test]
    public void Plain034()
    {
    }

    [Test]
    public void Plain035()
    {
    }

    [Test]
    public void Plain036()
    {
    }

    [Test]
    public void Plain037()
    {
    }

    [Test]
    public void Plain038()
    {
    }

    [Test]
    public void Plain039()
    {
    }

    [Test]
    public void Plain040()
    {
    }

    [Test]
    public void Plain041()
    {
    }

    [Test]
    public void Plain042()
    {
    }

    [Test]
    public void Plain043()
    {
    }

    [Test]
    public void Plain044()
    {
    }

    [Test]
    public void Plain045()
    {
    }

    [Test]
    public void Plain046()
    {
    }

    [Test]
    public void Plain047()
    {
    }

    [Test]
    public void Plain048()
    {
    }

    [Test]
    public void Plain049()
    {
    }

    [Test]
    public void Plain050()
    {
    }

    [Test]
    public void Plain051()
    {
    }

    [Test]
    public void Plain052()
    {
    }

    [Test]
    public void Plain053()
    {
    }

    [Test]
    public void Plain054()
    {
    }

    [Test]
    public void Plain055()
    {
    }

    [Test]
    public void Plain056()
    {
    }

    [Test]
    public void Plain057()
    {
    }

    [Test]
    public void Plain058()
    {
    }

    [Test]
    public void Plain059()
    {
    }

    [Test]
    public void Plain060()
    {
    }

    [Test]
    public void Plain061()
    {
    }

    [Test]
    public void Plain062()
    {
    }

    [Test]
    public void Plain063()
    {
    }

    [Test]
    public void Plain064()
    {
    }

    [Test]
    public void Plain065()
    {
    }

    [Test]
    public void Plain066()
    {
    }

    [Test]
    public void Plain067()
    {
    }

    [Test]
    public void Plain068()
    {
    }

    [Test]
    public void Plain069()
    {
    }

    [Test]
    public void Plain070()
    {
    }

    [Test]
    public void Plain071()
    {
    }

    [Test]
    public void Plain072()
    {
    }

    [Test]
    public void Plain073()
    {
    }

    [Test]
    public void Plain074()
    {
    }

    [Test]
    public void Plain075()
    {
    }

    [Test]
    public void Plain076()
    {
    }

    [Test]
    public void Plain077()
    {
    }

    [Test]
    public void Plain078()
    {
    }

    [Test]
    public void Plain079()
    {
    }

    [Test]
    public void Plain080()
    {
    }

    [Test]
    public void Plain081()
    {
    }

    [Test]
    public void Plain082()
    {
    }

    [Test]
    public void Plain083()
    {
    }

    [Test]
    public void Plain084()
    {
    }

    [Test]
    public void Plain085()
    {
    }

    [Test]
    public void Plain086()
    {
    }

    [Test]
    public void Plain087()
    {
    }

    [Test]
    public void Plain088()
    {
    }

    [Test]
    public void Plain089()
    {
    }

    [Test]
    public void Plain090()
    {
    }

    [Test]
    public void Plain091()
    {
    }

    [Test]
    public void Plain092()
    {
    }

    [Test]
    public void Plain093()
    {
    }

    [Test]
    public void Plain094()
    {
    }

    [Test]
    public void Plain095()
    {
    }

    [Test]
    public void Plain096()
    {
    }

    [Test]
    public void Plain097()
    {
    }

    [Test]
    public void Plain098()
    {
    }

    [Test]
    public void Plain099()
    {
    }

    [Test]
    public void Plain100()
    {
    }

    [Test]
    public void Plain101()
    {
    }

    [Test]
    public void Plain102()
    {
    }

    [Test]
    public void Plain103()
    {
    }

    [Test]
    public void Plain104()
    {
    }

    [Test]
    [Arguments(1)]
    public void Data105(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data106(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data107(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data108(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data109(int value)
    {
    }

    [Test]
    [Arguments(1)]
    public void Data110(int value)
    {
    }
}
