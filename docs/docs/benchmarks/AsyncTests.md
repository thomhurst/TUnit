---
title: AsyncTests
description: Performance benchmark results for AsyncTests
sidebar_position: 2
---

# AsyncTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-02-07** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.102
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.13.8 | 522.6 ms | 522.0 ms | 2.59 ms |
| NUnit | 4.4.0 | 668.2 ms | 666.8 ms | 6.38 ms |
| MSTest | 4.1.0 | 608.7 ms | 607.5 ms | 6.51 ms |
| xUnit3 | 3.2.2 | 735.0 ms | 732.5 ms | 9.82 ms |
| **TUnit (AOT)** | 1.13.8 | 121.8 ms | 121.8 ms | 0.38 ms |

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
  y-axis "Time (ms)" 0 --> 882
  bar [522.6, 668.2, 608.7, 735, 121.8]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-02-07T00:34:22.515Z*
