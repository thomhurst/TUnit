---
title: MatrixTests
description: Performance benchmark results for MatrixTests
sidebar_position: 5
---

# MatrixTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2025-12-30** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.101
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.7.7 | 571.98 ms | 571.21 ms | 4.201 ms |
| NUnit | 4.4.0 | 1,568.19 ms | 1,570.13 ms | 7.937 ms |
| MSTest | 4.0.2 | 1,503.52 ms | 1,506.56 ms | 10.484 ms |
| xUnit3 | 3.2.1 | 1,595.18 ms | 1,595.22 ms | 9.318 ms |
| **TUnit (AOT)** | 1.7.7 | 79.47 ms | 79.40 ms | 0.397 ms |

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
  y-axis "Time (ms)" 0 --> 1915
  bar [571.98, 1568.19, 1503.52, 1595.18, 79.47]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-12-30T00:29:25.150Z*
