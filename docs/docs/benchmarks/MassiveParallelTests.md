---
title: MassiveParallelTests
description: Performance benchmark results for MassiveParallelTests
sidebar_position: 4
---

# MassiveParallelTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2025-12-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.100
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.3.25 | 602.1 ms | 601.2 ms | 4.68 ms |
| NUnit | 4.4.0 | 1,211.4 ms | 1,211.3 ms | 4.55 ms |
| MSTest | 4.0.2 | 2,995.8 ms | 2,991.3 ms | 10.78 ms |
| xUnit3 | 3.2.1 | 3,082.3 ms | 3,081.2 ms | 7.17 ms |
| **TUnit (AOT)** | 1.3.25 | 131.0 ms | 131.1 ms | 0.48 ms |

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
  title "MassiveParallelTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 3699
  bar [602.1, 1211.4, 2995.8, 3082.3, 131]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-12-04T00:29:39.692Z*
