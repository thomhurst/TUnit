---
title: SetupTeardownTests
description: Performance benchmark results for SetupTeardownTests
sidebar_position: 7
---

# SetupTeardownTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.36.0 | 561.5 ms | 561.7 ms | 3.07 ms |
| NUnit | 4.5.1 | 1,176.5 ms | 1,176.1 ms | 8.59 ms |
| MSTest | 4.2.1 | 1,094.9 ms | 1,096.4 ms | 6.18 ms |
| xUnit3 | 3.2.2 | 1,232.7 ms | 1,231.8 ms | 8.14 ms |
| **TUnit (AOT)** | 1.36.0 | NA | NA | NA |

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
  title "SetupTeardownTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 1480
  bar [561.5, 1176.5, 1094.9, 1232.7, 0]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-04-18T00:41:09.635Z*
