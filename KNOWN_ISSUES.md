# Known Issues in TUnit

## NotInParallel with Multiple Constraint Keys

**Issue**: Tests with multiple `NotInParallel` constraint keys may run in parallel when they shouldn't.

**Example**:
```csharp
[Test, NotInParallel(["GroupD", "GroupE"])]
public async Task Test1() { }

[Test, NotInParallel(["GroupD", "GroupF"])]  
public async Task Test2() { }
```

Test1 and Test2 share "GroupD" and should not run in parallel, but they might.

**Root Cause**: 
The current implementation adds tests with multiple keys to separate queues for each key. Each queue is processed independently in parallel. This means:
- GroupD queue will run Test1 and Test2 sequentially
- But GroupE queue (processing Test1) and GroupF queue (processing Test2) may run concurrently
- There's no cross-queue coordination to prevent tests sharing any constraint from overlapping

**Workaround**:
- Use single constraint keys per test
- Or group related tests in the same test class with a class-level `NotInParallel` attribute

**Fix Required**:
The scheduler needs to track running tests across all queues and check for shared constraints before starting any test. This requires significant changes to the scheduling algorithm in `TestScheduler.cs` and `TestGroupingService.cs`.

## Assembly-Level Hooks Affecting Unrelated Tests

**Issue**: Assembly-level hooks (e.g., `[AfterEvery(Assembly)]`) run for ALL tests in the assembly, which can cause unexpected failures when hooks from test-specific scenarios affect other tests.

**Workaround**: 
- Avoid using assembly-level hooks in test files that intentionally throw exceptions
- Or add proper filtering in the hooks to only run for specific test namespaces/classes