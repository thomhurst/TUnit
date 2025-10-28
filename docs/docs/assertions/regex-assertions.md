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

    // Assert on named capture groups
    await Assert.That(email)
        .Matches(pattern)
        .Group("username", user => user.IsEqualTo("john.doe"))
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
        .Group(0, full => full.IsEqualTo("2025-10-28"))
        .And.Group(1, year => year.IsEqualTo("2025"))
        .And.Group(2, month => month.IsEqualTo("10"))
        .And.Group(3, day => day.IsEqualTo("28"));
}
```

## Match Position and Length

You can assert on where a match occurs and its length:

```csharp
[Test]
public async Task PositionAndLengthAssertions()
{
    var text = "Hello World 123";
    var pattern = @"\d+";

    // Assert that match is at specific index
    await Assert.That(text)
        .Matches(pattern)
        .AtIndex(12);

    // Assert that match has specific length
    await Assert.That(text)
        .Matches(pattern)
        .HasLength(3);

    // Combine with group assertions
    await Assert.That(text)
        .Matches(pattern)
        .AtIndex(12)
        .And.HasLength(3);
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
        .Group("date", date => date.IsEqualTo("2025-10-28"))
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
        .Group("code", code => code.StartsWith("ABC"))
        .And.Group("price", price => price.Contains(".99"))
        .And.Group("stock", stock => stock.HasLength(2));
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
        .Group("protocol", p => p.IsEqualTo("https"))
        .And.Group("host", h => h.Contains("api"))
        .And.Group("port", p => p.IsEqualTo("8080"))
        .And.Group("path", p => p.StartsWith("users/"))
        .And.Group("query", q => q.Contains("format=json"));
}
```

## Regex Options

Use `.IgnoringCase()` or `.WithOptions()` for case-insensitive or other regex options:

```csharp
[Test]
public async Task RegexOptionsAssertions()
{
    var text = "HELLO world";

    // Case-insensitive matching
    await Assert.That(text)
        .Matches(@"hello")
        .IgnoringCase();

    // Custom options
    await Assert.That(text)
        .Matches(@"^hello.*world$")
        .WithOptions(RegexOptions.IgnoreCase | RegexOptions.Singleline);
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
            .Group("year", y => y.IsEqualTo("2025"))
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
        .Group("area", area => area.IsEqualTo("555"))
        .And.Group("prefix", p => p.IsEqualTo("123"));

    // Phone without area code (optional group is empty)
    await Assert.That(phone2)
        .Matches(pattern)
        .Group("area", area => area.IsEqualTo(""))
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
        .Group("local", local => local.StartsWith("john"))
        .And.Group("subdomain", sub => sub.IsEqualTo("mail"))
        .And.Group("domain", domain => domain.IsEqualTo("example"))
        .And.Group("tld", tld => tld.HasLength(3))
        .And.AtIndex(0)
        .And.HasLength(email.Length);
}
```

## Error Handling

The regex assertions throw specific exceptions for common error cases:

```csharp
[Test]
public async Task RegexAssertionErrors()
{
    var text = "Hello123World";

    // Throws ArgumentNullException if text is null
    await Assert.That((string?)null)
        .ThrowsAsync<ArgumentNullException>()
        .When(() => Matches(@"\d+"));

    // Throws RegexParseException for invalid patterns
    await Assert.That(text)
        .ThrowsAsync<RegexParseException>()
        .When(() => Matches(@"[invalid"));

    // Throws ArgumentOutOfRangeException for invalid group index
    await Assert.That(text)
        .Matches(@"Hello(\d+)World")
        .ThrowsAsync<ArgumentOutOfRangeException>()
        .When(m => m.Group(99, g => g.IsEqualTo("123")));

    // Throws ArgumentException for empty group name
    await Assert.That(text)
        .Matches(@"Hello(?<num>\d+)World")
        .ThrowsAsync<ArgumentException>()
        .When(m => m.Group("", g => g.IsEqualTo("123")));
}
```

## Best Practices

1. **Use source-generated regex** for better performance and compile-time validation
2. **Name your capture groups** descriptively (e.g., `username`, `domain`, not `g1`, `g2`)
3. **Chain assertions** using `.And` to validate multiple aspects in one test
4. **Handle optional groups** explicitly by checking for empty strings
5. **Test edge cases** like empty matches, multiple occurrences, and boundary conditions
6. **Use raw string literals** (`@""` or `"""`) to avoid escaping backslashes

## Related Assertions

- [String Assertions](string-assertions.md) - Basic string validation
- [Member Assertions](member-assertions.md) - Object property validation
- [Collection Assertions](collection-assertions.md) - Collection validation
