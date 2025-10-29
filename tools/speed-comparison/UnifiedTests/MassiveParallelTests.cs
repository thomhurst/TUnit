using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace UnifiedTests;

[TestClass]
#if NUNIT
[Parallelizable(ParallelScope.All)]
#endif
public class MassiveParallelTests
{
    [DataDrivenTest]
    [TestData(1)]
    [TestData(2)]
    [TestData(3)]
    [TestData(4)]
    [TestData(5)]
    [TestData(6)]
    [TestData(7)]
    [TestData(8)]
    [TestData(9)]
    [TestData(10)]
    [TestData(11)]
    [TestData(12)]
    [TestData(13)]
    [TestData(14)]
    [TestData(15)]
    [TestData(16)]
    [TestData(17)]
    [TestData(18)]
    [TestData(19)]
    [TestData(20)]
    [TestData(21)]
    [TestData(22)]
    [TestData(23)]
    [TestData(24)]
    [TestData(25)]
    [TestData(26)]
    [TestData(27)]
    [TestData(28)]
    [TestData(29)]
    [TestData(30)]
    [TestData(31)]
    [TestData(32)]
    [TestData(33)]
    [TestData(34)]
    [TestData(35)]
    [TestData(36)]
    [TestData(37)]
    [TestData(38)]
    [TestData(39)]
    [TestData(40)]
    [TestData(41)]
    [TestData(42)]
    [TestData(43)]
    [TestData(44)]
    [TestData(45)]
    [TestData(46)]
    [TestData(47)]
    [TestData(48)]
    [TestData(49)]
    public void Parallel_CPUBound_Test(int taskId)
    {
        var result = PerformCPUWork(taskId);
        var doubled = result * 2;
    }

    [DataDrivenTest]
    [TestData(1)]
    [TestData(2)]
    [TestData(3)]
    [TestData(4)]
    [TestData(5)]
    [TestData(6)]
    [TestData(7)]
    [TestData(8)]
    [TestData(9)]
    [TestData(10)]
    [TestData(11)]
    [TestData(12)]
    [TestData(13)]
    [TestData(14)]
    [TestData(15)]
    [TestData(16)]
    [TestData(17)]
    [TestData(18)]
    [TestData(19)]
    [TestData(20)]
    [TestData(21)]
    [TestData(22)]
    [TestData(23)]
    [TestData(24)]
    [TestData(25)]
    [TestData(26)]
    [TestData(27)]
    [TestData(28)]
    [TestData(29)]
    [TestData(30)]
    [TestData(31)]
    [TestData(32)]
    [TestData(33)]
    [TestData(34)]
    [TestData(35)]
    [TestData(36)]
    [TestData(37)]
    [TestData(38)]
    [TestData(39)]
    [TestData(40)]
    [TestData(41)]
    [TestData(42)]
    [TestData(43)]
    [TestData(44)]
    [TestData(45)]
    [TestData(46)]
    [TestData(47)]
    [TestData(48)]
    [TestData(49)]
    public async Task Parallel_IOBound_Test(int taskId)
    {
        await Task.Delay(50);
        var result = await PerformIOWorkAsync(taskId);
        var length = result.Length;
    }

    [DataDrivenTest]
    [TestData(1, 101)]
    [TestData(2, 102)]
    [TestData(3, 103)]
    [TestData(4, 104)]
    [TestData(5, 105)]
    [TestData(6, 106)]
    [TestData(7, 107)]
    [TestData(8, 108)]
    [TestData(9, 109)]
    [TestData(10, 110)]
    [TestData(11, 111)]
    [TestData(12, 112)]
    [TestData(13, 113)]
    [TestData(14, 114)]
    [TestData(15, 115)]
    [TestData(16, 116)]
    [TestData(17, 117)]
    [TestData(18, 118)]
    [TestData(19, 119)]
    public async Task Parallel_Mixed_Test(int cpuValue, int ioValue)
    {
        var cpuResult = PerformCPUWork(cpuValue);
        await Task.Yield();
        var ioResult = await PerformIOWorkAsync(ioValue);
        var combined = $"{cpuResult}_{ioResult}";
    }

    private int PerformCPUWork(int taskId)
    {
        var sum = 0;
        for (var i = 1; i <= 100; i++)
        {
            sum += (i * taskId) % 1000;
        }
        return sum;
    }

    private int CalculateExpectedResult(int taskId)
    {
        var sum = 0;
        for (var i = 1; i <= 100; i++)
        {
            sum += (i * taskId) % 1000;
        }
        return sum;
    }

    private async Task<string> PerformIOWorkAsync(int taskId)
    {
        await Task.Yield();
        return $"Task_{taskId}_Result_{Guid.NewGuid().ToString()[..8]}";
    }
}
