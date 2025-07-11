# Phase 7: Console Output Buffering

## Overview
Console output operations in TUnit currently allocate tuples for every formatted write operation, causing significant overhead for tests with heavy console output. This phase implements buffered writing with pooled buffers to eliminate allocations.

## Problem Statement
- ConsoleInterceptor allocates tuples for every Write/WriteLine with format arguments
- Each console write operation triggers 5-10 allocations
- Tests with heavy console output suffer 30-50% performance penalty
- No buffering means every write is a syscall

## Solution Design

### 1. Buffered Console Writer
Create a buffered writer that coalesces multiple writes before flushing to the actual console.

**Key Components:**
- Internal buffer (char[] or StringBuilder from pool)
- Configurable buffer size (default 4KB)
- Auto-flush on buffer full, newline, or test completion
- Thread-safe implementation for parallel test execution

### 2. Eliminate Tuple Allocations
Replace tuple-based WriteCore implementation with direct parameter passing.

**Current (Allocating):**
```csharp
public override void Write(string format, object? arg0, object? arg1) 
    => WriteCore((format, arg0, arg1), (w, v) => w.Write(v.format, v.arg0, v.arg1));
```

**Optimized (Non-allocating):**
```csharp
public override void Write(string format, object? arg0, object? arg1)
{
    if (!verbosityService.HideTestOutput)
    {
        BufferedWrite(format, arg0, arg1);
    }
    // ... redirected output handling
}
```

### 3. StringBuilder Pooling
Implement a pool for StringBuilder instances used in formatting operations.

**Components:**
- Thread-local StringBuilder pool
- Automatic capacity management
- Clear and return pattern

### 4. Flush Strategy
Ensure output visibility at critical points:
- On WriteLine operations
- On test completion
- On buffer capacity reached
- On error/exception
- Configurable flush interval for long-running tests

## Implementation Steps

1. **Create BufferedTextWriter**
   - Wraps existing TextWriter with buffering
   - Implements efficient buffer management
   - Thread-safe for concurrent access

2. **Refactor ConsoleInterceptor**
   - Remove tuple allocations in WriteCore
   - Implement direct method overloads
   - Add buffer management

3. **Add StringBuilder Pool**
   - Create StringBuilderPool class
   - Implement rent/return pattern
   - Integrate with ConsoleInterceptor

4. **Implement Flush Points**
   - Hook into test lifecycle events
   - Add automatic flush on errors
   - Ensure visibility of critical output

5. **Configuration Options**
   - Buffer size configuration
   - Flush interval settings
   - Enable/disable buffering flag

## Performance Targets
- Eliminate tuple allocations (100% reduction)
- Reduce console write overhead by 80-90%
- Overall performance improvement: 30-50% for output-heavy tests
- Memory allocation reduction: 5-10 allocations per write → 0

## Testing Strategy
1. Unit tests for BufferedTextWriter
2. Thread-safety tests for parallel execution
3. Flush behavior verification
4. Performance benchmarks with output-heavy tests
5. Compatibility tests with existing console capture

## Risk Mitigation
- Ensure no output loss on test failures
- Maintain output ordering
- Handle edge cases (very long lines, binary output)
- Preserve existing behavior for redirected output

## Success Criteria
- Zero allocations for common console operations
- 30%+ performance improvement for output-heavy tests
- No regression in output visibility or ordering
- Maintains full backward compatibility