---
sidebar_position: 4
---

# Argument Matchers

Argument matchers control which calls a setup or verification matches. They are used in both `mock.Setup` and `mock.Verify` surfaces.

## Quick Reference

| Matcher | Matches |
|---|---|
| `Arg.Any<T>()` | Any value of type T (including null) |
| `Arg.Is<T>(value)` | Exact equality |
| `Arg.Is<T>(predicate)` | Values satisfying a predicate |
| `Arg.IsNull<T>()` | Null only |
| `Arg.IsNotNull<T>()` | Any non-null value |
| `Arg.Matches(pattern)` | String matching a regex pattern |
| `Arg.Matches(regex)` | String matching a compiled `Regex` |
| `Arg.Contains<TCol, TElem>(item)` | Collection containing an element |
| `Arg.HasCount<T>(n)` | Collection with exactly n elements |
| `Arg.IsEmpty<T>()` | Empty collection |
| `Arg.SequenceEquals<TCol, TElem>(expected)` | Collection matching element-by-element |
| `Arg.IsInRange<T>(min, max)` | Value within an inclusive range |
| `Arg.IsIn<T>(values)` | Value in a set |
| `Arg.IsNotIn<T>(values)` | Value not in a set |
| `Arg.Not<T>(inner)` | Negation of another matcher |

## Basic Matchers

### Any

Matches any value, including null:

```csharp
mock.Setup.GetUser(Arg.Any<int>()).Returns(new User("Default"));

svc.GetUser(1);    // matches
svc.GetUser(999);  // matches
```

### Exact Value

Match a specific value using equality:

```csharp
mock.Setup.GetUser(Arg.Is(42)).Returns(new User("Alice"));
mock.Setup.GetUser(Arg.Is(99)).Returns(new User("Bob"));

svc.GetUser(42);  // returns Alice
svc.GetUser(99);  // returns Bob
svc.GetUser(1);   // no match — returns default
```

:::tip Implicit Conversion
Raw values are implicitly converted to `Arg.Is(value)`, so these are equivalent:
```csharp
mock.Setup.GetUser(Arg.Is(42)).Returns(new User("Alice"));
mock.Setup.GetUser(42).Returns(new User("Alice")); // same thing
```
:::

### Predicate

Match values satisfying a condition:

```csharp
mock.Setup.GetUser(Arg.Is<int>(id => id > 0)).Returns(new User("Valid"));
mock.Setup.GetUser(Arg.Is<int>(id => id <= 0)).Throws<ArgumentException>();
```

### Null and NotNull

```csharp
mock.Setup.Process(Arg.IsNull<string>()).Returns("was null");
mock.Setup.Process(Arg.IsNotNull<string>()).Returns("had value");
```

## String Matchers

### Regex Pattern

```csharp
// Match strings against a regex pattern
mock.Setup.Search(Arg.Matches(@"^user_\d+$")).Returns(new[] { "found" });

svc.Search("user_42");   // matches
svc.Search("admin_1");   // no match
```

### Compiled Regex

```csharp
var pattern = new Regex(@"^user_\d+$", RegexOptions.Compiled);
mock.Setup.Search(Arg.Matches(pattern)).Returns(new[] { "found" });
```

## Collection Matchers

### Contains

```csharp
mock.Setup.ProcessItems(Arg.Contains<List<int>, int>(42)).Returns(true);

svc.ProcessItems(new List<int> { 1, 42, 3 }); // matches — contains 42
svc.ProcessItems(new List<int> { 1, 2, 3 });  // no match
```

### HasCount

```csharp
mock.Setup.ProcessItems(Arg.HasCount<List<int>>(3)).Returns(true);

svc.ProcessItems(new List<int> { 1, 2, 3 }); // matches — count is 3
svc.ProcessItems(new List<int> { 1, 2 });     // no match
```

### IsEmpty

```csharp
mock.Setup.ProcessItems(Arg.IsEmpty<List<int>>()).Returns(false);

svc.ProcessItems(new List<int>());          // matches
svc.ProcessItems(new List<int> { 1 });      // no match
```

### SequenceEquals

```csharp
mock.Setup.ProcessItems(
    Arg.SequenceEquals<List<int>, int>(new[] { 1, 2, 3 })
).Returns(true);

svc.ProcessItems(new List<int> { 1, 2, 3 }); // matches
svc.ProcessItems(new List<int> { 3, 2, 1 }); // no match — wrong order
```

## Range and Set Matchers

### IsInRange

```csharp
mock.Setup.SetAge(Arg.IsInRange(0, 120)).Returns(true);

svc.SetAge(25);   // matches
svc.SetAge(-1);   // no match
svc.SetAge(200);  // no match
```

### IsIn / IsNotIn

```csharp
mock.Setup.GetRole(Arg.IsIn("admin", "editor", "viewer")).Returns(true);
mock.Setup.GetRole(Arg.IsNotIn("admin", "superadmin")).Returns(false);
```

## Negation

Wrap any matcher with `Not` to invert it:

```csharp
mock.Setup.Process(Arg.Not(Arg.Is(0))).Returns("non-zero");
// Matches any int except 0
```

## Argument Capture

Every `Arg<T>` matcher automatically captures the values it sees:

```csharp
var nameArg = Arg.Any<string>();
mock.Setup.Greet(nameArg).Returns("Hi");

svc.Greet("Alice");
svc.Greet("Bob");
svc.Greet("Charlie");

// Access captured values
var all = nameArg.Values;   // ["Alice", "Bob", "Charlie"]
var last = nameArg.Latest;  // "Charlie"

await Assert.That(nameArg.Values).HasCount().EqualTo(3);
```

:::tip
Capture works in both setup and verification contexts. Store the `Arg<T>` in a variable, then inspect `.Values` or `.Latest` after exercising the code.
:::

## Custom Matchers

Implement `IArgumentMatcher<T>` for reusable matching logic:

```csharp
public class StringLengthMatcher : IArgumentMatcher<string>
{
    private readonly int _min;
    private readonly int _max;

    public StringLengthMatcher(int min, int max)
    {
        _min = min;
        _max = max;
    }

    public bool Matches(string? value)
        => value is not null && value.Length >= _min && value.Length <= _max;

    public bool Matches(object? value)
        => Matches(value as string);

    public string Describe()
        => $"string with length between {_min} and {_max}";
}

// Usage
mock.Setup.Greet(Arg.Matches(new StringLengthMatcher(3, 50)))
    .Returns("Valid name");
```

The `Describe()` method is used in verification failure messages to explain what was expected.
