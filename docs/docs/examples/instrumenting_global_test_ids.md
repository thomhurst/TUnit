---
sidebar_position: 3
---

# Instrumenting: Global Test IDs

There are plenty use cases for having a unique identifier for each test in your test suite. If you're engineering tests that connect to a data source, you might want to ensure data isolation between the tests. One way to do that is cleaning up the data source after each test, but that prevents you from running tests cleanly in parallel, and it requires you to write either very intelligent or a lot of cleanup code. Additionally, bugs can cause data to leak between tests and make your tests flaky.

A straightforward way to ensure data isolation is to connect to a different data source for each test. To isolate, you need a unique identifier to differentiate between the tests. This identifier should be known before the test starts, so you can use it to provision the data source and build the connection string. For Redis, you would typically use a different database number for each test. For SQL databases, you would typically use a different table or database name for each test.

We can hook into TUnit's test discovery process and assign unique identifiers to tests by implementing `OnTestDiscovery(DiscoveredTestContext discoveredTestContext)` in `ITestDiscoveryEventReceiver`. Here's an example implementation that is tailored for contrived test cases involving Redis databases:

```csharp
class AssignTestIdentifiersAttribute : Attribute, ITestDiscoveryEventReceiver
{
    public const string TestIdObjectBagKey = "\u0001TestId";

    public static int TestId { get; private set; } = 0;

    public void OnTestDiscovery(DiscoveredTestContext discoveredTestContext)
    {
        discoveredTestContext.TestContext.ObjectBag[TestIdObjectBagKey] = TestId++;
    }
}
```

`TestIdObjectBagKey` is a unique key that we use to store the test identifier in the `ObjectBag` of TUnit's `TestContext`.

`TestId` is a static integer that we increment for each test. We use this to assign a unique identifier to each test.

In `OnTestDiscovery`, we assign the test identifier to the `ObjectBag` using the `TestIdObjectBagKey`. The use of `ObjectBag` exposes the test identifier to hooks and tests.

Before we demonstrate how to use this attribute, let's create a simple extension method for `TestContext` to retrieve the test identifier swiftly:

```csharp
static class TestContextExtensions
{
    public static int GetTestId(this TestContext? testContext)
    {
        // Retrieve the test identifier from the ObjectBag
        return (int)testContext!.ObjectBag[AssignTestIdentifiersAttribute.TestIdObjectBagKey]!;
    }
}
```

Assign unique identifiers to all tests in the assembly by decorating in `AssemblyInfo.cs`:

```csharp
[assembly: AssignTestIdentifiers]
```

And then use the unique identifier in your tests:

```csharp
class MyTestClassThatNeedsUniqueTestIds
{
    private IDatabase isolatedRedisDb = null!;

    [Before(HookType.Test)]
    public void BeforeEach()
    {
        // Call the extension method to retrieve the unique test id:
        int currentTestId = TestContext.Current.GetTestId();

        // Connect to the Redis database using the test id:
        this.isolatedRedisDb = StackExchange.Redis.ConnectionMultiplexer
                                .Connect("localhost")
                                .GetDatabase(currentTestId);
    }

    [Test]
    public void MyTestCase()
    {
        // Do stuff with isolatedRedisDb
    }

    [Test]
    public void MyOtherTestCase()
    {
        // Do stuff with isolatedRedisDb
    }

    [Test]
    public void YetAnotherTestCase()
    {
        // Do stuff with isolatedRedisDb
    }
}
```

The test identifier for each test is assigned in the order that TUnit discovers the tests. The test identifier is unique for each test and is guaranteed to be assigned before the test starts. For other uses cases, you would need to adjust the implementation of `AssignTestIdentifiersAttribute` to suit your needs. For example, you could choose to use GUIDs instead of integers. We've only used integers to match the Redis database number example.
