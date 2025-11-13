# Bug: Constructor Called Twice Non-Deterministically with SharedType.PerTestSession

## Issue Type
Bug - High Severity

## Description
When using `ClassDataSourceAttribute` with `SharedType.PerTestSession`, constructors are sometimes called twice non-deterministically. This issue occurs specifically when `InitializeAsync` and resource creation operations are time-consuming.

## Related Discussion
- Original discussion: https://github.com/thomhurst/TUnit/discussions/3803
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
