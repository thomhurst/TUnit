# Exception Handling

When a test fails, TUnit throws an exception. Most of the time you don't need to think about this — the framework handles it and reports the failure. But there are a couple of exception types you might want to throw yourself.

## Skipping a test at runtime

If a test can't run because of some runtime condition, throw `SkipTestException`. The test will be reported as skipped rather than failed.

```csharp
[Test]
public async Task RequiresExternalService()
{
    if (!await IsServiceAvailable())
    {
        throw new SkipTestException("Service is not available");
    }

    // Test logic
}
```

## Marking a test as inconclusive

If a test can't determine a pass/fail result, throw `InconclusiveTestException`.

```csharp
[Test]
public async Task CheckFeatureFlag()
{
    var flag = await GetFeatureFlag("new-checkout");

    if (flag is null)
    {
        throw new InconclusiveTestException("Feature flag not configured");
    }

    // Test logic
}
```

## Checking for failure in teardown

In an `[After(Test)]` hook, you can check whether the test failed via `TestContext`:

```csharp
[After(Test)]
public async Task TakeScreenshotOnFailure(TestContext context)
{
    if (context.Result?.Exception is not null)
    {
        await SaveScreenshot(context.TestDetails.TestName);
    }
}
```
