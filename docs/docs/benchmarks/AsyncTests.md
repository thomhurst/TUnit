---
title: AsyncTests
description: Performance benchmark results for AsyncTests
sidebar_position: 2
---

# AsyncTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-01-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.101
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.7.16 | 655.0 ms | 653.6 ms | 12.00 ms |
| NUnit | 4.4.0 | 689.1 ms | 688.2 ms | 5.99 ms |
| MSTest | 4.0.2 | 630.8 ms | 630.9 ms | 6.58 ms |
| xUnit3 | 3.2.1 | 719.3 ms | 718.0 ms | 9.02 ms |
| **TUnit (AOT)** | 1.7.16 | 124.0 ms | 124.0 ms | 0.31 ms |

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
  title "AsyncTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 864
  bar [655, 689.1, 630.8, 719.3, 124]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-01-02T00:29:55.106Z*
