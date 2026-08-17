# Test Filters

Running TUnit via `dotnet run` supports test filters.

Coming from xUnit, NUnit, or MSTest?

TUnit runs on Microsoft.Testing.Platform, not VSTest. The familiar `dotnet test --filter "Category=X"` is **not supported** — the flag is silently rejected, the MTP help is printed, and the run exits with `Zero tests ran`. This can look like a test failure when it's actually just an unrecognised flag.

Use `--treenode-filter` instead:

| VSTest (xUnit / NUnit / MSTest)            | TUnit                                                |
| ------------------------------------------ | ---------------------------------------------------- |
| `--filter "Category=Integration"`          | `--treenode-filter "/*/*/*/*[Category=Integration]"` |
| `--filter "FullyQualifiedName~LoginTests"` | `--treenode-filter "/*/*/LoginTests/*"`              |
| `--filter "Name=AcceptCookiesTest"`        | `--treenode-filter "/*/*/*/AcceptCookiesTest"`       |

When using `dotnet test`, pass the flag directly: `dotnet test --treenode-filter "..."`. (On older SDKs, separate it with `--`: `dotnet test -- --treenode-filter "..."`.)

TUnit can select tests by:

* Assembly
* Namespace
* Class name
* Test name

You must use the `--treenode-filter` flag on the command line.

The syntax for the filter value is (without the angled brackets) `/<Assembly>/<Namespace>/<Class name>/<Test name>`

Wildcards are also supported with `*`

## Filter Operators[​](#filter-operators "Direct link to Filter Operators")

TUnit supports several operators for building complex filters:

* **Wildcard matching:** Use `*` for pattern matching (e.g., `LoginTests*` matches `LoginTests`, `LoginTestsSuite`, etc.)

* **Equality:** Use `=` for exact match (e.g., `[Category=Unit]`)

* **Negation:** Use `!=` for excluding values (e.g., `[Category!=Performance]`)

* **AND operator:** Use `&` to combine conditions within a single path segment or property group, with each side wrapped in parentheses. Examples:

  <!-- -->

  * AND across path patterns: `/*/*/(ClassA*)&(*Smoke)/*`
  * AND across properties: `/**[(Category=Unit)&(Priority=High)]`

* **OR operator:** Use `|` the same way — within a single segment or property group, with parentheses. Examples:

  <!-- -->

  * OR across classes: `/*/*/(Class1)|(Class2)/*`
  * OR across properties: `/**[(Category=Smoke)|(Priority=High)]`

* **Match-all:** `**` matches any path depth (e.g., `/**` or `/MyAssembly/**`). It must appear at the end of the path — `/**/Path` is not allowed.

Only one property group `[...]` is permitted per path segment, so combine multiple property conditions inside a single bracket (e.g., `[(A=1)&(B=2)]`, not `[A=1]&[B=2]`).

For full information on the treenode filters, see [Microsoft's documentation](https://github.com/microsoft/testfx/blob/main/docs/mstest-runner-graphqueryfiltering/graph-query-filtering.md)

## Examples[​](#examples "Direct link to Examples")

### Filter by class name[​](#filter-by-class-name "Direct link to Filter by class name")

Run all tests in the `LoginTests` class:

```
dotnet run --treenode-filter "/*/*/LoginTests/*"
```

### Filter by test name[​](#filter-by-test-name "Direct link to Filter by test name")

Run all tests with the name `AcceptCookiesTest`:

```
dotnet run --treenode-filter "/*/*/*/AcceptCookiesTest"
```

### Filter by namespace[​](#filter-by-namespace "Direct link to Filter by namespace")

Run all tests in a specific namespace:

```
dotnet run --treenode-filter "/*/MyProject.Tests.Integration/*/*"
```

Use a wildcard to match namespace prefixes:

```
dotnet run --treenode-filter "/*/MyProject.Tests.Api*/*/*"
```

### Filter by custom property value[​](#filter-by-custom-property-value "Direct link to Filter by custom property value")

TUnit supports filtering by [custom properties](/docs/writing-tests/test-context.md#custom-properties). If a test has a property with a matching name and value, it will be included:

```
dotnet run --treenode-filter "/*/*/*/*[Category=Smoke]"
```

Use a wildcard to match partial property values:

```
dotnet run --treenode-filter "/*/*/*/*[Owner=*Team-Backend*]"
```

### Exclude tests by property[​](#exclude-tests-by-property "Direct link to Exclude tests by property")

Use `!=` to exclude tests with a specific property value:

```
dotnet run --treenode-filter "/*/*/*/*[Category!=Slow]"
```

### Combined filters[​](#combined-filters "Direct link to Combined filters")

Combine multiple property conditions inside a single bracket group with `&`. Run only high-priority smoke tests:

```
dotnet run --treenode-filter "/*/*/*/*[(Category=Smoke)&(Priority=High)]"
```

Combine namespace and property filters. Run integration tests tagged as critical:

```
dotnet run --treenode-filter "/*/MyProject.Tests.Integration/*/*[Priority=Critical]"
```

### OR filter across classes[​](#or-filter-across-classes "Direct link to OR filter across classes")

Run tests from either of two classes:

```
dotnet run --treenode-filter "/*/*/(LoginTests)|(SignupTests)/*"
```

### OR filter across different properties[​](#or-filter-across-different-properties "Direct link to OR filter across different properties")

Run tests matching either property. The OR must live inside a single bracket group, with each condition wrapped in parentheses:

```
dotnet run --treenode-filter "/**[(Category=Smoke)|(Priority=High)]"
```

Note that `[Category=Smoke]|[Priority=High]` (separate brackets) is **not** valid — only one `[...]` group is allowed per path segment, so every property condition must live inside it.
