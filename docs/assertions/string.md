# String Assertions

TUnit provides rich assertions for testing strings, including substring matching, pattern matching, length checks, and various string comparison options.

## Content Assertions[​](#content-assertions "Direct link to Content Assertions")

### Contains[​](#contains "Direct link to Contains")

Tests that a string contains a substring:

```
[Test]

public async Task String_Contains()

{

    var message = "Hello, World!";



    await Assert.That(message).Contains("World");

    await Assert.That(message).Contains("Hello");

    await Assert.That(message).Contains(", ");

}
```

#### Case-Insensitive Contains[​](#case-insensitive-contains "Direct link to Case-Insensitive Contains")

```
[Test]

public async Task Contains_Ignoring_Case()

{

    var message = "Hello, World!";



    await Assert.That(message)

        .Contains("world")

        .IgnoringCase();



    await Assert.That(message)

        .Contains("HELLO")

        .IgnoringCase();

}
```

#### With String Comparison[​](#with-string-comparison "Direct link to With String Comparison")

```
[Test]

public async Task Contains_With_Comparison()

{

    var message = "Hello, World!";



    await Assert.That(message)

        .Contains("world")

        .WithComparison(StringComparison.OrdinalIgnoreCase);

}
```

#### With Trimming[​](#with-trimming "Direct link to With Trimming")

```
[Test]

public async Task Contains_With_Trimming()

{

    var message = "  Hello, World!  ";



    await Assert.That(message)

        .Contains("Hello, World!")

        .WithTrimming();

}
```

#### Ignoring Whitespace[​](#ignoring-whitespace "Direct link to Ignoring Whitespace")

```
[Test]

public async Task Contains_Ignoring_Whitespace()

{

    var message = "Hello,    World!";



    await Assert.That(message)

        .Contains("Hello, World!")

        .IgnoringWhitespace();

}
```

### DoesNotContain[​](#doesnotcontain "Direct link to DoesNotContain")

Tests that a string does not contain a substring:

```
[Test]

public async Task String_Does_Not_Contain()

{

    var message = "Hello, World!";



    await Assert.That(message).DoesNotContain("Goodbye");

    await Assert.That(message).DoesNotContain("xyz");

}
```

All modifiers work with `DoesNotContain`:

```
[Test]

public async Task Does_Not_Contain_Ignoring_Case()

{

    var message = "Hello, World!";



    await Assert.That(message)

        .DoesNotContain("goodbye")

        .IgnoringCase();

}
```

### StartsWith[​](#startswith "Direct link to StartsWith")

Tests that a string starts with a specific prefix:

```
[Test]

public async Task String_Starts_With()

{

    var filename = "report_2024.pdf";



    await Assert.That(filename).StartsWith("report");



    var url = "https://example.com";

    await Assert.That(url).StartsWith("https://");

}
```

With case-insensitive comparison:

```
[Test]

public async Task Starts_With_Ignoring_Case()

{

    var filename = "Report_2024.pdf";



    await Assert.That(filename)

        .StartsWith("report")

        .IgnoringCase();

}
```

### EndsWith[​](#endswith "Direct link to EndsWith")

Tests that a string ends with a specific suffix:

```
[Test]

public async Task String_Ends_With()

{

    var filename = "document.pdf";



    await Assert.That(filename).EndsWith(".pdf");



    var email = "user@example.com";

    await Assert.That(email).EndsWith("example.com");

}
```

With case-insensitive comparison:

```
[Test]

public async Task Ends_With_Ignoring_Case()

{

    var filename = "document.PDF";



    await Assert.That(filename)

        .EndsWith(".pdf")

        .IgnoringCase();

}
```

## Pattern Matching[​](#pattern-matching "Direct link to Pattern Matching")

### Matches (Regex)[​](#matches-regex "Direct link to Matches (Regex)")

Tests that a string matches a regular expression pattern:

```
[Test]

public async Task String_Matches_Pattern()

{

    var email = "test@example.com";



    await Assert.That(email).Matches(@"^[\w\.-]+@[\w\.-]+\.\w+$");

}
```

With a compiled `Regex`:

```
[Test]

public async Task Matches_With_Regex()

{

    var phoneNumber = "(123) 456-7890";

    var pattern = new Regex(@"^\(\d{3}\) \d{3}-\d{4}$");



    await Assert.That(phoneNumber).Matches(pattern);

}
```

