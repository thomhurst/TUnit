---
title: SetupTeardownTests
description: Performance benchmark results for SetupTeardownTests
sidebar_position: 7
---

# SetupTeardownTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-21** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.37.10 | 567.4 ms | 566.4 ms | 6.91 ms |
| NUnit | 4.5.1 | 1,186.4 ms | 1,188.4 ms | 8.36 ms |
| MSTest | 4.2.1 | 1,107.8 ms | 1,108.1 ms | 14.20 ms |
| xUnit3 | 3.2.2 | 1,247.9 ms | 1,248.2 ms | 12.75 ms |
| **TUnit (AOT)** | 1.37.10 | NA | NA | NA |

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
  y-axis "Time (ms)" 0 --> 1498
  bar [567.4, 1186.4, 1107.8, 1247.9, 0]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-04-21T00:46:15.219Z*
