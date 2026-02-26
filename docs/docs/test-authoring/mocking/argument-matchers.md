---
sidebar_position: 4
---

# Argument Matchers

Argument matchers control which calls a setup or verification matches. The same matchers work in both contexts — the chain method determines whether it's a setup or verification.

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
| `RefStructArg<T>.Any` | Any value of a ref struct type (.NET 9+) |

## Basic Matchers

### Any

Matches any value, including null:

```csharp
mock.GetUser(Arg.Any<int>()).Returns(new User("Default"));

svc.GetUser(1);    // matches
svc.GetUser(999);  // matches
```

### Exact Value

Match a specific value using equality:

```csharp
mock.GetUser(Arg.Is(42)).Returns(new User("Alice"));
mock.GetUser(Arg.Is(99)).Returns(new User("Bob"));

svc.GetUser(42);  // returns Alice
svc.GetUser(99);  // returns Bob
svc.GetUser(1);   // no match — returns default
```

:::tip Implicit Conversion
Raw values are implicitly converted to `Arg.Is(value)`, so these are equivalent:
```csharp
mock.GetUser(Arg.Is(42)).Returns(new User("Alice"));
mock.GetUser(42).Returns(new User("Alice")); // same thing
```
:::

### Predicate

Match values satisfying a condition:

```csharp
mock.GetUser(Arg.Is<int>(id => id > 0)).Returns(new User("Valid"));
mock.GetUser(Arg.Is<int>(id => id <= 0)).Throws<ArgumentException>();
```

### Null and NotNull

```csharp
mock.Process(Arg.IsNull<string>()).Returns("was null");
mock.Process(Arg.IsNotNull<string>()).Returns("had value");
```

## String Matchers

### Regex Pattern

```csharp
// Match strings against a regex pattern
mock.Search(Arg.Matches(@"^user_\d+$")).Returns(new[] { "found" });

svc.Search("user_42");   // matches
svc.Search("admin_1");   // no match
```

### Compiled Regex

```csharp
var pattern = new Regex(@"^user_\d+$", RegexOptions.Compiled);
mock.Search(Arg.Matches(pattern)).Returns(new[] { "found" });
```

## Collection Matchers

### Contains

```csharp
mock.ProcessItems(Arg.Contains<List<int>, int>(42)).Returns(true);

svc.ProcessItems(new List<int> { 1, 42, 3 }); // matches — contains 42
svc.ProcessItems(new List<int> { 1, 2, 3 });  // no match
```

### HasCount

```csharp
mock.ProcessItems(Arg.HasCount<List<int>>(3)).Returns(true);

svc.ProcessItems(new List<int> { 1, 2, 3 }); // matches — count is 3
svc.ProcessItems(new List<int> { 1, 2 });     // no match
```

### IsEmpty

```csharp
mock.ProcessItems(Arg.IsEmpty<List<int>>()).Returns(false);

svc.ProcessItems(new List<int>());          // matches
svc.ProcessItems(new List<int> { 1 });      // no match
```

### SequenceEquals

```csharp
mock.ProcessItems(
    Arg.SequenceEquals<List<int>, int>(new[] { 1, 2, 3 })
).Returns(true);

svc.ProcessItems(new List<int> { 1, 2, 3 }); // matches
svc.ProcessItems(new List<int> { 3, 2, 1 }); // no match — wrong order
```

## Range and Set Matchers

### IsInRange

```csharp
mock.SetAge(Arg.IsInRange(0, 120)).Returns(true);

svc.SetAge(25);   // matches
svc.SetAge(-1);   // no match
svc.SetAge(200);  // no match
```

### IsIn / IsNotIn

```csharp
mock.GetRole(Arg.IsIn("admin", "editor", "viewer")).Returns(true);
mock.GetRole(Arg.IsNotIn("admin", "superadmin")).Returns(false);
```

## Negation

Wrap any matcher with `Not` to invert it:

```csharp
mock.Process(Arg.Not(Arg.Is(0))).Returns("non-zero");
// Matches any int except 0
```

## Argument Capture

Every `Arg<T>` matcher automatically captures the values it sees:

```csharp
var nameArg = Arg.Any<string>();
mock.Greet(nameArg).Returns("Hi");

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

## Ref Struct Parameters

Regular `Arg<T>` matchers cannot be used with ref struct types like `ReadOnlySpan<T>` or `Span<T>` because ref structs cannot be generic type arguments. On **.NET 9+**, TUnit.Mocks provides `RefStructArg<T>` which uses the `allows ref struct` anti-constraint to make these parameters visible in the setup and verification API.

:::note .NET 9+ Only
`RefStructArg<T>` requires .NET 9 or later. On older target frameworks, ref struct parameters are excluded from the setup/verify API and all calls match regardless of the ref struct argument value.
:::

### Matching Any Value

Currently, `RefStructArg<T>.Any` is the only supported matcher — it matches any value passed for that parameter:

```csharp
public interface IBufferProcessor
{
    void Process(ReadOnlySpan<byte> data);
    int Parse(ReadOnlySpan<char> text);
}

var mock = Mock.Of<IBufferProcessor>();

// Setup — ref struct param is visible in the API
mock.Process(RefStructArg<ReadOnlySpan<byte>>.Any).Callback(() => Console.WriteLine("called"));
mock.Parse(RefStructArg<ReadOnlySpan<char>>.Any).Returns(42);

// Verification
mock.Process(RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Once);
```

### Mixed Parameters

When a method has both regular and ref struct parameters, use `Arg<T>` for the regular ones and `RefStructArg<T>` for the ref struct ones. Argument matching works on the regular parameters while the ref struct parameter matches any value:

```csharp
public interface IMixedProcessor
{
    int Compute(int id, ReadOnlySpan<byte> data);
}

var mock = Mock.Of<IMixedProcessor>();

// Match on 'id', accept any span value
mock.Compute(1, RefStructArg<ReadOnlySpan<byte>>.Any).Returns(100);
mock.Compute(2, RefStructArg<ReadOnlySpan<byte>>.Any).Returns(200);

var result = mock.Object.Compute(1, new byte[] { 0xFF }); // returns 100
```

### Limitations

- **Only `.Any` matching** — exact value and predicate matching are not supported because ref struct values cannot be stored on the heap
- **No argument capture** — `RefStructArg<T>` does not support `.Values` or `.Latest` like `Arg<T>` does
- **Not available in typed callbacks** — ref struct parameters are excluded from the typed `Callback`/`Returns`/`Throws` delegate overloads (use the `Action<object?[]>` overload instead)

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
mock.Greet(Arg.Matches(new StringLengthMatcher(3, 50)))
    .Returns("Valid name");
```

The `Describe()` method is used in verification failure messages to explain what was expected.
