---
sidebar_position: 13
---

# Regex Assertions

The `.Matches()` method allows you to validate strings against regular expressions and assert on capture groups, match positions, and match lengths. This is useful when you need to validate structured text like emails, phone numbers, dates, or extract specific parts of a string.

## Basic Usage

```csharp
[Test]
public async Task BasicRegexAssertions()
{
    var email = "john.doe@example.com";

    // Assert that string matches a pattern
    await Assert.That(email).Matches(@"^[\w.]+@[\w.]+$");

    // Use a compiled Regex object
    var emailRegex = new Regex(@"^[\w.]+@[\w.]+$");
    await Assert.That(email).Matches(emailRegex);

    // Use source-generated regex (C# 11+)
    [GeneratedRegex(@"^[\w.]+@[\w.]+$")]
    static partial Regex EmailRegex();

    await Assert.That(email).Matches(EmailRegex());
}
```

## Group Assertions

The key advantage of regex assertions is the ability to assert on capture groups using `.Group()`:

### Named Groups

```csharp
[Test]
public async Task NamedGroupAssertions()
{
    var email = "john.doe@example.com";
    var pattern = @"(?<username>[\w.]+)@(?<domain>[\w.]+)";

    // Assert on named capture groups (requires .And before .Group())
    await Assert.That(email)
        .Matches(pattern)
        .And.Group("username", user => user.IsEqualTo("john.doe"))
        .And.Group("domain", domain => domain.IsEqualTo("example.com"));
}
```

### Indexed Groups

```csharp
[Test]
public async Task IndexedGroupAssertions()
{
    var date = "2025-10-28";
    var pattern = @"(\d{4})-(\d{2})-(\d{2})";

    // Assert on indexed capture groups (1-based, 0 is full match)
    await Assert.That(date)
        .Matches(pattern)
        .And.Group(0, full => full.IsEqualTo("2025-10-28"))
        .And.Group(1, year => year.IsEqualTo("2025"))
        .And.Group(2, month => month.IsEqualTo("10"))
        .And.Group(3, day => day.IsEqualTo("28"));
}
```

## Multiple Matches

When a regex matches multiple times in a string, you can access specific matches using `.Match(index)`:

```csharp
[Test]
public async Task MultipleMatchAssertions()
{
    var text = "test123 hello456 world789";
    var pattern = @"\w+\d+";

    // Assert on first match
    await Assert.That(text)
        .Matches(pattern)
        .And.Match(0)
        .And.Group(0, match => match.IsEqualTo("test123"));

    // Use lambda pattern to assert on a specific match
    await Assert.That(text)
        .Matches(pattern)
        .And.Match(1, match => match.Group(0, g => g.IsEqualTo("hello456")));
}
```

## Match Position and Length

To assert on where a match occurs or how long it is, use `.Match(index)` to select a match from the collection, then assert on the resulting `RegexMatch` (you can also combine this with `Regex.Match(...)` directly if you need more detailed inspection):

```csharp
[Test]
public async Task PositionAndLengthAssertions()
{
    var text = "Hello World 123";
    var pattern = @"\d+";

    // Directly inspect the first match for position and length
    var match = System.Text.RegularExpressions.Regex.Match(text, pattern);

    await Assert.That(match.Index).IsEqualTo(12);
    await Assert.That(match.Length).IsEqualTo(3);

    // Or combine the TUnit regex assertion with a direct group check
    await Assert.That(text)
        .Matches(pattern)
        .And.Group(0, g => g.IsEqualTo("123"));
}
```

## Complex Patterns with Multiple Groups

```csharp
[Test]
public async Task ComplexPatternAssertions()
{
    var logEntry = "[2025-10-28 14:30:45] ERROR: Connection timeout";
    var pattern = @"\[(?<date>\d{4}-\d{2}-\d{2}) (?<time>\d{2}:\d{2}:\d{2})\] (?<level>\w+): (?<message>.+)";

    await Assert.That(logEntry)
        .Matches(pattern)
        .And.Group("date", date => date.IsEqualTo("2025-10-28"))
        .And.Group("time", time => time.StartsWith("14"))
        .And.Group("level", level => level.IsEqualTo("ERROR"))
        .And.Group("message", msg => msg.Contains("timeout"));
}
```

## Product Information Validation

```csharp
[Test]
public async Task ProductCodeValidation()
{
    var product = "Product: ABC-123 Price: $99.99 Stock: 42";
    var pattern = @"Product:\s+(?<code>[A-Z]+-\d+)\s+Price:\s+\$(?<price>[\d.]+)\s+Stock:\s+(?<stock>\d+)";

    await Assert.That(product)
        .Matches(pattern)
        .And.Group("code", code => code.StartsWith("ABC"))
        .And.Group("price", price => price.Contains(".99"))
        .And.Group("stock", stock => stock.Length().IsEqualTo(2));
}
```

## URL Parsing

