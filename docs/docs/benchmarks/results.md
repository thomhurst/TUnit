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
| **TUnit** | 1.1.0 | 564.8 ms | 564.8 ms | 5.34 ms |
| NUnit | 4.4.0 | 1,156.4 ms | 1,156.4 ms | 5.00 ms |
| MSTest | 4.0.2 | 1,132.6 ms | 1,130.6 ms | 9.92 ms |
| xUnit3 | 3.2.0 | 1,200.7 ms | 1,200.4 ms | 5.74 ms |
| **TUnit (AOT)** | 1.1.0 | NA | NA | NA |

## 📈 Visual Comparison

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#ffffff',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'mainBkg': '#2563eb',
    'secondBkg': '#7c3aed',
    'tertiaryBkg': '#dc2626',
    'clusterBkg': '#f3f4f6',
    'edgeLabelBackground': '#ffffff',
    'tertiaryTextColor': '#1f2937',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1',
    'pie9': '#84cc16',
    'pie10': '#f97316',
    'pie11': '#14b8a6',
    'pie12': '#a855f7'
  }
}}%%
xychart-beta
  title "results Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 1441
  bar [564.8, 1156.4, 1132.6, 1200.7, 0]
```

## 🎯 Key Insights

- **2.05x faster** than NUnit (4.4.0)
- **2.01x faster** than MSTest (4.0.2)
- **2.13x faster** than xUnit3 (3.2.0)
- **Infinityx faster** with Native AOT compilation

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-11-12T20:55:56.395Z*
