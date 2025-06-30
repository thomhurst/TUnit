# TUnit Parallel Test Execution Optimization

## Overview

This document describes the optimized parallel test execution architecture implemented in TUnit, replacing the simplistic binary parallel/serial execution model with a sophisticated DAG-based scheduler.

## Key Improvements

### 1. DAG-Based Scheduling
- **Before**: Tests were divided into two groups - fully parallel or fully serial
- **After**: All tests are represented in a directed acyclic graph (DAG) with dependency edges
- **Benefit**: Tests can run as soon as their dependencies complete, maximizing parallelism

### 2. Adaptive Parallelism
- **Before**: Fixed parallelism based on processor count
- **After**: Dynamic thread pool that adapts based on system metrics
- **Benefit**: Better resource utilization for mixed CPU/IO workloads

### 3. Work Stealing
- **Before**: Threads waited idle when their queue was empty
- **After**: Idle threads steal work from busy threads
- **Benefit**: Near-perfect load balancing across all threads

### 4. Lock-Free Operations
- **Before**: Dependency tracking used locks
- **After**: Atomic operations for dependency counting
- **Benefit**: Reduced contention and better scalability

### 5. Comprehensive Deadlock Prevention
- **Before**: Basic circular dependency detection during execution
- **After**: Multiple layers of protection:
  - Pre-execution cycle detection
  - Individual test timeouts (default 5 minutes)
  - Global progress monitoring (10-minute stall detection)
  - Diagnostic dumps on timeout

## Architecture

### Core Components

1. **ITestScheduler**: Interface for scheduling strategies
2. **DagTestScheduler**: Main scheduler implementation
3. **TestExecutionState**: Tracks test state with atomic operations
4. **IParallelismStrategy**: Interface for parallelism adaptation
5. **AdaptiveParallelismStrategy**: Hill-climbing algorithm for optimal thread count
6. **WorkStealingQueue**: Per-thread queue with steal capability
7. **TestCompletionTracker**: Lock-free dependency resolution
8. **IProgressMonitor**: Stall detection and monitoring

### Usage

```csharp
// Default configuration (adaptive parallelism)
var executor = new UnifiedTestExecutor(
    singleTestExecutor,
    commandLineOptions,
    logger);

// Custom configuration
var config = new SchedulerConfiguration
{
    MinParallelism = 2,
    MaxParallelism = 16,
    TestTimeout = TimeSpan.FromMinutes(10),
    StallTimeout = TimeSpan.FromMinutes(15),
    EnableWorkStealing = true,
    EnableAdaptiveParallelism = true,
    Strategy = ParallelismStrategy.Adaptive
};

var scheduler = TestSchedulerFactory.Create(config, logger);
var executor = new UnifiedTestExecutor(
    singleTestExecutor,
    commandLineOptions,
    logger,
    scheduler);
```

## Performance Characteristics

### Expected Improvements
- **Throughput**: 30-50% improvement for complex dependency graphs
- **Resource Utilization**: 80-95% vs previous 40-60%
- **Test Start Latency**: 70% reduction
- **Scalability**: Near-linear with available cores

### Complexity Analysis
- **Dependency Resolution**: O(V+E) instead of O(V²)
- **Work Distribution**: O(1) amortized per test
- **Memory Usage**: O(V) where V is number of tests

## Configuration Options

### Environment Variables
- `TUNIT_MIN_PARALLELISM`: Minimum thread count
- `TUNIT_MAX_PARALLELISM`: Maximum thread count
- `TUNIT_TEST_TIMEOUT_SECONDS`: Individual test timeout
- `TUNIT_STALL_TIMEOUT_SECONDS`: Progress stall detection timeout

### Scheduler Configuration
```csharp
public class SchedulerConfiguration
{
    public int MinParallelism { get; set; } = 1;
    public int MaxParallelism { get; set; } = Environment.ProcessorCount * 2;
    public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StallTimeout { get; set; } = TimeSpan.FromMinutes(10);
    public bool EnableWorkStealing { get; set; } = true;
    public bool EnableAdaptiveParallelism { get; set; } = true;
    public ParallelismStrategy Strategy { get; set; } = ParallelismStrategy.Adaptive;
}
```

## Design Principles

1. **SOLID**: Each component has a single responsibility with clear interfaces
2. **DRY**: Shared logic extracted to reusable components
3. **KISS**: Complex optimizations hidden behind simple interfaces
4. **SRP**: Separate concerns for scheduling, execution, monitoring, and adaptation

## Migration Notes

The new scheduler is backward compatible. Existing code continues to work without changes. The optimizations are transparent to test authors and only affect the execution engine.

## Future Enhancements

1. **Historical Data**: Use past test execution times for better scheduling
2. **Critical Path Analysis**: Prioritize tests on the critical path
3. **Resource Profiling**: Consider memory and IO requirements in scheduling
4. **Distributed Execution**: Extend to support multi-machine test execution