# Test Filters

Running TUnit via `dotnet run` supports test filters.

TUnit can select tests by:

- Assembly
- Namespace
- Class name
- Test name

You must use the `--treenode-filter` flag on the command line.

The syntax for the filter value is (without the angled brackets) `/<Assembly>/<Namespace>/<Class name>/<Test name>`

Wildcards are also supported with `*`

## Filter Operators

TUnit supports several operators for building complex filters:

- **Wildcard matching:** Use `*` for pattern matching (e.g., `LoginTests*` matches `LoginTests`, `LoginTestsSuite`, etc.)
- **Equality:** Use `=` for exact match (e.g., `[Category=Unit]`)
- **Negation:** Use `!=` for excluding values (e.g., `[Category!=Performance]`)
- **AND operator:** Use `&` to combine conditions (e.g., `[Category=Unit]&[Priority=High]`)
- **OR operator:** Use `|` to match either condition within a single path segment - requires parentheses (e.g., `/*/*/(Class1)|(Class2)/*`)

For full information on the treenode filters, see [Microsoft's documentation](https://github.com/microsoft/testfx/blob/main/docs/mstest-runner-graphqueryfiltering/graph-query-filtering.md)

## Examples

### Filter by class name

Run all tests in the `LoginTests` class:

```bash
dotnet run --treenode-filter "/*/*/LoginTests/*"
```

### Filter by test name

Run all tests with the name `AcceptCookiesTest`:

```bash
dotnet run --treenode-filter "/*/*/*/AcceptCookiesTest"
```

### Filter by namespace

Run all tests in a specific namespace:

```bash
dotnet run --treenode-filter "/*/MyProject.Tests.Integration/*/*"
```

Use a wildcard to match namespace prefixes:

```bash
dotnet run --treenode-filter "/*/MyProject.Tests.Api*/*/*"
```

### Filter by custom property value

TUnit supports filtering by [custom properties](../writing-tests/test-context.md#custom-properties). If a test has a property with a matching name and value, it will be included:

```bash
dotnet run --treenode-filter "/*/*/*/*[Category=Smoke]"
```

Use a wildcard to match partial property values:

```bash
dotnet run --treenode-filter "/*/*/*/*[Owner=*Team-Backend*]"
```

### Exclude tests by property

Use `!=` to exclude tests with a specific property value:

```bash
dotnet run --treenode-filter "/*/*/*/*[Category!=Slow]"
```

### Combined filters

Combine multiple conditions with `&` to narrow results. Run only high-priority smoke tests:

```bash
dotnet run --treenode-filter "/*/*/*/*[Category=Smoke]&[Priority=High]"
```

Combine namespace and property filters. Run integration tests tagged as critical:

```bash
dotnet run --treenode-filter "/*/MyProject.Tests.Integration/*/*/*[Priority=Critical]"
```

### OR filter across classes

Run tests from either of two classes:

```bash
dotnet run --treenode-filter "/*/*/(LoginTests)|(SignupTests)/*"
```