#### Case-Insensitive Matching[​](#case-insensitive-matching "Direct link to Case-Insensitive Matching")

```
[Test]

public async Task Matches_Ignoring_Case()

{

    var text = "Hello World";



    await Assert.That(text)

        .Matches("hello.*world")

        .IgnoringCase();

}
```

#### With Regex Options[​](#with-regex-options "Direct link to With Regex Options")

```
[Test]

public async Task Matches_With_Options()

{

    var multiline = "Line 1\nLine 2\nLine 3";



    await Assert.That(multiline)

        .Matches("^Line 2$")

        .WithOptions(RegexOptions.Multiline);

}
```

### DoesNotMatch[​](#doesnotmatch "Direct link to DoesNotMatch")

Tests that a string does not match a pattern:

```
[Test]

public async Task String_Does_Not_Match()

{

    var text = "abc123";



    await Assert.That(text).DoesNotMatch(@"^\d+$"); // Not all digits

}
```

## Length Assertions[​](#length-assertions "Direct link to Length Assertions")

### IsEmpty[​](#isempty "Direct link to IsEmpty")

Tests that a string is empty (`""`):

```
[Test]

public async Task String_Is_Empty()

{

    var emptyString = "";



    await Assert.That(emptyString).IsEmpty();

}
```

Note: This checks for an empty string, not `null`:

```
[Test]

public async Task Empty_vs_Null()

{

    string? nullString = null;

    string emptyString = "";



    await Assert.That(nullString).IsNull();      // null

    await Assert.That(emptyString).IsEmpty();    // not null, but empty

    await Assert.That(emptyString).IsNotNull();  // also passes

}
```

### IsNotEmpty[​](#isnotempty "Direct link to IsNotEmpty")

Tests that a string is not empty:

```
[Test]

public async Task String_Is_Not_Empty()

{

    var text = "Hello";



    await Assert.That(text).IsNotEmpty();

}
```

### Length[​](#length "Direct link to Length")

Tests that a string has a specific length:

```
[Test]

public async Task String_Has_Length()

{

    var code = "ABC123";



    await Assert.That(code).Length().IsEqualTo(6);

}
```

With chained comparison:

```
[Test]

public async Task Length_With_Comparison()

{

    var username = "alice";



    await Assert.That(username)

        .Length().IsGreaterThan(3)

        .And.Length().IsLessThan(20);

}
```

Using `IsBetween`:

```
[Test]

public async Task Length_Range()

{

    var username = "alice";



    await Assert.That(username).Length().IsBetween(3, 20);

}
```

## Equality with Options[​](#equality-with-options "Direct link to Equality with Options")

### IsEqualTo[​](#isequalto "Direct link to IsEqualTo")

String equality with various comparison options:

```
[Test]

public async Task String_Equality()

{

    var actual = "Hello";

    var expected = "Hello";



    await Assert.That(actual).IsEqualTo(expected);

}
```

#### Ignoring Case[​](#ignoring-case "Direct link to Ignoring Case")

```
[Test]

public async Task Equality_Ignoring_Case()

{

    var actual = "Hello";

    var expected = "HELLO";



    await Assert.That(actual)

        .IsEqualTo(expected)

        .IgnoringCase();

}
```

#### With String Comparison[​](#with-string-comparison-1 "Direct link to With String Comparison")

```
[Test]

public async Task Equality_With_Comparison()

{

    var actual = "café";

    var expected = "CAFÉ";



    await Assert.That(actual)

        .IsEqualTo(expected)

        .WithComparison(StringComparison.CurrentCultureIgnoreCase);

}
```

## String Parsing[​](#string-parsing "Direct link to String Parsing")

You can parse strings to other types and assert on the result:

```
[Test]

public async Task Parse_String_To_Int()

{

    var text = "42";



    var number = await Assert.That(text).WhenParsedInto<int>();

    await Assert.That(number).IsEqualTo(42);

}
```

```
[Test]

public async Task Parse_String_To_DateTime()

{

    var text = "2024-01-15";



    var date = await Assert.That(text).WhenParsedInto<DateTime>();

    await Assert.That(date.Year).IsEqualTo(2024);

}
```

## Practical Examples[​](#practical-examples "Direct link to Practical Examples")

### Email Validation[​](#email-validation "Direct link to Email Validation")

