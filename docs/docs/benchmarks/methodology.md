---
title: Benchmark Methodology
description: How we measure and compare test framework performance
sidebar_position: 3
---

# Benchmark Methodology

This document explains how TUnit's performance benchmarks are conducted to ensure fair, accurate, and reproducible results.

## Core Principles

### 1. Real-World Scenarios
We test realistic patterns, not artificial micro-benchmarks:
- Actual assertion logic
- Real data source patterns
- Typical setup/teardown workflows
- Common parallelization strategies

### 2. Fair Comparison
Every framework implements identical test logic:
- Same test methods
- Same data inputs
- Same assertion complexity
- Equivalent configuration

### 3. Statistical Rigor
We use [BenchmarkDotNet](https://benchmarkdotnet.org/), the industry-standard .NET benchmarking library:
- Multiple iterations per benchmark
- Statistical outlier detection
- Warm-up phase excluded from measurements
- Standard deviation and median reported

## Test Categories

### Runtime Benchmarks

#### DataDrivenTests
**Purpose**: Measure parameterized test performance

**What we test**:
```csharp
[Test]
[Arguments(1, 2, 3)]
[Arguments(4, 5, 9)]
// ... 50 argument sets
public void TestAddition(int a, int b, int expected)
{
    Assert.That(a + b).IsEqualTo(expected);
}
```

**Why it matters**: Most test suites use parameterized tests extensively.

---

#### AsyncTests
**Purpose**: Measure async/await pattern performance

**What we test**:
```csharp
[Test]
public async Task TestAsyncOperation()
{
    var result = await SimulateAsyncWork();
    await Assert.That(result).IsNotNull();
}
```

**Why it matters**: Modern .NET is async-first.

---

#### ScaleTests
**Purpose**: Measure scalability with large test counts

**What we test**:
- 1000+ test methods
- Parallel execution
- Memory efficiency

**Why it matters**: Enterprise codebases have thousands of tests.

---

#### MatrixTests
**Purpose**: Measure combinatorial test generation

**What we test**:
```csharp
[Test]
[Matrix("Create", "Update", "Delete")] // Operation
[Matrix("User", "Admin", "Guest")]     // Role
public void TestPermissions(string op, string role)
{
    // 9 test combinations
}
```

**Why it matters**: Matrix testing is common for comprehensive coverage.

---

#### MassiveParallelTests
**Purpose**: Stress test parallel execution

**What we test**:
- 100+ tests running concurrently
- Resource contention
- Thread safety

**Why it matters**: Parallel execution is TUnit's default behavior.

---

### Build Benchmarks

**Purpose**: Measure compilation time impact

**What we test**:
- Clean build time
- Incremental build time
- Source generator overhead

**Why it matters**: Fast builds improve developer productivity.

## Environment

### Hardware
- **Platform**: GitHub Actions Ubuntu runners
- **Consistency**: Same hardware for all frameworks
- **Reproducibility**: Daily automated runs

### Software
- **Framework Versions**: Latest stable releases
- **.NET Version**: .NET 10 (latest)
- **OS**: Ubuntu Latest

### Configuration
- **Release Mode**: All tests compiled with optimizations
- **Native AOT**: Separate TUnit_AOT benchmark
- **Default Settings**: No special framework configuration

## Measurement Process

### 1. Build Phase
```bash
# Build all frameworks identically
dotnet build -c Release -p:TestFramework=TUNIT
dotnet build -c Release -p:TestFramework=XUNIT3
dotnet build -c Release -p:TestFramework=NUNIT
dotnet build -c Release -p:TestFramework=MSTEST
```

### 2. Execution Phase
```csharp
[Benchmark]
public async Task TUnit()
{
    await Cli.Wrap("UnifiedTests.exe")
        .WithArguments(["--filter", "TestCategory"])
        .ExecuteBufferedAsync();
}
```

### 3. Analysis Phase
- BenchmarkDotNet collects metrics
- Statistical analysis performed
- Results exported to markdown
- Historical trends tracked

## What We Measure

### Primary Metrics

#### Mean Execution Time
- **Definition**: Average time across all iterations
- **Unit**: Milliseconds (ms) or Seconds (s)
- **Lower is better**

#### Median Execution Time
- **Definition**: Middle value, less affected by outliers
- **Unit**: Milliseconds (ms) or Seconds (s)
- **More stable than mean**

#### Standard Deviation
- **Definition**: Measure of result consistency
- **Unit**: Same as mean
- **Lower is better** (more consistent)

### Derived Metrics

#### Speedup Factor
```
Speedup = (Other Framework Time) / (TUnit Time)
```

Example: "2.5x faster" means TUnit is 2.5 times faster.

#### AOT Improvement
```
AOT Speedup = (TUnit JIT Time) / (TUnit AOT Time)
```

Example: "4x faster with AOT" means Native AOT is 4 times faster than JIT.

## Benchmark Automation

### Daily Execution
Benchmarks run automatically every 24 hours via [GitHub Actions](https://github.com/thomhurst/TUnit/blob/main/.github/workflows/speed-comparison.yml).

### Process
1. **Build**: Compile all framework versions
2. **Execute**: Run benchmarks in isolated processes
3. **Analyze**: Parse BenchmarkDotNet output
4. **Publish**: Update documentation automatically
5. **Track**: Store historical trends

### Artifacts
All raw benchmark results are available as GitHub Actions artifacts for 90 days.

## Reproducibility

### Running Locally

```bash
# 1. Navigate to benchmark project
cd tools/speed-comparison

# 2. Build all frameworks
dotnet build -c Release

# 3. Run specific benchmark
cd Tests.Benchmark
dotnet run -c Release -- --filter "*RuntimeBenchmarks*"
```

### Viewing Results
Results are generated in `BenchmarkDotNet.Artifacts/results/`:
- Markdown reports (*.md)
- CSV data (*.csv)
- HTML reports (*.html)

## Limitations & Caveats

### What Benchmarks Don't Measure

❌ **IDE Integration**: Benchmarks don't measure test discovery in IDEs

❌ **Debugger Performance**: Debug mode performance is not measured

❌ **Real I/O**: Most tests use in-memory operations to avoid I/O variance

❌ **External Dependencies**: No database, network, or file system calls

### Variance Factors

Results can vary based on:
- Hardware configuration
- Background processes
- OS scheduling
- .NET runtime version
- Test complexity

### Interpreting Results

- **Relative Performance**: Compare frameworks, not absolute times
- **Your Mileage May Vary**: Real-world results depend on test characteristics
- **Trends Matter More**: Watch for performance regressions over time

## Transparency

### Open Source
All benchmark code is open source:
- [Unified Test Suite](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison/UnifiedTests)
- [Benchmark Harness](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison/Tests.Benchmark)
- [CI Workflow](https://github.com/thomhurst/TUnit/blob/main/.github/workflows/speed-comparison.yml)

### Community Verification
Found an issue with our benchmarks? [Open an issue](https://github.com/thomhurst/TUnit/issues) or submit a PR!

---

## Further Reading

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [.NET Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
- [TUnit Performance Best Practices](/docs/advanced/performance-best-practices)

*Last updated: {new Date().toISOString().split('T')[0]}*