```csharp
[Test]
public async Task UrlParsingAssertions()
{
    var url = "https://api.example.com:8080/users/123?format=json";
    var pattern = @"(?<protocol>https?)://(?<host>[\w.]+):(?<port>\d+)/(?<path>[^?]+)\?(?<query>.+)";

    await Assert.That(url)
        .Matches(pattern)
        .And.Group("protocol", p => p.IsEqualTo("https"))
        .And.Group("host", h => h.Contains("api"))
        .And.Group("port", p => p.IsEqualTo("8080"))
        .And.Group("path", p => p.StartsWith("users/"))
        .And.Group("query", q => q.Contains("format=json"));
}
```

## Regex Options

The `Matches(string)` overload does not take `RegexOptions`. To apply options like case-insensitivity, construct a `Regex` (or use a source-generated regex) with the desired options and pass it to `Matches`:

```csharp
[Test]
public async Task RegexOptionsAssertions()
{
    var text = "HELLO world";

    // Case-insensitive matching via a Regex instance
    var caseInsensitive = new Regex("hello", RegexOptions.IgnoreCase);
    await Assert.That(text).Matches(caseInsensitive);

    // Custom combined options
    var multi = new Regex(@"^hello.*world$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    await Assert.That(text).Matches(multi);
}
```

## Source-Generated Regex (Recommended)

For performance-critical code, use C# 11+ source-generated regex:

```csharp
public partial class MyTests
{
    [GeneratedRegex(@"(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})")]
    private static partial Regex DatePattern();

    [Test]
    public async Task SourceGeneratedRegexAssertions()
    {
        var date = "2025-10-28";

        // Source-generated regex provides better performance
        await Assert.That(date)
            .Matches(DatePattern())
            .And.Group("year", y => y.IsEqualTo("2025"))
            .And.Group("month", m => m.IsEqualTo("10"))
            .And.Group("day", d => d.IsEqualTo("28"));
    }
}
```

## Optional and Empty Groups

Handle optional capture groups that may be empty:

```csharp
[Test]
public async Task OptionalGroupAssertions()
{
    var phone1 = "(555) 123-4567";
    var phone2 = "123-4567";
    var pattern = @"(\((?<area>\d{3})\)\s+)?(?<prefix>\d{3})-(?<line>\d{4})";

    // Phone with area code
    await Assert.That(phone1)
        .Matches(pattern)
        .And.Group("area", area => area.IsEqualTo("555"))
        .And.Group("prefix", p => p.IsEqualTo("123"));

    // Phone without area code (optional group is empty)
    await Assert.That(phone2)
        .Matches(pattern)
        .And.Group("area", area => area.IsEqualTo(""))
        .And.Group("prefix", p => p.IsEqualTo("123"));
}
```

## Complete Example

```csharp
[Test]
public async Task CompleteEmailValidation()
{
    var email = "john.doe+test@mail.example.com";
    var pattern = @"(?<local>[\w.+-]+)@(?<subdomain>[\w]+)\.(?<domain>[\w]+)\.(?<tld>\w+)";

    await Assert.That(email)
        .Matches(pattern)
        .And.Group("local", local => local.StartsWith("john"))
        .And.Group("subdomain", sub => sub.IsEqualTo("mail"))
        .And.Group("domain", domain => domain.IsEqualTo("example"))
        .And.Group("tld", tld => tld.Length().IsEqualTo(3));

    // For position/length checks, use Regex.Match directly
    var match = System.Text.RegularExpressions.Regex.Match(email, pattern);
    await Assert.That(match.Index).IsEqualTo(0);
    await Assert.That(match.Length).IsEqualTo(email.Length);
}
```

## Error Handling

The regex assertions surface standard exceptions for common error cases. Wrap the call in an `Assert.That(() => ...)` delegate and assert on the thrown exception type:

```csharp
[Test]
public async Task RegexAssertionErrors()
{
    var text = "Hello123World";

    // Throws ArgumentNullException if text is null
    await Assert.That(async () =>
        await Assert.That((string?)null!).Matches(@"\d+"))
        .Throws<ArgumentNullException>();

    // Throws RegexParseException for invalid patterns
    await Assert.That(async () =>
        await Assert.That(text).Matches(@"[invalid"))
        .Throws<RegexParseException>();
}
```

For invalid group indices or names, let the underlying `Regex` call throw and assert on it via a delegate in the same way.

## Best Practices

1. **Use source-generated regex** for better performance and compile-time validation
2. **Name your capture groups** descriptively (e.g., `username`, `domain`, not `g1`, `g2`)
3. **Chain assertions** using `.And` to validate multiple aspects in one test
4. **Handle optional groups** explicitly by checking for empty strings
5. **Test edge cases** like empty matches, multiple occurrences, and boundary conditions
6. **Use raw string literals** (`@""` or `"""`) to avoid escaping backslashes

## Related Assertions

- [String Assertions](string.md) - Basic string validation
- [Member Assertions](member-assertions.md) - Object property validation
- [Collection Assertions](collections.md) - Collection validation
