---
title: DataDrivenTests
description: Performance benchmark results for DataDrivenTests
sidebar_position: 3
---

# DataDrivenTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.200
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.19.22 | 481.19 ms | 480.87 ms | 5.309 ms |
| NUnit | 4.5.1 | 581.32 ms | 580.68 ms | 10.282 ms |
| MSTest | 4.1.0 | 556.50 ms | 559.78 ms | 9.837 ms |
| xUnit3 | 3.2.2 | 624.51 ms | 622.92 ms | 7.306 ms |
| **TUnit (AOT)** | 1.19.22 | 25.31 ms | 25.36 ms | 0.679 ms |

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
  title "DataDrivenTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 750
  bar [481.19, 581.32, 556.5, 624.51, 25.31]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-03-12T00:31:51.552Z*