```
[Test]

public async Task Validate_Email()

{

    var email = "user@example.com";



    await Assert.That(email)

        .Contains("@")

        .And.Matches(@"^[\w\.-]+@[\w\.-]+\.\w+$")

        .And.DoesNotContain(" ");

}
```

### URL Validation[​](#url-validation "Direct link to URL Validation")

```
[Test]

public async Task Validate_URL()

{

    var url = "https://www.example.com/path";



    await Assert.That(url)

        .StartsWith("https://")

        .And.Contains("example.com")

        .And.Matches(@"^https?://[\w\.-]+");

}
```

### File Extension Check[​](#file-extension-check "Direct link to File Extension Check")

```
[Test]

public async Task Check_File_Extension()

{

    var filename = "document.pdf";



    await Assert.That(filename)

        .EndsWith(".pdf")

        .IgnoringCase();

}
```

### Username Validation[​](#username-validation "Direct link to Username Validation")

```
[Test]

public async Task Validate_Username()

{

    var username = "alice_123";



    await Assert.That(username)

        .Length().IsGreaterThanOrEqualTo(3)

        .And.Length().IsLessThanOrEqualTo(20)

        .And.Matches(@"^[a-zA-Z0-9_]+$")

        .And.DoesNotContain(" ");

}
```

### Password Requirements[​](#password-requirements "Direct link to Password Requirements")

```
[Test]

public async Task Validate_Password()

{

    var password = "SecureP@ss123";



    await Assert.That(password)

        .Length().IsGreaterThanOrEqualTo(8)

        .And.Matches(@"[A-Z]")  // Has uppercase

        .And.Matches(@"[a-z]")  // Has lowercase

        .And.Matches(@"\d")     // Has digit

        .And.Matches(@"[@$!%*?&]"); // Has special char

}
```

### JSON String Content[​](#json-string-content "Direct link to JSON String Content")

```
[Test]

public async Task Check_JSON_Content()

{

    var json = """{"name":"Alice","age":30}""";



    await Assert.That(json)

        .Contains("\"name\"")

        .And.Contains("\"Alice\"")

        .And.StartsWith("{")

        .And.EndsWith("}");

}
```

### SQL Query Validation[​](#sql-query-validation "Direct link to SQL Query Validation")

```
[Test]

public async Task Validate_SQL_Query()

{

    var query = "SELECT * FROM Users WHERE Age > 18";



    await Assert.That(query)

        .StartsWith("SELECT")

        .And.Contains("FROM Users")

        .And.Matches(@"WHERE\s+\w+\s*[><=]");

}
```

## Null and Empty Checks Combined[​](#null-and-empty-checks-combined "Direct link to Null and Empty Checks Combined")

### IsNullOrEmpty Equivalent[​](#isnullorempty-equivalent "Direct link to IsNullOrEmpty Equivalent")

```
[Test]

public async Task Check_Null_Or_Empty()

{

    string? text = GetOptionalString();



    // Option 1: Check both conditions

    if (string.IsNullOrEmpty(text))

    {

        // Handle null or empty

    }

    else

    {

        await Assert.That(text).IsNotNull();

        await Assert.That(text).IsNotEmpty();

    }

}
```

### IsNullOrWhiteSpace Equivalent[​](#isnullorwhitespace-equivalent "Direct link to IsNullOrWhiteSpace Equivalent")

```
[Test]

public async Task Check_Null_Or_Whitespace()

{

    string? text = "   ";



    await Assert.That(string.IsNullOrWhiteSpace(text)).IsTrue();

}
```

Better with trimming:

```
[Test]

public async Task Require_Non_Whitespace()

{

    string? text = GetInput();



    await Assert.That(text)

        .IsNotNull()

        .And.IsNotEmpty();



    var trimmed = text.Trim();

    await Assert.That(trimmed).IsNotEmpty();

}
```

## StringBuilder Assertions[​](#stringbuilder-assertions "Direct link to StringBuilder Assertions")

TUnit also supports assertions on `StringBuilder`:

```
[Test]

public async Task StringBuilder_Tests()

{

    var builder = new StringBuilder();

    builder.Append("Hello");

    builder.Append(" ");

    builder.Append("World");



    var result = builder.ToString();



    await Assert.That(result).IsEqualTo("Hello World");

    await Assert.That(result).Contains("Hello");

}
```

