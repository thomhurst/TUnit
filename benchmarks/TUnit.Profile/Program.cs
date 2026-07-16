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

/// <summary>
/// Massive parallel tests similar to benchmark suite - stress tests parallelism
/// with large numbers of CPU-bound, IO-bound, and mixed workload tests
/// </summary>
public class MassiveParallelTests
{
    [Test]
    [Arguments(1)] [Arguments(2)] [Arguments(3)] [Arguments(4)] [Arguments(5)]
    [Arguments(6)] [Arguments(7)] [Arguments(8)] [Arguments(9)] [Arguments(10)]
    [Arguments(11)] [Arguments(12)] [Arguments(13)] [Arguments(14)] [Arguments(15)]
    [Arguments(16)] [Arguments(17)] [Arguments(18)] [Arguments(19)] [Arguments(20)]
    [Arguments(21)] [Arguments(22)] [Arguments(23)] [Arguments(24)] [Arguments(25)]
    [Arguments(26)] [Arguments(27)] [Arguments(28)] [Arguments(29)] [Arguments(30)]
    [Arguments(31)] [Arguments(32)] [Arguments(33)] [Arguments(34)] [Arguments(35)]
    [Arguments(36)] [Arguments(37)] [Arguments(38)] [Arguments(39)] [Arguments(40)]
    [Arguments(41)] [Arguments(42)] [Arguments(43)] [Arguments(44)] [Arguments(45)]
    [Arguments(46)] [Arguments(47)] [Arguments(48)] [Arguments(49)]
    public void Parallel_CPUBound_Test(int taskId)
    {
        var result = PerformCPUWork(taskId);
        var doubled = result * 2;
        _ = doubled;
    }

    [Test]
    [Arguments(1)] [Arguments(2)] [Arguments(3)] [Arguments(4)] [Arguments(5)]
    [Arguments(6)] [Arguments(7)] [Arguments(8)] [Arguments(9)] [Arguments(10)]
    [Arguments(11)] [Arguments(12)] [Arguments(13)] [Arguments(14)] [Arguments(15)]
    [Arguments(16)] [Arguments(17)] [Arguments(18)] [Arguments(19)] [Arguments(20)]
    [Arguments(21)] [Arguments(22)] [Arguments(23)] [Arguments(24)] [Arguments(25)]
    [Arguments(26)] [Arguments(27)] [Arguments(28)] [Arguments(29)] [Arguments(30)]
    [Arguments(31)] [Arguments(32)] [Arguments(33)] [Arguments(34)] [Arguments(35)]
    [Arguments(36)] [Arguments(37)] [Arguments(38)] [Arguments(39)] [Arguments(40)]
    [Arguments(41)] [Arguments(42)] [Arguments(43)] [Arguments(44)] [Arguments(45)]
    [Arguments(46)] [Arguments(47)] [Arguments(48)] [Arguments(49)]
    public async Task Parallel_IOBound_Test(int taskId)
    {
        await Task.Delay(50);
        var result = await PerformIOWorkAsync(taskId);
        var length = result.Length;
        _ = length;
    }

    [Test]
    [Arguments(1, 101)] [Arguments(2, 102)] [Arguments(3, 103)] [Arguments(4, 104)]
    [Arguments(5, 105)] [Arguments(6, 106)] [Arguments(7, 107)] [Arguments(8, 108)]
    [Arguments(9, 109)] [Arguments(10, 110)] [Arguments(11, 111)] [Arguments(12, 112)]
    [Arguments(13, 113)] [Arguments(14, 114)] [Arguments(15, 115)] [Arguments(16, 116)]
    [Arguments(17, 117)] [Arguments(18, 118)] [Arguments(19, 119)]
    public async Task Parallel_Mixed_Test(int cpuValue, int ioValue)
    {
        var cpuResult = PerformCPUWork(cpuValue);
        await Task.Yield();
        var ioResult = await PerformIOWorkAsync(ioValue);
        var combined = $"{cpuResult}_{ioResult}";
        _ = combined;
    }

    private static int PerformCPUWork(int taskId)
    {
        var sum = 0;
        for (var i = 1; i <= 100; i++)
        {
            sum += (i * taskId) % 1000;
        }
        return sum;
    }

    private static async Task<string> PerformIOWorkAsync(int taskId)
    {
        await Task.Yield();
        return $"Task_{taskId}_Result_{Guid.NewGuid().ToString()[..8]}";
    }
}
