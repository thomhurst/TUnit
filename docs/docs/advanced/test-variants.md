# Test Variants

Test variants enable you to dynamically create additional test cases during test execution based on runtime results. This powerful feature unlocks advanced testing patterns like property-based testing shrinking, mutation testing, adaptive stress testing, and intelligent retry strategies.

## What Are Test Variants?

Test variants are tests that are created **during the execution** of a parent test, inheriting the parent's test method template but potentially using different arguments, properties, or display names. They appear as distinct tests in the test explorer and can have their own outcomes.

### Test Variants vs Dynamic Tests

| Feature | Test Variants (`CreateTestVariant`) | Dynamic Tests (`AddDynamicTest`) |
|---------|-------------------------------------|----------------------------------|
| **Created** | During test execution | During test discovery |
| **Parent** | Always has a parent test | Standalone tests |
| **Template** | Reuses parent's test method | Requires explicit method definition |
| **Use Case** | Runtime adaptation (shrinking, mutation, stress) | Pre-generation of test cases |
| **AOT Compatible** | No (requires reflection) | Yes (with source generators) |

## Core Concepts

### TestRelationship Enum

The `TestRelationship` enum categorizes how a variant relates to its parent, informing the test runner about execution semantics:

```csharp
public enum TestRelationship
{
    None,       // Independent test (no parent)
    Retry,      // Identical re-run after failure
    Generated,  // Pre-execution exploration (e.g., initial PBT cases)
    Derived     // Post-execution analysis (e.g., shrinking, mutation)
}
```

**When to use each:**
- **`Retry`**: For identical re-runs, typically handled by `[Retry]` attribute
- **`Generated`**: For upfront test case generation before execution
- **`Derived`**: For runtime analysis based on parent results (most common for variants)

### DisplayName Parameter

The optional `displayName` parameter provides user-facing labels in test explorers and reports. While the `TestRelationship` informs the framework about execution semantics, `displayName` communicates intent to humans:

```csharp
await context.CreateTestVariant(
    arguments: new object[] { smallerInput },
    relationship: TestRelationship.Derived,
    displayName: "Shrink Attempt #3"  // Shows in test explorer
);
```

### Properties Dictionary

Store metadata for filtering, reporting, or variant logic:

```csharp
await context.CreateTestVariant(
    arguments: new object[] { mutatedValue },
    properties: new Dictionary<string, object?>
    {
        { "AttemptNumber", 3 },
        { "ShrinkStrategy", "Binary" },
        { "OriginalValue", originalInput }
    },
    relationship: TestRelationship.Derived,
    displayName: "Shrink #3 (Binary)"
);
```

## Use Cases

### 1. Property-Based Testing (PBT) - Shrinking

When a property-based test fails with a complex input, create variants with progressively simpler inputs to find the minimal failing case. Use a custom attribute implementing `ITestEndEventReceiver` to automatically shrink on failure:

```csharp
// Custom attribute that shrinks inputs on test failure
public class ShrinkOnFailureAttribute : Attribute, ITestEndEventReceiver
{
    private readonly int _maxAttempts;

    public ShrinkOnFailureAttribute(int maxAttempts = 5)
    {
        _maxAttempts = maxAttempts;
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        // Only shrink if test failed and it's not already a shrink attempt
        if (testContext.Result?.Status != TestStatus.Failed)
            return;

        if (testContext.Relationship == TestRelationship.Derived)
            return; // Don't shrink shrink attempts

        // Get the test's numeric argument to shrink
        var args = testContext.Metadata.TestDetails.TestMethodArguments;
        if (args.Length == 0 || args[0] is not int size)
            return;

        if (size <= 1)
            return; // Can't shrink further

        // Create shrink variants
        var shrinkSize = size / 2;
        for (int attempt = 1; attempt <= _maxAttempts && shrinkSize > 0; attempt++)
        {
            await testContext.CreateTestVariant(
                arguments: new object[] { shrinkSize },
                properties: new Dictionary<string, object?>
                {
                    { "AttemptNumber", attempt },
                    { "OriginalSize", size },
                    { "ShrinkStrategy", "Binary" }
                },
                relationship: TestRelationship.Derived,
                displayName: $"Shrink #{attempt} (size={shrinkSize})"
            );

            shrinkSize /= 2;
        }
    }
}

// Usage: Just add the attribute - shrinking happens automatically on failure
[Test]
[ShrinkOnFailure(maxAttempts: 5)]
[Arguments(1000)]
[Arguments(500)]
[Arguments(100)]
public async Task PropertyTest_ListReversal(int size)
{
    var list = Enumerable.Range(0, size).ToList();

    // Property: reversing twice should return original
    var reversed = list.Reverse().Reverse().ToList();
    await Assert.That(reversed).IsEquivalentTo(list);

    // If this fails, the attribute automatically creates shrink variants
}
```

