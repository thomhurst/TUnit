---
title: MatrixTests
description: Performance benchmark results for MatrixTests
sidebar_position: 5
---

# MatrixTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.44.0 | 551.9 ms | 551.9 ms | 1.67 ms |
| NUnit | 4.6.0 | 1,590.8 ms | 1,590.6 ms | 7.24 ms |
| MSTest | 4.2.2 | 1,490.3 ms | 1,490.4 ms | 3.36 ms |
| xUnit3 | 3.2.2 | 1,632.4 ms | 1,628.6 ms | 6.91 ms |
| **TUnit (AOT)** | 1.44.0 | 125.4 ms | 125.3 ms | 0.28 ms |

## 📈 Visual Comparison

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#1f2937',
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
  title "MatrixTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 1959
  bar [551.9, 1590.8, 1490.3, 1632.4, 125.4]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-05-11T00:54:14.876Z*
