---
title: SetupTeardownTests
description: Performance benchmark results for SetupTeardownTests
sidebar_position: 7
---

# SetupTeardownTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.200
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.19.22 | 568.0 ms | 568.0 ms | 4.00 ms |
| NUnit | 4.5.1 | 1,163.8 ms | 1,164.5 ms | 7.69 ms |
| MSTest | 4.1.0 | 1,117.8 ms | 1,119.2 ms | 7.13 ms |
| xUnit3 | 3.2.2 | 1,235.4 ms | 1,236.5 ms | 5.44 ms |
| **TUnit (AOT)** | 1.19.22 | NA | NA | NA |

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
  y-axis "Time (ms)" 0 --> 1483
  bar [568, 1163.8, 1117.8, 1235.4, 0]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-03-12T00:31:51.553Z*