**Why this pattern is better:**
- **Separation of concerns**: Test logic stays clean, shrinking is in the attribute
- **Reusable**: Apply `[ShrinkOnFailure]` to any test with numeric inputs
- **Declarative**: Intent is clear from the attribute
- **Automatic**: No try-catch or manual failure detection needed

### 2. Mutation Testing

Create variants that test your test's ability to catch bugs by introducing controlled mutations:

```csharp
[Test]
[Arguments(5, 10)]
public async Task CalculatorTest_Addition(int a, int b)
{
    var context = TestContext.Current!;
    var calculator = new Calculator();

    var result = calculator.Add(a, b);
    await Assert.That(result).IsEqualTo(a + b);

    // After test passes, create mutants to verify test quality
    var mutations = new[]
    {
        (a + 1, b, "Mutant: Boundary +1 on first arg"),
        (a, b + 1, "Mutant: Boundary +1 on second arg"),
        (a - 1, b, "Mutant: Boundary -1 on first arg"),
        (0, 0, "Mutant: Zero case")
    };

    foreach (var (mutA, mutB, name) in mutations)
    {
        await context.CreateTestVariant(
            arguments: new object[] { mutA, mutB },
            relationship: TestRelationship.Derived,
            displayName: name
        );
    }
}
```

### 3. Adaptive Stress Testing

Progressively increase load based on system performance:

```csharp
[Test]
[Arguments(10)] // Start with low load
public async Task LoadTest_ApiEndpoint(int concurrentUsers)
{
    var context = TestContext.Current!;
    var stopwatch = Stopwatch.StartNew();

    // Simulate load
    var tasks = Enumerable.Range(0, concurrentUsers)
        .Select(_ => CallApiAsync())
        .ToArray();

    await Task.WhenAll(tasks);
    stopwatch.Stop();

    var avgResponseTime = stopwatch.ElapsedMilliseconds / (double)concurrentUsers;
    context.WriteLine($"Users: {concurrentUsers}, Avg response: {avgResponseTime}ms");

    // If system handled load well, increase it
    if (avgResponseTime < 200 && concurrentUsers < 1000)
    {
        var nextLoad = concurrentUsers * 2;
        await context.CreateTestVariant(
            arguments: new object[] { nextLoad },
            properties: new Dictionary<string, object?>
            {
                { "PreviousLoad", concurrentUsers },
                { "PreviousAvgResponseTime", avgResponseTime }
            },
            relationship: TestRelationship.Derived,
            displayName: $"Load Test ({nextLoad} users)"
        );
    }

    await Assert.That(avgResponseTime).IsLessThan(500);
}
```

### 4. Exploratory Fuzzing

Generate additional test cases when edge cases are discovered:

```csharp
[Test]
[Arguments("normal text")]
public async Task InputValidation_SpecialCharacters(string input)
{
    var context = TestContext.Current!;
    var validator = new InputValidator();

    var result = validator.Validate(input);
    await Assert.That(result.IsValid).IsTrue();

    // If we haven't tested special characters yet, generate variants
    if (!context.ObjectBag.ContainsKey("TestedSpecialChars"))
    {
        context.ObjectBag["TestedSpecialChars"] = true;

        var specialInputs = new[]
        {
            "<script>alert('xss')</script>",
            "'; DROP TABLE users; --",
            "../../../etc/passwd",
            "\0\0\0null bytes\0",
            new string('A', 10000) // Buffer overflow attempt
        };

        foreach (var specialInput in specialInputs)
        {
            await context.CreateTestVariant(
                arguments: new object[] { specialInput },
                relationship: TestRelationship.Derived,
                displayName: $"Fuzz: {specialInput.Substring(0, Math.Min(30, specialInput.Length))}"
            );
        }
    }
}
```

### 5. Smart Retry with Parameter Adjustment

Retry failed tests with adjusted parameters to differentiate transient failures from persistent bugs:

```csharp
[Test]
[Arguments(TimeSpan.FromSeconds(5))]
public async Task ExternalService_WithTimeout(TimeSpan timeout)
{
    var context = TestContext.Current!;

    try
    {
        using var cts = new CancellationTokenSource(timeout);
        var result = await _externalService.FetchDataAsync(cts.Token);
        await Assert.That(result).IsNotNull();
    }
    catch (TimeoutException ex)
    {
        // If timeout, try with longer timeout to see if it's a transient issue
        if (timeout < TimeSpan.FromSeconds(30))
        {
            var longerTimeout = timeout.Add(TimeSpan.FromSeconds(5));

            await context.CreateTestVariant(
                arguments: new object[] { longerTimeout },
                properties: new Dictionary<string, object?>
                {
                    { "OriginalTimeout", timeout },
                    { "RetryReason", "Timeout" }
                },
                relationship: TestRelationship.Derived,
                displayName: $"Retry with {longerTimeout.TotalSeconds}s timeout"
            );
        }

        throw;
    }
}
```

### 6. Chaos Engineering

Inject faults and verify system resilience:

