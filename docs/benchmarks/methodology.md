# Benchmark Methodology

This page explains how TUnit's performance benchmarks are conducted to ensure fair, accurate, and reproducible results.

## Core Principles[​](#core-principles "Direct link to Core Principles")

### 1. Real-World Scenarios[​](#1-real-world-scenarios "Direct link to 1. Real-World Scenarios")

Benchmarks test realistic patterns, not artificial micro-benchmarks:

* Actual assertion logic
* Real data source patterns
* Typical setup/teardown workflows
* Common parallelization strategies

### 2. Fair Comparison[​](#2-fair-comparison "Direct link to 2. Fair Comparison")

Every framework implements identical test logic:

* Same test methods
* Same data inputs
* Same assertion complexity
* Equivalent configuration

### 3. Statistical Rigor[​](#3-statistical-rigor "Direct link to 3. Statistical Rigor")

All benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/), the industry-standard .NET benchmarking library:

* Multiple iterations per benchmark
* Statistical outlier detection
* Warm-up phase excluded from measurements
* Standard deviation and median reported

## Test Categories[​](#test-categories "Direct link to Test Categories")

### Runtime Benchmarks[​](#runtime-benchmarks "Direct link to Runtime Benchmarks")

#### DataDrivenTests[​](#datadriventests "Direct link to DataDrivenTests")

**Purpose**: Measure parameterized test performance

**What's tested**:

```
[Test]

[Arguments(1, 2, 3)]

[Arguments(4, 5, 9)]

// ... 50 argument sets

public async Task TestAddition(int a, int b, int expected)

{

    await Assert.That(a + b).IsEqualTo(expected);

}
```

**Why it matters**: Most test suites use parameterized tests extensively.

***

#### AsyncTests[​](#asynctests "Direct link to AsyncTests")

**Purpose**: Measure async/await pattern performance

**What's tested**:

```
[Test]

public async Task TestAsyncOperation()

{

    var result = await SimulateAsyncWork();

    await Assert.That(result).IsNotNull();

}
```

**Why it matters**: Modern .NET is async-first.

***

#### ScaleTests[​](#scaletests "Direct link to ScaleTests")

**Purpose**: Measure scalability with large test counts

**What's tested**:

* 1000+ test methods
* Parallel execution
* Memory efficiency

**Why it matters**: Enterprise codebases have thousands of tests.

***

#### MatrixTests[​](#matrixtests "Direct link to MatrixTests")

**Purpose**: Measure combinatorial test generation

**What's tested**:

```
[Test]

[MatrixDataSource]

public async Task TestPermissions(

    [Matrix("Create", "Update", "Delete")] string op,

    [Matrix("User", "Admin", "Guest")] string role)

{

    // 3 × 3 = 9 test combinations

    await Task.CompletedTask;

}
```

**Why it matters**: Matrix testing is common for comprehensive coverage.

***

#### MassiveParallelTests[​](#massiveparalleltests "Direct link to MassiveParallelTests")

**Purpose**: Stress test parallel execution

**What's tested**:

* 100+ tests running concurrently
* Resource contention
* Thread safety

**Why it matters**: Parallel execution is TUnit's default behavior.

***

### Build Benchmarks[​](#build-benchmarks "Direct link to Build Benchmarks")

**Purpose**: Measure compilation time impact

**What's tested**:

* Clean build time
* Incremental build time
* Source generator overhead

**Why it matters**: Fast builds improve developer productivity.

## Environment[​](#environment "Direct link to Environment")

### Hardware[​](#hardware "Direct link to Hardware")

* **Platform**: GitHub Actions Ubuntu runners
* **Consistency**: Same hardware for all frameworks
* **Reproducibility**: Daily automated runs

### Software[​](#software "Direct link to Software")

* **Framework Versions**: Latest stable releases
* **.NET Version**: .NET 10 (latest)
* **OS**: Ubuntu Latest

### Configuration[​](#configuration "Direct link to Configuration")

* **Release Mode**: All tests compiled with optimizations
* **Native AOT**: Separate TUnit\_AOT benchmark
* **Default Settings**: No special framework configuration

## Measurement Process[​](#measurement-process "Direct link to Measurement Process")

### 1. Build Phase[​](#1-build-phase "Direct link to 1. Build Phase")

```
# Build all frameworks identically

dotnet build -c Release -p:TestFramework=TUNIT

dotnet build -c Release -p:TestFramework=XUNIT3

dotnet build -c Release -p:TestFramework=NUNIT

dotnet build -c Release -p:TestFramework=MSTEST
```

### 2. Execution Phase[​](#2-execution-phase "Direct link to 2. Execution Phase")

```
[Benchmark]

public async Task TUnit()

{

    await Cli.Wrap("UnifiedTests.exe")

        .WithArguments(["--filter", "TestCategory"])

        .ExecuteBufferedAsync();

}
```

