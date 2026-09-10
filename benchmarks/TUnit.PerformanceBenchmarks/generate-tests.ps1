# Generate test classes for TUnit.PerformanceBenchmarks
# Usage: .\generate-tests.ps1 -Scale 1000

param(
    [int]$Scale = 1000
)

# Calculate test distribution (60% simple, 30% data-driven, 10% lifecycle)
$simpleTests = [math]::Floor($Scale * 0.6)
$dataDrivenTests = [math]::Floor($Scale * 0.3)
$lifecycleTests = [math]::Floor($Scale * 0.1)

Write-Host "Generating $Scale tests: $simpleTests simple, $dataDrivenTests data-driven, $lifecycleTests lifecycle"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Clean existing generated files
Get-ChildItem -Path "$scriptDir\Tests\Simple" -Filter "SimpleTests_*.cs" | Remove-Item -Force
Get-ChildItem -Path "$scriptDir\Tests\DataDriven" -Filter "DataDrivenTests_*.cs" | Remove-Item -Force
Get-ChildItem -Path "$scriptDir\Tests\Lifecycle" -Filter "LifecycleTests_*.cs" | Remove-Item -Force

# Generate simple tests (10 tests per class, 6 methods with 10 arguments each = 60 tests per class)
$simpleClassCount = [math]::Ceiling($simpleTests / 60)
for ($i = 1; $i -le $simpleClassCount; $i++) {
    $className = "SimpleTests_{0:D2}" -f $i
    $content = @"
namespace TUnit.PerformanceBenchmarks.Tests.Simple;

public class $className
{
    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    [Arguments(6), Arguments(7), Arguments(8), Arguments(9), Arguments(10)]
    public void Test_01(int v) { }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    [Arguments(6), Arguments(7), Arguments(8), Arguments(9), Arguments(10)]
    public void Test_02(int v) { }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    [Arguments(6), Arguments(7), Arguments(8), Arguments(9), Arguments(10)]
    public void Test_03(int v) { }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    [Arguments(6), Arguments(7), Arguments(8), Arguments(9), Arguments(10)]
    public void Test_04(int v) { }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    [Arguments(6), Arguments(7), Arguments(8), Arguments(9), Arguments(10)]
    public void Test_05(int v) { }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    [Arguments(6), Arguments(7), Arguments(8), Arguments(9), Arguments(10)]
    public void Test_06(int v) { }
}
"@
    $content | Out-File -FilePath "$scriptDir\Tests\Simple\$className.cs" -Encoding utf8
}

# Generate data-driven tests (10 tests per class using MethodDataSource)
$dataDrivenClassCount = [math]::Ceiling($dataDrivenTests / 30)
for ($i = 1; $i -le $dataDrivenClassCount; $i++) {
    $className = "DataDrivenTests_{0:D2}" -f $i
    $content = @"
namespace TUnit.PerformanceBenchmarks.Tests.DataDriven;

public class $className
{
    public static IEnumerable<(int, string)> TestData()
    {
        for (int i = 0; i < 10; i++)
            yield return (i, "Value" + i);
    }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public void DataTest_01((int num, string str) data) { _ = data.num + data.str.Length; }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public void DataTest_02((int num, string str) data) { _ = data.num + data.str.Length; }

    [Test]
    [MethodDataSource(nameof(TestData))]
    public void DataTest_03((int num, string str) data) { _ = data.num + data.str.Length; }
}
"@
    $content | Out-File -FilePath "$scriptDir\Tests\DataDriven\$className.cs" -Encoding utf8
}

# Generate lifecycle tests (with hooks)
$lifecycleClassCount = [math]::Ceiling($lifecycleTests / 10)
for ($i = 1; $i -le $lifecycleClassCount; $i++) {
    $className = "LifecycleTests_{0:D2}" -f $i
    $content = @"
using TUnit.Core.Interfaces;

namespace TUnit.PerformanceBenchmarks.Tests.Lifecycle;

public class $className : IAsyncInitializer, IAsyncDisposable
{
    private int _initialized;

    public Task InitializeAsync()
    {
        _initialized = 1;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _initialized = 0;
        return ValueTask.CompletedTask;
    }

    [Before(Test)]
    public void BeforeEach() { _ = _initialized; }

    [After(Test)]
    public void AfterEach() { _ = _initialized; }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    public void LifecycleTest_01(int v) { _ = v + _initialized; }

    [Test]
    [Arguments(1), Arguments(2), Arguments(3), Arguments(4), Arguments(5)]
    public void LifecycleTest_02(int v) { _ = v + _initialized; }
}
"@
    $content | Out-File -FilePath "$scriptDir\Tests\Lifecycle\$className.cs" -Encoding utf8
}

Write-Host "Generated $simpleClassCount simple classes, $dataDrivenClassCount data-driven classes, $lifecycleClassCount lifecycle classes"
Write-Host "Total approximate test count: $($simpleClassCount * 60 + $dataDrivenClassCount * 30 + $lifecycleClassCount * 10)"
