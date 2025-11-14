---
title: Build Performance
description: Compilation time benchmark results
sidebar_position: 8
---

# Build Performance Benchmark

:::info Last Updated
This benchmark was automatically generated on **2025-11-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.100
:::

## 📊 Results

Compilation time comparison across frameworks:

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.1.10 | 2.051 s | 2.047 s | 0.0286 s |
| Build_NUnit | 4.4.0 | 1.637 s | 1.634 s | 0.0138 s |
| Build_MSTest | 4.0.2 | 1.709 s | 1.706 s | 0.0086 s |
| Build_xUnit3 | 3.2.0 | 1.618 s | 1.615 s | 0.0180 s |

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
  title "Build Time Comparison"
  x-axis ["Build_TUnit", "Build_NUnit", "Build_MSTest", "Build_xUnit3"]
  y-axis "Time (s)" 0 --> 3
  bar [2.051, 1.637, 1.709, 1.618]
```

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-11-14T00:28:50.468Z*