```csharp
[Test]
public async Task Resilience_DatabaseFailover()
{
    var context = TestContext.Current!;
    var system = new DistributedSystem();

    // Normal operation test
    var result = await system.ProcessRequestAsync();
    await Assert.That(result.Success).IsTrue();

    // Create chaos variants
    var chaosScenarios = new[]
    {
        ("primary-db-down", "Primary DB Failure"),
        ("network-latency-500ms", "High Network Latency"),
        ("replica-lag-10s", "Replica Lag"),
        ("cascading-failure", "Cascading Failure")
    };

    foreach (var (faultType, displayName) in chaosScenarios)
    {
        await context.CreateTestVariant(
            arguments: new object[] { faultType },
            properties: new Dictionary<string, object?>
            {
                { "ChaosType", faultType },
                { "InjectionPoint", "AfterSuccess" }
            },
            relationship: TestRelationship.Derived,
            displayName: $"Chaos: {displayName}"
        );
    }
}
```

## API Reference

### Method Signature

```csharp
public static async Task CreateTestVariant(
    this TestContext context,
    object?[]? arguments = null,
    Dictionary<string, object?>? properties = null,
    TestRelationship relationship = TestRelationship.Derived,
    string? displayName = null)
```

### Parameters

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `context` | `TestContext` | Yes | - | The current test context |
| `arguments` | `object?[]?` | No | `null` | Method arguments for the variant. If `null`, reuses parent's arguments |
| `properties` | `Dictionary<string, object?>?` | No | `null` | Custom metadata stored in the variant's `TestContext.ObjectBag` |
| `relationship` | `TestRelationship` | No | `Derived` | Categorizes the variant's relationship to its parent |
| `displayName` | `string?` | No | `null` | User-facing label shown in test explorers. If `null`, uses default format |

### Return Value

Returns `Task` that completes when the variant has been queued for execution.

### Exceptions

- `InvalidOperationException`: Thrown if `TestContext.Current` is null
- `InvalidOperationException`: Thrown if the test method cannot be resolved

## Best Practices

### 1. Choose Appropriate TestRelationship

```csharp
// ✅ Good: Derived for post-execution analysis
await context.CreateTestVariant(
    arguments: [smallerInput],
    relationship: TestRelationship.Derived,
    displayName: "Shrink Attempt"
);

// ❌ Bad: Using None loses parent relationship
await context.CreateTestVariant(
    arguments: [smallerInput],
    relationship: TestRelationship.None  // Parent link lost!
);
```

### 2. Provide Descriptive Display Names

```csharp
// ✅ Good: Clear, specific, actionable
displayName: "Shrink #3 (Binary Search, size=125)"

// ⚠️ Okay: Somewhat clear
displayName: "Shrink Attempt 3"

// ❌ Bad: Vague, unhelpful
displayName: "Variant"
```

### 3. Avoid Infinite Recursion

```csharp
[Test]
public async Task RecursiveVariant()
{
    var context = TestContext.Current!;

    // ✅ Good: Check depth
    var depth = context.ObjectBag.TryGetValue("Depth", out var d) ? (int)d : 0;
    if (depth < 5)
    {
        await context.CreateTestVariant(
            properties: new Dictionary<string, object?> { { "Depth", depth + 1 } },
            relationship: TestRelationship.Derived
        );
    }

    // ❌ Bad: Infinite loop!
    // await context.CreateTestVariant(relationship: TestRelationship.Derived);
}
```

### 4. Use Properties for Metadata

```csharp
// ✅ Good: Structured metadata
properties: new Dictionary<string, object?>
{
    { "AttemptNumber", 3 },
    { "Strategy", "BinarySearch" },
    { "OriginalValue", largeInput },
    { "Timestamp", DateTime.UtcNow }
}

// ❌ Bad: Encoding metadata in displayName
displayName: "Attempt=3,Strategy=Binary,Original=1000,Time=2024-01-01"
```

### 5. Consider Performance

Creating many variants has overhead. Be strategic:

```csharp
// ✅ Good: Limited, strategic variants
if (shouldShrink && attemptCount < 10)
{
    await context.CreateTestVariant(...);
}

// ❌ Bad: Explosion of variants
for (int i = 0; i < 10000; i++)  // Creates 10,000 tests!
{
    await context.CreateTestVariant(...);
}
```

## Limitations

- **Not AOT Compatible**: Test variants require runtime reflection and expression compilation
- **Requires Reflection Mode**: Must run with reflection-based discovery (not source-generated)
- **Performance Overhead**: Each variant is a full test execution with its own lifecycle
- **No Source Generator Support**: Cannot be used in AOT-compiled scenarios

## See Also

- [Test Context](../test-lifecycle/test-context.md) - Understanding TestContext and ObjectBag
- [Dynamic Tests](../experimental/dynamic-tests.md) - Pre-execution test generation
- [Retrying](../execution/retrying.md) - Built-in retry mechanism comparison
- [Properties](../test-lifecycle/properties.md) - Test metadata and custom properties
- [Event Subscribing](../test-lifecycle/event-subscribing.md) - Test lifecycle event receivers