## Chaining String Assertions[​](#chaining-string-assertions "Direct link to Chaining String Assertions")

```
[Test]

public async Task Chained_String_Assertions()

{

    var input = "Hello, World!";



    await Assert.That(input)

        .IsNotNull()

        .And.IsNotEmpty()

        .And.Contains("World")

        .And.StartsWith("Hello")

        .And.EndsWith("!")

        .And.Length().IsEqualTo(13);

}
```

## Case Sensitivity Patterns[​](#case-sensitivity-patterns "Direct link to Case Sensitivity Patterns")

```
[Test]

public async Task Case_Sensitivity()

{

    var text = "TUnit Framework";



    // Case-sensitive (default)

    await Assert.That(text).Contains("TUnit");



    // Case-insensitive

    await Assert.That(text)

        .Contains("tunit")

        .IgnoringCase();



    // Using StringComparison

    await Assert.That(text)

        .Contains("framework")

        .WithComparison(StringComparison.OrdinalIgnoreCase);

}
```

## String Formatting Validation[​](#string-formatting-validation "Direct link to String Formatting Validation")

```
[Test]

public async Task Formatted_String()

{

    var name = "Alice";

    var age = 30;

    var message = $"User {name} is {age} years old";



    await Assert.That(message)

        .IsEqualTo("User Alice is 30 years old")

        .And.Contains(name)

        .And.Contains(age.ToString());

}
```

## Multi-line Strings[​](#multi-line-strings "Direct link to Multi-line Strings")

```
[Test]

public async Task Multiline_String()

{

    var multiline = """

        Line 1

        Line 2

        Line 3

        """;



    await Assert.That(multiline)

        .Contains("Line 1")

        .And.Contains("Line 2")

        .And.Matches("Line.*Line.*Line");

}
```

## Common String Comparison Options[​](#common-string-comparison-options "Direct link to Common String Comparison Options")

```
[Test]

public async Task String_Comparison_Options()

{

    var text = "Hello";



    // Ordinal (binary comparison)

    await Assert.That(text)

        .IsEqualTo("hello")

        .WithComparison(StringComparison.OrdinalIgnoreCase);



    // Current culture

    await Assert.That(text)

        .IsEqualTo("hello")

        .WithComparison(StringComparison.CurrentCultureIgnoreCase);



    // Invariant culture

    await Assert.That(text)

        .IsEqualTo("hello")

        .WithComparison(StringComparison.InvariantCultureIgnoreCase);

}
```

## Path Validation[​](#path-validation "Direct link to Path Validation")

```
[Test]

public async Task File_Path_Validation()

{

    var path = @"C:\Users\Alice\Documents\file.txt";



    await Assert.That(path)

        .Contains(@"\")

        .And.EndsWith(".txt")

        .And.Matches(@"[A-Z]:\\");

}
```

Unix path:

```
[Test]

public async Task Unix_Path_Validation()

{

    var path = "/home/alice/documents/file.txt";



    await Assert.That(path)

        .StartsWith("/")

        .And.Contains("/")

        .And.EndsWith(".txt");

}
```

## Common Patterns[​](#common-patterns "Direct link to Common Patterns")

### Trim and Assert[​](#trim-and-assert "Direct link to Trim and Assert")

```
[Test]

public async Task Trim_Before_Assert()

{

    var input = "  Hello  ";

    var trimmed = input.Trim();



    await Assert.That(trimmed).IsEqualTo("Hello");

}
```

### Case Normalization[​](#case-normalization "Direct link to Case Normalization")

```
[Test]

public async Task Normalize_Case()

{

    var input = "Hello World";

    var lower = input.ToLowerInvariant();



    await Assert.That(lower).IsEqualTo("hello world");

}
```

### Substring Extraction[​](#substring-extraction "Direct link to Substring Extraction")

```
[Test]

public async Task Extract_Substring()

{

    var text = "Hello, World!";

    var greeting = text.Substring(0, 5);



    await Assert.That(greeting).IsEqualTo("Hello");

}
```

## See Also[​](#see-also "Direct link to See Also")

* [Equality & Comparison](/docs/assertions/equality-and-comparison.md) - String equality assertions
* [Collections](/docs/assertions/collections.md) - Working with collections of strings
* [Null & Default](/docs/assertions/null-and-default.md) - Null string checks
