namespace TUnit.Profile;

/// <summary>
/// Performance profiling workload: ~1000 tests covering diverse scenarios
/// to stress test TUnit's scheduling, parallelism, and execution infrastructure
/// </summary>
public class SyncTests
{
    [Test]
    [Repeat(100)]
    public void FastSyncTest()
    {
        _ = 1 + 1;
    }

    [Test]
    [Arguments(1, 2)]
    [Arguments(3, 4)]
    [Arguments(5, 6)]
    [Arguments(7, 8)]
    [Arguments(9, 10)]
    [Repeat(20)]
    public void ParameterizedSyncTest(int a, int b)
    {
        _ = a + b;
    }

    [Test]
    [Repeat(50)]
    public void ComputeBoundTest()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
        {
            sum += i;
        }
        _ = sum;
    }
}

public class AsyncTests
{
    [Test]
    [Repeat(100)]
    public async Task FastAsyncTest()
    {
        await Task.CompletedTask;
        _ = 1 + 1;
    }

    [Test]
    [Repeat(50)]
    public async Task AsyncWithYield()
    {
        await Task.Yield();
        _ = 1 + 1;
    }

    [Test]
    [Arguments(1, 2)]
    [Arguments(3, 4)]
    [Arguments(5, 6)]
    [Arguments(7, 8)]
    [Repeat(10)]
    public async Task ParameterizedAsyncTest(int a, int b)
    {
        await Task.CompletedTask;
        _ = a + b;
    }
}

public class MatrixTests
{
    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    [Arguments(8)]
    [Arguments(9)]
    [Arguments(10)]
    [Repeat(10)]
    public void TenByTenMatrix(int value)
    {
        _ = value * value;
    }

    [Test]
    [Arguments("a", 1)]
    [Arguments("b", 2)]
    [Arguments("c", 3)]
    [Arguments("d", 4)]
    [Arguments("e", 5)]
    [Repeat(10)]
    public void MixedParameterTypes(string str, int num)
    {
        _ = str + num;
    }
}

public class DataExpansionTests
{
    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 6)]
    [Arguments(7, 8, 9)]
    [Arguments(10, 11, 12)]
    [Arguments(13, 14, 15)]
    [Arguments(16, 17, 18)]
    [Arguments(19, 20, 21)]
    [Arguments(22, 23, 24)]
    [Repeat(5)]
    public void ThreeParameterCombinations(int a, int b, int c)
    {
        _ = a + b + c;
    }

    [Test]
    [Arguments(true, 1)]
    [Arguments(false, 2)]
    [Arguments(true, 3)]
    [Arguments(false, 4)]
    [Repeat(10)]
    public void BooleanAndInt(bool flag, int value)
    {
        _ = flag ? value : -value;
    }
}

public class MixedWorkloadTests
{
    [Test]
    [Repeat(30)]
    public void QuickTest1() => _ = 1;

    [Test]
    [Repeat(30)]
    public void QuickTest2() => _ = 2;

    [Test]
    [Repeat(30)]
    public void QuickTest3() => _ = 3;

    [Test]
    [Repeat(30)]
    public async Task QuickAsyncTest1() { await Task.CompletedTask; _ = 1; }

    [Test]
    [Repeat(30)]
    public async Task QuickAsyncTest2() { await Task.CompletedTask; _ = 2; }

    [Test]
    [Repeat(30)]
    public async Task QuickAsyncTest3() { await Task.CompletedTask; _ = 3; }
}
