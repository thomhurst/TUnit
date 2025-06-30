# TUnit Performance Benchmarks

This project contains performance benchmarks comparing reflection-based test execution with the new reflection-free delegate-based approach.

## Benchmark Categories

### 1. Test Discovery Benchmarks
- Compares reflection-based test discovery vs pre-compiled metadata
- Measures filtering performance
- Tests category-based discovery

### 2. Test Execution Benchmarks
- Synchronous test execution
- Asynchronous test execution
- Parameterized test execution
- Test instance creation
- Full test lifecycle with hooks

### 3. Data Source Benchmarks
- Property data source access
- Method data source invocation
- Async data source handling
- Complex data expansion
- Tuple conversion performance

## Running the Benchmarks

### Standard .NET Runtime
```bash
cd TUnit.Performance.Tests
dotnet run -c Release
```

### Native AOT Comparison
```bash
# Build for AOT
dotnet publish -c Release -r win-x64 --self-contained -p:PublishAot=true

# Run the published executable
./bin/Release/net8.0/win-x64/publish/TUnit.Performance.Tests.exe
```

### Run Specific Benchmarks
```bash
# Run only discovery benchmarks
dotnet run -c Release -- --filter *Discovery*

# Run only execution benchmarks
dotnet run -c Release -- --filter *Execution*

# Run only data source benchmarks
dotnet run -c Release -- --filter *DataSource*
```

## Expected Results

### Test Discovery
- Delegate-based discovery should be 10-50x faster than reflection
- No allocations for pre-compiled metadata enumeration
- Constant time complexity for delegate lookup

### Test Execution
- Delegate invocation ~5-10x faster than reflection
- Significant reduction in allocations
- Better performance scaling with parameter count

### Data Sources
- Property access via delegates ~20x faster
- Method invocation ~10x faster
- Async overhead reduced by ~50%

## Interpreting Results

The benchmarks will output:
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Gen0/1/2**: Number of garbage collections
- **Allocated**: Memory allocated per operation

Lower numbers are better for all metrics.

## AOT Benefits

When running with Native AOT:
- Startup time reduced by 60-80%
- Memory footprint reduced by 40-60%
- Consistent performance (no JIT warmup)
- Smaller deployment size

## Notes

- Baseline is always the reflection-based approach
- Benchmarks use BenchmarkDotNet for accuracy
- Results vary based on hardware and .NET version
- AOT benchmarks require .NET 8.0 or later