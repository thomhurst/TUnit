# Deferring Data Source Enumeration

By default, data sources are enumerated during **test discovery** — every row becomes its own test node, so a data source that produces thousands of cases produces thousands of nodes in your IDE's test explorer. For very large data sets this can make discovery slow and the test tree unwieldy.

Setting `DeferEnumeration = true` on a data source tells TUnit **not** to enumerate it during discovery. The test then appears as a **single placeholder node**, and the data source is enumerated into individual cases only when the test is **run**. This is similar to xUnit's `DisableDiscoveryEnumeration`.

```csharp
public static IEnumerable<int> ManyCases() => Enumerable.Range(0, 10_000);

public class MyTests
{
    [Test]
    [MethodDataSource(nameof(ManyCases), DeferEnumeration = true)]
    public async Task MyTest(int input)
    {
        await Assert.That(input).IsGreaterThanOrEqualTo(0);
    }
}
```

With the flag set:

- **Discovery** shows one node for `MyTest` instead of 10,000.
- **Running** `MyTest` enumerates the data source and reports each case as a result nested under the placeholder.

`DeferEnumeration` is available on any data source attribute (`[MethodDataSource]`, `[ClassDataSource]`, custom `DataSourceGenerator` attributes, etc.). If **any** data source on a test sets it, the entire test's case expansion is deferred. It has no effect on `[Arguments]` (a single inline row, so there is nothing to defer).

:::info
The placeholder is reported as a **container**: the individual cases (nested under it) carry the real pass/fail results, and the placeholder's own result aggregates them — it passes only if every case passes, and fails if any case fails. Because it is reported, it adds one extra entry to flat result counts (TRX/console) per deferred test. If the data source itself throws while enumerating, the error surfaces as a failed result at run time (just as a non-deferred data source error would) instead of failing discovery for the whole assembly.
:::

:::warning Trade-offs
Because the individual cases do not exist until runtime, a deferred test:

- cannot have its individual rows selected/filtered from the IDE — you can only run the whole test;
- cannot be targeted by another test's `[DependsOn]`;
- adds **one extra entry** to flat result totals (TRX/console) — the placeholder container — per deferred test, so a 10‑row deferred source reports 11 results. Keep this in mind for CI dashboards or count-based quality gates that compare totals across runs.

Use it for large data sets where reducing discovery overhead matters more than per-row selection.
:::
