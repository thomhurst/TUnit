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
| **TUnit** | 1.1.0 | 536.11 ms | 532.73 ms | 6.770 ms |
| NUnit | 4.4.0 | 1,556.59 ms | 1,554.77 ms | 10.335 ms |
| MSTest | 4.0.2 | 1,526.36 ms | 1,525.39 ms | 3.795 ms |
| xUnit3 | 3.2.0 | 1,603.59 ms | 1,604.07 ms | 8.271 ms |
| **TUnit (AOT)** | 1.1.0 | 77.73 ms | 77.78 ms | 0.214 ms |

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
  y-axis "Time (ms)" 0 --> 1925
  bar [536.11, 1556.59, 1526.36, 1603.59, 77.73]
```

## 🎯 Key Insights

- **2.90x faster** than NUnit (4.4.0)
- **2.85x faster** than MSTest (4.0.2)
- **2.99x faster** than xUnit3 (3.2.0)
- **6.90x faster** with Native AOT compilation

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-11-12T20:28:34.075Z*
