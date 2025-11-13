# Bug: Constructor Console Output Not Captured in Test Output

## Issue Type
Bug / Enhancement - Medium Severity

## Description
Console output (e.g., `Console.WriteLine`) from constructors of test data source classes is not captured or shown in the test output. This makes debugging test data initialization issues extremely difficult.

## Related Discussion
- Original discussion: https://github.com/thomhurst/TUnit/discussions/3803
- Specific comment: https://github.com/thomhurst/TUnit/discussions/3803#discussioncomment-14957214

## Current Behavior
- `Console.WriteLine` and other console output from data source constructors is not visible in test output
- Output disappears regardless of execution environment (Visual Studio or console)
- This is especially problematic when `InitializeAsync` takes a long time (reproducible with `await Task.Delay(...)`)
- Users have to resort to workarounds like writing to files to debug constructor behavior

## Expected Behavior
- Console output from test data source constructors should be captured and displayed in test output
- OR: Provide an alternative logging mechanism that works during test data construction
- OR: Redirect constructor output to test context once available

## Root Cause (as identified by maintainer)
From the discussion:

> "Test data is constructed before tests so doesn't have a test context available yet to redirect output to when you're in the constructor. I'll have a think and see if there's a way around that."

The issue occurs because:
1. Test data construction happens **before** test execution begins
2. No test context exists yet to redirect output to
3. Constructor output has nowhere to go

## Impact
- **Debugging Difficulty**: Cannot easily debug test data initialization issues
- **Poor Developer Experience**: Must resort to file-based logging or other workarounds
- **Delayed Issue Detection**: Silent failures in constructors are harder to detect
- **Verification Challenges**: As reported, users had to write to files to confirm constructor was being called multiple times

## Real-World Example
From the discussion, the user needed to verify that constructors were being called twice, but:

> "...to confirm that the constructor was being called multiple times, I had to write to a file from within the constructor in my real scenario, because no information from `Console.WriteLine` was available."

## Reproduction
1. Create a test data source class with `ClassDataSourceAttribute`
2. Add `Console.WriteLine` statements in the constructor
3. Optionally add `await Task.Delay(5000)` in `InitializeAsync` to make timing more observable
4. Run tests from Visual Studio or console
5. Observe that constructor output is not shown in test results

## Environment
- TUnit version: Affects multiple versions including v1.1.0
- Test execution: Both Visual Studio and console runners
- Scope: All test data source constructors

## Possible Solutions
1. **Buffer Output**: Capture console output during data source construction and replay it when test context becomes available
2. **Alternative Logging**: Provide a static logger available during construction phase that's displayed later
3. **Deferred Construction**: Delay data source construction until test context is available (may have other implications)
4. **Test Context Injection**: Make a lightweight test context available earlier in the lifecycle
5. **Documentation**: At minimum, document this limitation and recommended workarounds

## Workaround
Currently, users must:
- Write to files from constructors for debugging
- Use external logging mechanisms
- Avoid relying on console output for constructor diagnostics

## Additional Context
- This issue becomes more critical when combined with the "constructor called twice" bug, as users cannot easily verify the issue without file-based logging
- Affects all test data sources, not just `SharedType.PerTestSession`
- Related to test lifecycle and output redirection mechanisms
