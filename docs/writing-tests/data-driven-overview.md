# Choosing a Data Approach

TUnit offers several ways to provide data to your tests. Use this guide to pick the right one.

## Decision Table[​](#decision-table "Direct link to Decision Table")

| Scenario                            | Approach                             | Page                                                                 |
| ----------------------------------- | ------------------------------------ | -------------------------------------------------------------------- |
| Fixed inline values                 | `[Arguments(...)]`                   | [Arguments](/docs/writing-tests/arguments.md)                        |
| Data from a method                  | `[MethodDataSource]`                 | [Method Data Sources](/docs/writing-tests/method-data-source.md)     |
| Shared object with lifecycle        | `[ClassDataSource<T>]`               | [Class Data Source](/docs/writing-tests/class-data-source.md)        |
| Reusable data rows                  | `[TestDataRow<T>]`                   | [Test Data Row](/docs/writing-tests/test-data-row.md)                |
| All parameter combinations          | `[MatrixDataSource]`                 | [Matrix Tests](/docs/writing-tests/matrix-tests.md)                  |
| Multiple sources on one method      | Combined attributes                  | [Combined Data Sources](/docs/writing-tests/combined-data-source.md) |
| Hierarchical injection              | Nested properties                    | [Nested Data Sources](/docs/writing-tests/nested-data-sources.md)    |
| Custom generic attributes           | `[GenerateGenericTest(typeof(...))]` | [Generic Attributes](/docs/writing-tests/generic-attributes.md)      |
| Huge data set (reduce IDE overhead) | `DeferEnumeration = true`            | [Defer Enumeration](/docs/writing-tests/defer-enumeration.md)        |

## Quick Examples[​](#quick-examples "Direct link to Quick Examples")

### Inline arguments[​](#inline-arguments "Direct link to Inline arguments")

```
[Test]

[Arguments(1, 2, 3)]

[Arguments(0, 0, 0)]

public async Task Add_ReturnsSum(int a, int b, int expected)

{

    await Assert.That(a + b).IsEqualTo(expected);

}
```

### Method data source[​](#method-data-source "Direct link to Method data source")

```
[Test]

[MethodDataSource(nameof(GetCases))]

public async Task MyTest(string input)

{

    await Assert.That(input).IsNotEmpty();

}



public static IEnumerable<string> GetCases() => ["hello", "world"];
```

### Class data source (shared fixture)[​](#class-data-source-shared-fixture "Direct link to Class data source (shared fixture)")

```
[ClassDataSource<DatabaseFixture>(Shared = SharedType.PerTestSession)]

public class MyTests(DatabaseFixture db)

{

    [Test]

    public async Task QueryWorks()

    {

        var result = await db.QueryAsync("SELECT 1");

        await Assert.That(result).IsNotNull();

    }

}
```

### Matrix (combinatorial)[​](#matrix-combinatorial "Direct link to Matrix (combinatorial)")

```
[Test]

[MatrixDataSource]

public async Task Multiply(

    [Matrix(2, 3)] int a,

    [Matrix(4, 5)] int b)

{

    await Assert.That(a * b).IsGreaterThan(0);

}

// Generates: (2,4), (2,5), (3,4), (3,5)
```

## Quick Rules[​](#quick-rules "Direct link to Quick Rules")

* **`[Arguments]`** is the simplest — use it when values are known at compile time.
* **`[MethodDataSource]`** is best for computed or complex data.
* **`[ClassDataSource<T>]`** manages object lifecycles (initialization, disposal, sharing across tests).
* **`[MatrixDataSource]`** generates the Cartesian product of all `[Matrix]` parameter values.
* Attributes can be combined on a single method — see [Combined Data Sources](/docs/writing-tests/combined-data-source.md).