### 3. Analysis Phase[​](#3-analysis-phase "Direct link to 3. Analysis Phase")

* BenchmarkDotNet collects metrics
* Statistical analysis performed
* Results exported to markdown
* Historical trends tracked

## What Gets Measured[​](#what-gets-measured "Direct link to What Gets Measured")

### Primary Metrics[​](#primary-metrics "Direct link to Primary Metrics")

#### Mean Execution Time[​](#mean-execution-time "Direct link to Mean Execution Time")

* **Definition**: Average time across all iterations
* **Unit**: Milliseconds (ms) or Seconds (s)
* **Lower is better**

#### Median Execution Time[​](#median-execution-time "Direct link to Median Execution Time")

* **Definition**: Middle value, less affected by outliers
* **Unit**: Milliseconds (ms) or Seconds (s)
* **More stable than mean**

#### Standard Deviation[​](#standard-deviation "Direct link to Standard Deviation")

* **Definition**: Measure of result consistency
* **Unit**: Same as mean
* **Lower is better** (more consistent)

### Derived Metrics[​](#derived-metrics "Direct link to Derived Metrics")

#### Speedup Factor[​](#speedup-factor "Direct link to Speedup Factor")

```
Speedup = (Other Framework Time) / (TUnit Time)
```

Example: "2.5x faster" means TUnit is 2.5 times faster.

#### AOT Improvement[​](#aot-improvement "Direct link to AOT Improvement")

```
AOT Speedup = (TUnit JIT Time) / (TUnit AOT Time)
```

Example: "4x faster with AOT" means Native AOT is 4 times faster than JIT.

## Benchmark Automation[​](#benchmark-automation "Direct link to Benchmark Automation")

### Daily Execution[​](#daily-execution "Direct link to Daily Execution")

Benchmarks run automatically every 24 hours via [GitHub Actions](https://github.com/thomhurst/TUnit/blob/main/.github/workflows/speed-comparison.yml).

### Process[​](#process "Direct link to Process")

1. **Build**: Compile all framework versions
2. **Execute**: Run benchmarks in isolated processes
3. **Analyze**: Parse BenchmarkDotNet output
4. **Publish**: Update documentation automatically
5. **Track**: Store historical trends

### Artifacts[​](#artifacts "Direct link to Artifacts")

All raw benchmark results are available as GitHub Actions artifacts for 90 days.

## Reproducibility[​](#reproducibility "Direct link to Reproducibility")

### Running Locally[​](#running-locally "Direct link to Running Locally")

```
# 1. Navigate to benchmark project

cd tools/speed-comparison



# 2. Build all frameworks

dotnet build -c Release



# 3. Run specific benchmark

cd Tests.Benchmark

dotnet run -c Release -- --filter "*RuntimeBenchmarks*"
```

### Viewing Results[​](#viewing-results "Direct link to Viewing Results")

Results are generated in `BenchmarkDotNet.Artifacts/results/`:

* Markdown reports (\*.md)
* CSV data (\*.csv)
* HTML reports (\*.html)

## Limitations & Caveats[​](#limitations--caveats "Direct link to Limitations & Caveats")

### What Benchmarks Don't Measure[​](#what-benchmarks-dont-measure "Direct link to What Benchmarks Don't Measure")

❌ **IDE Integration**: Benchmarks don't measure test discovery in IDEs

❌ **Debugger Performance**: Debug mode performance is not measured

❌ **Real I/O**: Most tests use in-memory operations to avoid I/O variance

❌ **External Dependencies**: No database, network, or file system calls

### Variance Factors[​](#variance-factors "Direct link to Variance Factors")

Results can vary based on:

* Hardware configuration
* Background processes
* OS scheduling
* .NET runtime version
* Test complexity

### Interpreting Results[​](#interpreting-results "Direct link to Interpreting Results")

* **Relative Performance**: Compare frameworks, not absolute times
* **Your Mileage May Vary**: Real-world results depend on test characteristics
* **Trends Matter More**: Watch for performance regressions over time

## Transparency[​](#transparency "Direct link to Transparency")

### Open Source[​](#open-source "Direct link to Open Source")

All benchmark code is open source:

* [Unified Test Suite](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison/UnifiedTests)
* [Benchmark Harness](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison/Tests.Benchmark)
* [CI Workflow](https://github.com/thomhurst/TUnit/blob/main/.github/workflows/speed-comparison.yml)

### Community Verification[​](#community-verification "Direct link to Community Verification")

Found an issue with the benchmarks? [Open an issue](https://github.com/thomhurst/TUnit/issues) or submit a PR!

***

## Further Reading[​](#further-reading "Direct link to Further Reading")

* [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
* [.NET Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/framework/performance/)
* [TUnit Performance Best Practices](/docs/guides/performance.md)
