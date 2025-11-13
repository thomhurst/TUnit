# GitHub Issues to Create for Discussion #3803

This document contains two separate issue reports based on the discussion thread at:
https://github.com/thomhurst/TUnit/discussions/3803#discussioncomment-14957214

## Instructions for Creating Issues

Due to API limitations, these issues need to be created manually. Use the content below for each issue.

---

## Issue 1: Constructor Called Twice Non-Deterministically with SharedType.PerTestSession

**Labels:** `bug`, `priority: high`, `area: test-data`

**Title:**
```
Constructor called twice non-deterministically with SharedType.PerTestSession
```

**Body:**
```markdown
## Description
When using `ClassDataSourceAttribute` with `SharedType.PerTestSession`, constructors are sometimes called twice non-deterministically. This issue occurs specifically when `InitializeAsync` and resource creation operations are time-consuming.

## Related Discussion
- Original discussion: #3803
- Specific comment: https://github.com/thomhurst/TUnit/discussions/3803#discussioncomment-14957214

## Current Behavior
- Constructors of test data source classes marked with `SharedType.PerTestSession` are being invoked multiple times
- The issue is **non-deterministic** - it doesn't happen every time
- The likelihood increases when:
  - `InitializeAsync` contains time-consuming operations
  - Container field initialization is performed
  - Lengthy initialization methods are run

## Expected Behavior
- Constructor should be called **exactly once** per test session when using `SharedType.PerTestSession`
- The shared instance should be reused across all tests in the session

## Impact
- **Port Binding Conflicts**: When constructors attempt to bind to the same port multiple times, tests fail
- **Resource Duplication**: Heavy resources (like test containers) are instantiated multiple times, wasting memory and time
- **Unpredictable Behavior**: Non-deterministic nature makes debugging difficult
- **Test Reliability**: Tests may pass or fail randomly depending on whether constructor is called once or twice

## Reproduction
As reported in the discussion:

> "Unfortunately, it seems to be non-deterministic. It works perfectly fine with your test examples, but as soon as InitializeAsync and the resource creation become time-consuming (for example, initializing a container field or running a lengthy initialization method), the constructor is sometimes called twice. This leads to an error in my case, since the actual code uses the same port binding."

The issue was confirmed by the reporter having to write to a file from the constructor to verify multiple invocations, since console output was not available (see related issue).

## Environment
- TUnit version: Reported in v1.1.0, but may affect other versions
- Test execution: Both Visual Studio and console
- Test configuration: Parallel test execution with `SharedType.PerTestSession` data sources

## Suggested Investigation Areas
1. Race condition in shared instance creation/caching mechanism
2. Timing issues in async initialization with `InitializeAsync`
3. Thread safety of the shared instance registry
4. Instance lifecycle management for `PerTestSession` scope

## Workaround
None identified - the issue is non-deterministic and difficult to work around.

## Additional Context
- The user is trying to share test containers (resource-heavy) across tests in a session while having separate `WebApplicationFactory` instances per test
- Tests run in parallel
- `WebApplicationFactory` is NOT using `SharedType.PerTestSession`, only the test containers are
```

---

## Issue 2: Constructor Console Output Not Captured in Test Output

**Labels:** `bug`, `enhancement`, `priority: medium`, `area: test-output`

**Title:**
```
Constructor console output not captured in test output
```

**Body:**
```markdown
## Description
Console output (e.g., `Console.WriteLine`) from constructors of test data source classes is not captured or shown in the test output. This makes debugging test data initialization issues extremely difficult.

## Related Discussion
- Original discussion: #3803
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
```

---

## How to Create These Issues

You can create these issues using either method:

### Method 1: GitHub Web Interface
1. Go to https://github.com/thomhurst/TUnit/issues/new
2. Copy the title and body for each issue from above
3. Add the suggested labels
4. Submit the issue

### Method 2: GitHub CLI (if authenticated)
```bash
# Issue 1
gh issue create \
  --repo thomhurst/TUnit \
  --title "Constructor called twice non-deterministically with SharedType.PerTestSession" \
  --body-file <(cat <<'EOF'
[paste body from above]
EOF
) \
  --label "bug,priority: high,area: test-data"

# Issue 2
gh issue create \
  --repo thomhurst/TUnit \
  --title "Constructor console output not captured in test output" \
  --body-file <(cat <<'EOF'
[paste body from above]
EOF
) \
  --label "bug,enhancement,priority: medium,area: test-output"
```

## Summary

Both issues stem from the same discussion thread where a user reported problems with `SharedType.PerTestSession` test data sources:

1. **Constructor Called Twice** - A race condition or timing issue causing duplicate initialization
2. **Constructor Output Lost** - Console output from constructors not being captured due to lack of test context

These issues are related but distinct and should be tracked separately for proper resolution.
