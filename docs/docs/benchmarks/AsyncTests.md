---
title: AsyncTests
description: Performance benchmark results for AsyncTests
sidebar_position: 2
---

# AsyncTests Benchmark

:::info Last Updated
This benchmark was automatically generated on **2025-12-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.100
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.3.25 | 576.1 ms | 576.6 ms | 4.65 ms |
| NUnit | 4.4.0 | 692.6 ms | 693.9 ms | 5.54 ms |
| MSTest | 4.0.2 | 666.6 ms | 666.1 ms | 3.88 ms |
| xUnit3 | 3.2.1 | 742.9 ms | 743.1 ms | 5.00 ms |
| **TUnit (AOT)** | 1.3.25 | 124.7 ms | 124.8 ms | 0.34 ms |

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
  title "AsyncTests Performance Comparison"
  x-axis ["TUnit", "NUnit", "MSTest", "xUnit3", "TUnit_AOT"]
  y-axis "Time (ms)" 0 --> 892
  bar [576.1, 692.6, 666.6, 742.9, 124.7]
```

## 🎯 Key Insights

This benchmark compares TUnit's performance against NUnit, MSTest, xUnit3 using identical test scenarios.

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-12-04T00:29:39.691Z*
