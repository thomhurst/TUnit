# TUnit Discovery Safeguards

This document describes the safeguards implemented to prevent hanging and deadlocks during test discovery.

## Implemented Safeguards

### 1. CartesianProduct Recursion Protection
**Location**: `TestFactory.CartesianProductWithLimits()`
**Issues Addressed**: Stack overflow from deep recursion, exponential test expansion

**Safeguards**:
- Maximum recursion depth (default: 100)
- Maximum total combinations (default: 100,000)
- Diagnostic logging of recursion depth and set counts
- Configurable limits via `DiscoveryConfiguration`

### 2. Discovery Phase Timeout
**Location**: `TestDiscoveryService.DiscoverTests()`
**Issues Addressed**: Infinite hanging during test discovery

**Safeguards**:
- Overall discovery timeout (default: 5 minutes)
- Cancellation token propagation throughout discovery
- Maximum test count per discovery session (default: 50,000)
- Configurable via environment variables

### 3. Dynamic Data Source Protection
**Location**: `DataSourceResolver.ResolveDynamicDataAsync()`
**Issues Addressed**: Hanging on blocking data source operations

**Safeguards**:
- Timeout for data source resolution (default: 30 seconds)
- Maximum items per data source (default: 10,000)
- Task.Run wrapper for reflection-based operations
- Cancellation token support for async data sources

### 4. Test Registry Optimization
**Location**: `TestRegistry`
**Issues Addressed**: Lock contention during parallel registration

**Existing Design**:
- Uses `ConcurrentDictionary` for lock-free operations
- Minimal locking only for singleton initialization
- Thread-safe registration methods

### 5. Discovery Diagnostics
**Location**: `DiscoveryDiagnostics`
**Issues Addressed**: Monitoring and detection of problematic patterns

**Features**:
- Event logging for key discovery operations
- Cartesian product depth tracking
- Data source timing monitoring
- Hang detection and reporting
- Environment variable control

## Configuration

### Environment Variables
- `TUNIT_DISCOVERY_DIAGNOSTICS=1` - Enable diagnostic logging
- `TUNIT_DISCOVERY_TIMEOUT_SECONDS` - Override discovery timeout
- `TUNIT_DATA_SOURCE_TIMEOUT_SECONDS` - Override data source timeout
- `TUNIT_MAX_TESTS` - Override maximum tests per discovery
- `TUNIT_MAX_COMBINATIONS` - Override maximum cartesian combinations
- `TUNIT_MAX_DATA_ITEMS` - Override maximum data source items

### Default Limits
```csharp
DiscoveryTimeout = 30 seconds
DataSourceTimeout = 30 seconds
MaxCartesianDepth = 100
MaxCartesianCombinations = 100,000
MaxTestsPerDiscovery = 50,000
MaxDataSourceItems = 10,000
```

## Error Messages

### CartesianProduct Depth Exceeded
```
Cartesian product exceeded maximum recursion depth of {maxDepth}. 
This may indicate an excessive number of data source combinations.
```

### Combinations Limit Exceeded
```
Cartesian product exceeded maximum combinations limit of {maxCombinations:N0}. 
Consider reducing the number of data sources or their sizes.
```

### Discovery Timeout
```
Test discovery timed out after {timeout} seconds. 
This may indicate an issue with data sources or excessive test generation.
```

### Data Source Timeout
```
Data source '{memberName}' on type '{typeName}' timed out after {timeout} seconds. 
Consider optimizing the data source or using cached data.
```

### Test Count Exceeded
```
Test discovery exceeded maximum test count of {maxTests:N0}. 
Consider reducing data source sizes or using test filters.
```

### Data Source Items Exceeded
```
Data source '{memberName}' exceeded maximum item count of {maxItems:N0}
```

## Monitoring

### Diagnostic Output
When `TUNIT_DISCOVERY_DIAGNOSTICS=1`:
- Event timestamps and thread IDs
- Data source start/end tracking
- Test expansion metrics
- Cartesian product depth analysis
- Potential hang warnings

### Hang Detection
- Console warnings for operations exceeding thresholds
- Diagnostic dump on discovery completion
- Analysis of incomplete operations

## Best Practices

1. **Keep Data Sources Manageable**: Limit data source sizes to reasonable numbers
2. **Use Test Filters**: Apply filters to reduce discovery scope when needed
3. **Monitor Diagnostics**: Enable diagnostics during development to catch issues early
4. **Configure Limits**: Adjust limits based on your testing needs
5. **Optimize Data Sources**: Cache expensive data generation operations
6. **Avoid Blocking Operations**: Keep data source methods fast and non-blocking

## Common Discovery Issues

### Not a Deadlock: Initialization Order Problems

The most common "hanging" issue is actually an initialization order problem:
- **Symptom**: Discovery appears to hang for the full timeout duration
- **Cause**: `TestRegistry.Instance` throws `InvalidOperationException` if not initialized
- **Solution**: The framework now provides a clear error message instead of timing out

### Other Potential Causes of Discovery Hangs

1. **Source Generator Issues**
   - Infinite loops or bugs in source generators during build
   - Malformed generated code causing runtime issues
   - Check build logs for source generator warnings

2. **Assembly Loading Problems**
   - Missing dependencies
   - Version conflicts
   - Bad image format (x86/x64 mismatch)
   - Custom AssemblyLoadContext issues

3. **Reflection-Based Discovery Issues**
   - Circular type dependencies
   - Problematic static constructors/properties
   - Performance bottlenecks with large assemblies

4. **Static Constructor Deadlocks**
   - Circular dependencies between static constructors
   - Self-deadlock from re-entrant static access
   - Use debugger to inspect thread call stacks

5. **Resource Issues**
   - I/O or network bottlenecks
   - Memory exhaustion
   - Excessive thread/handle creation
   - External process interaction problems

6. **Logging/Tracing Overhead**
   - Excessive synchronous logging
   - Slow or problematic logging sinks

### Debugging Discovery Issues

1. **Enable Diagnostics**: Set `TUNIT_DISCOVERY_DIAGNOSTICS=1`
2. **Attach Debugger**: Inspect thread call stacks during hang
3. **Check Assembly Loading**: Enable assembly binding logging
4. **Profile Performance**: Use profilers to find hotspots
5. **Monitor Resources**: Check memory, CPU, and handle usage

## Existing Safeguards

The following safeguards were already present in TUnit:
- Circular dependency detection in test execution
- Property data combination iteration limits
- Proper async/await patterns (no `.Result` or `.Wait()`)
- CancellationToken support throughout the stack