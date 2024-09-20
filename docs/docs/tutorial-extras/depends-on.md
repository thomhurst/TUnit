---
sidebar_position: 11
---

# Depends On

A test can depend on another test. This means that your test will not start unless the other test has finished.

To do this, add a  `[DependsOn]` to your test.

This takes a test name, which you can easily reference by using the `nameof(TestMethod)` keyword. And if your test you depend on has parameters, you must include the types of those too.

e.g.:
```csharp
public void Test1(string value1, int value2) { ... }

[DependsOn(nameof(Test1), [typeof(string), typeof(int)])]
public void Test2() { ... }
```

This means you can create more complex test suites, without having to compromise on parallelism or speed.

For example, performing some operations on a database and asserting a count at the end:

```csharp
[Test]
public async Task AddUser1() 
{
    ...
}

[Test]
public async Task AddUser2() 
{
    ...
}

[Test]
public async Task AddUser3() 
{
    ...
}

[Test]
public async Task AddUser4() 
{
    ...
}

[Test]
public async Task AddUser5() 
{
    ...
}

[Test, DependsOn(nameof(AddUser1))]
public async Task AddItemToBagForUser1() 
{
    ...
}

[Test, DependsOn(nameof(AddUser2))]
public async Task AddItemToBagForUser2() 
{
    ...
}

[Test, DependsOn(nameof(AddUser3))]
public async Task AddItemToBagForUser3() 
{
    ...
}

[Test]
[DependsOn(nameof(AddUser4))]
[DependsOn(nameof(AddUser5))]
[DependsOn(nameof(AddItemToBagForUser1))]
[DependsOn(nameof(AddItemToBagForUser2))]
[DependsOn(nameof(AddItemToBagForUser3))]
public async Task AssertItemsInDatabase() 
{
    ...
}
```

## Getting other tests
If your tests depends on another test, it's possible to retrieve that test's context. This allows you to do things like check its result, or retrieve objects from its object bag.

This is done by calling the `GetTests` method on a `TestContext` object. It takes the test's method name (so you can use `nameof(...)`) and optionally the parameter types for if there's multiple overloads.

You'll notice this returns an array - This is because tests may be data driven and be invoked multiple times - If this is the case you'll have to find the one you want yourself.

Example:

```csharp
[Test]
public async Task AddItemToBag() 
{
    var itemId = await AddToBag();
    TestContext.Current!.ObjectBag.Add("ItemId", itemId);
}

[Test]
[DependsOn(nameof(AddItemToBag))]
public async Task DeleteItemFromBag() 
{
    var addToBagTestContext = TestContext.Current!.GetTests(nameof(AddItemToBag)).First();
    var itemId = addToBagTestContext.ObjectBag["ItemId"];
    await DeleteFromBag(itemId);
}
```

## Failures

If your test depends on another test, by default, if that dependency fails, then your test that depends on it will not start. This can be bypassed by adding the property `ProceedOnFailure = true` to the `DependsOnAttribute`. Your test suite will still fail due to that test, but it allows you to proceed with other tests if you require it. For example, CRUD testing, and wanting to perform a delete after all your other tests, regardless of if they passed.

```csharp
[Test]
public async Task Test1() 
{
    ...
}

[Test]
[DependsOn(nameof(Test1), ProceedOnFailure = true)]
public async Task Test2() 
{
    ...
}
```