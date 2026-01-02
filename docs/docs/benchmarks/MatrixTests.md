---
title: MatrixTests
description: Performance benchmark results for MatrixTests
sidebar_position: 5
---

# MatrixTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-01-02** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.101
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.7.16 | 674.15 ms | 671.77 ms | 11.942 ms |
| NUnit | 4.4.0 | 1,588.16 ms | 1,588.48 ms | 4.673 ms |
| MSTest | 4.0.2 | 1,529.69 ms | 1,528.42 ms | 5.728 ms |
| xUnit3 | 3.2.1 | 1,624.99 ms | 1,624.34 ms | 13.133 ms |
| **TUnit (AOT)** | 1.7.16 | 80.95 ms | 80.91 ms | 0.361 ms |

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
  y-axis "Time (ms)" 0 --> 1950
  bar [674.15, 1588.16, 1529.69, 1624.99, 80.95]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-01-02T00:29:55.107Z*
