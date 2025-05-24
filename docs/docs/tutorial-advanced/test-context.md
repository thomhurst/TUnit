---
sidebar_position: 7
---

# Test Context

All tests have a `TestContext` object available to them.

This can be accessed statically via `TestContext.Current`.

Here you can see information about the test, including things like the test name, containing class, custom properties, categories, etc.

This can be useful if you want something like a generic `AfterEachTest` for all tests, but with logic to execute for only certain tests.

e.g.
```csharp
if (TestContext.Current.TestInformation.CustomProperties.ContainsKey("SomeProperty"))
{
    // Do something
}
```

The context also has a `Results` object. You'll notice this is nullable. This will be null until you're in the context of a `AfterEachTest` method. That's because the `Results` can only be set after a test has finished.

These results can be handy when you're cleaning up, but maybe only want to do something if a test failed.

e.g.
```csharp
if (TestContext.Current?.Result?.Status == Status.Failed)
{
    // Take a screenshot?
}
```
