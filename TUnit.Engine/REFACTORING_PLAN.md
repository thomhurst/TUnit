# TUnit.Engine Refactoring Plan

## Overview
This document outlines a phased approach to address code quality issues identified in the TUnit.Engine project. Each phase focuses on specific categories of issues, ordered by priority and risk.

## Phase 1: Critical Async/Await Issues (HIGH PRIORITY)
**Goal**: Eliminate deadlock risks and improve async code patterns

### Tasks:
1. **OrderedConstraintTestScheduler.cs**
   - Line 221: Replace `.Result` with proper async/await in `CreatePriorityGroupedTests`
   - Make the method async and propagate changes up the call chain

2. **Search and fix all .Result and GetAwaiter().GetResult() calls**
   - Files to check:
     - SingleTestExecutor.cs
     - TestDiscoveryServiceV2.cs
     - HookOrchestratingTestExecutorAdapter.cs
     - FailFastTestExecutorAdapter.cs
     - TestExecutorAdapter.cs

### Verification:
- Run all tests to ensure no deadlocks
- Verify async operations complete properly
- Check for any new compiler warnings

## Phase 2: Resource Disposal Issues (HIGH PRIORITY)
**Goal**: Properly dispose all IDisposable resources

### Tasks:
1. **UnifiedTestExecutor.cs**
   - Implement IDisposable/IAsyncDisposable
   - Dispose `_failFastCancellationSource` properly
   - Add disposal in finalizer as safety net

2. **Review all CancellationTokenSource usages**
   - Ensure proper disposal in all classes
   - Use `using` statements where appropriate
   - Consider pooling for frequently created instances

3. **Add disposal patterns to:**
   - OrderedConstraintTestScheduler.cs
   - ConstraintAwareTestScheduler.cs
   - DagTestScheduler.cs

### Verification:
- Use memory profiler to check for leaks
- Verify disposal in unit tests

## Phase 3: Code Duplication Refactoring (MEDIUM PRIORITY)
**Goal**: Eliminate repetitive code patterns

### Tasks:
1. **ConsoleInterceptor.cs**
   - Extract common Write logic to a single method
   - Create generic Write<T> helper method
   - Reduce 500+ lines to ~100 lines

2. **Pattern to extract:**
   ```csharp
   private void WriteCore<T>(T value, Action<TextWriter, T> writeAction)
   {
       if (!commandLineOptions.IsOptionSet(HideTestOutputCommandProvider.HideTestOutput))
       {
           writeAction(GetOriginalOut(), value);
       }
       if (RedirectedOut != null)
       {
           writeAction(RedirectedOut, value);
       }
   }
   ```

### Verification:
- Ensure console output behavior unchanged
- Test with hidden output flag

## Phase 4: Exception Handling Improvements (MEDIUM PRIORITY)
**Goal**: Implement specific exception handling and proper error propagation

### Tasks:
1. **Replace generic catch blocks**
   - Identify specific exceptions each method can throw
   - Add specific catch blocks for expected exceptions
   - Log and rethrow unexpected exceptions

2. **Priority files:**
   - TestBuilder.cs
   - SingleTestExecutor.cs
   - HookOrchestrator.cs
   - EventReceiverOrchestrator.cs

3. **Add exception documentation**
   - Document which exceptions methods can throw
   - Add XML documentation for public methods

### Verification:
- Test exception scenarios
- Verify proper error messages in logs

## Phase 5: Method Decomposition (LOW PRIORITY)
**Goal**: Break down large methods into smaller, focused methods

### Tasks:
1. **TestBuilder.cs**
   - Extract object creation logic from `CreateFailedTestForDataGenerationError`
   - Create builder methods for complex objects

2. **UnifiedTestExecutor.cs**
   - Split `ExecuteTests` into:
     - FilterTests
     - SetupHookOrchestration
     - ExecuteWithScheduler
     - HandleExecutionErrors

3. **HookOrchestrator.cs**
   - Extract state tracking logic
   - Create separate methods for each hook type

### Verification:
- Ensure behavior remains identical
- Add unit tests for extracted methods

## Phase 6: Performance Optimizations (LOW PRIORITY)
**Goal**: Improve performance in hot paths

### Tasks:
1. **String concatenation improvements**
   - Use StringBuilder in TestIdentifierService.cs
   - Cache frequently used string concatenations

2. **LINQ optimization**
   - Cache results of expensive LINQ queries
   - Use arrays instead of IEnumerable where possible
   - Avoid multiple enumeration

3. **Async improvements**
   - Remove unnecessary Task.Run calls
   - Use ValueTask where appropriate

### Verification:
- Benchmark before and after changes
- Profile memory allocations

## Implementation Notes

### General Guidelines:
1. **Testing**: Run full test suite after each change
2. **Commits**: Make atomic commits for each fix
3. **Reviews**: Consider breaking changes that affect public APIs
4. **Documentation**: Update XML docs as methods change

### Questions to Address:
1. Are there specific performance benchmarks we should meet?
2. Is backward compatibility required for all public APIs?
3. Are there specific coding standards beyond general C# conventions?
4. Should we add analyzers to prevent these issues in the future?

### Success Criteria:
- No .Result or GetAwaiter().GetResult() calls remain
- All IDisposable resources properly disposed
- Code duplication reduced by 70%+
- All generic exception handlers replaced
- Methods under 50 lines
- Performance improvements measurable

## Execution Order
1. Phase 1 & 2 (Critical) - Do simultaneously as they're high risk
2. Phase 3 (ConsoleInterceptor) - High impact, low risk
3. Phase 4 (Exception handling) - Medium risk, improves debuggability
4. Phase 5 & 6 (Refactoring) - Low priority, can be done incrementally