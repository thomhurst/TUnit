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
[DependsOn(nameof(AddUser4)]
[DependsOn(nameof(AddUser5)]
[DependsOn(nameof(AddItemToBagForUser1)]
[DependsOn(nameof(AddItemToBagForUser2)]
[DependsOn(nameof(AddItemToBagForUser3)]
public async Task AssertItemsInDatabase() 
{
    ...
}
```