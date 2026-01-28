---
title: MatrixTests
description: Performance benchmark results for MatrixTests
sidebar_position: 5
---

# MatrixTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-01-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.102
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.12.65 | 502.32 ms | 502.39 ms | 2.981 ms |
| NUnit | 4.4.0 | 1,538.44 ms | 1,541.41 ms | 9.097 ms |
| MSTest | 4.0.2 | 1,477.14 ms | 1,471.96 ms | 12.304 ms |
| xUnit3 | 3.2.2 | 1,590.89 ms | 1,589.89 ms | 6.794 ms |
| **TUnit (AOT)** | 1.12.65 | 73.81 ms | 73.76 ms | 0.281 ms |

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
  title "MatrixTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 1910
  bar [502.32, 1538.44, 1477.14, 1590.89, 73.81]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-01-28T00:31:02.193Z*
