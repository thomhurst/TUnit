---
title: results
description: Performance benchmark results for results
sidebar_position: 2
---

# results Benchmark

:::info Last Updated
This benchmark was automatically generated on **2025-11-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.100
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.0.78 | 569.0 ms | 568.5 ms | 3.62 ms |
| NUnit | 4.4.0 | 691.2 ms | 691.0 ms | 5.19 ms |
| MSTest | 4.0.2 | 658.5 ms | 655.6 ms | 5.84 ms |
| xUnit3 | 3.2.0 | 738.2 ms | 735.9 ms | 7.49 ms |
| **TUnit (AOT)** | 1.0.78 | 124.2 ms | 124.4 ms | 0.42 ms |

## 📈 Visual Comparison

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#10b981',
    'primaryTextColor': '#fff',
    'primaryBorderColor': '#059669',
    'lineColor': '#d1d5db',
    'secondaryColor': '#3b82f6',
    'tertiaryColor': '#f59e0b',
    'background': '#ffffff',
    'mainBkg': '#10b981',
    'secondBkg': '#ef4444',
    'tertiaryBkg': '#f59e0b'
  }
}}%%
xychart-beta
  title "results Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 886
  bar [569, 691.2, 658.5, 738.2, 124.2]
```

## 🎯 Key Insights

- **1.21x faster** than NUnit (4.4.0)
- **1.16x faster** than MSTest (4.0.2)
- **1.30x faster** than xUnit3 (3.2.0)
- **4.58x faster** with Native AOT compilation

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-11-12T00:44:12.201Z*
