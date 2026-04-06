---
title: MassiveParallelTests
description: Performance benchmark results for MassiveParallelTests
sidebar_position: 4
---

# MassiveParallelTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.28.7 | 664.1 ms | 665.1 ms | 4.80 ms |
| NUnit | 4.5.1 | 1,223.8 ms | 1,220.8 ms | 11.01 ms |
| MSTest | 4.1.0 | 2,964.9 ms | 2,962.8 ms | 12.12 ms |
| xUnit3 | 3.2.2 | 3,099.4 ms | 3,101.2 ms | 10.35 ms |
| **TUnit (AOT)** | 1.28.7 | 229.0 ms | 229.1 ms | 0.49 ms |

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
  title "MassiveParallelTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 3720
  bar [664.1, 1223.8, 2964.9, 3099.4, 229]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-04-06T00:41:40.887Z*
