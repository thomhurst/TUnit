# TUnit Discovery Safeguards

This document describes the safeguards implemented to prevent hanging and deadlocks during test discovery.

## Implemented Safeguards

### 1. Memory and Time-Based Resource Protection
**Location**: `DiscoveryCircuitBreaker`
**Issues Addressed**: Excessive resource consumption during test discovery

**Safeguards**:
- Memory-based limits (default: 70% of available memory)
- Time-based limits (default: 2 minutes)
- Intelligent resource monitoring
- Auto-scaling based on system capabilities (CI, container, CPU count)

### 2. Discovery Phase Timeout
**Location**: `TestDiscoveryService.DiscoverTests()`
**Issues Addressed**: Infinite hanging during test discovery

**Safeguards**:
- Overall discovery timeout (default: 5 minutes)
- Cancellation token propagation throughout discovery
- Configurable via environment variables

### 3. Dynamic Data Source Protection
**Location**: `DataSourceResolver.ResolveDynamicDataAsync()`
**Issues Addressed**: Hanging on blocking data source operations

**Safeguards**:
- Timeout for data source resolution (default: 30 seconds, auto-scaled)
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
- Data source timing monitoring
- Hang detection and reporting
- Environment variable control

## Configuration

### Environment Variables
- `TUNIT_DISCOVERY_DIAGNOSTICS=1` - Enable diagnostic logging
- `TUNIT_DISCOVERY_TIMEOUT_SECONDS` - Override discovery timeout
- `TUNIT_DATA_SOURCE_TIMEOUT_SECONDS` - Override data source timeout

### Default Limits
```csharp
DiscoveryTimeout = 30 seconds (auto-scaled based on system)
DataSourceTimeout = 30 seconds (auto-scaled based on system)
```

## Error Messages


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