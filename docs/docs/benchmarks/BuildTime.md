---
title: Build Performance
description: Compilation time benchmark results
sidebar_position: 8
---

# Build Performance Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-01-11** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.101
:::

## 📊 Results

Compilation time comparison across frameworks:

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.9.55 | 2.046 s | 2.054 s | 0.0196 s |
| Build_NUnit | 4.4.0 | 1.638 s | 1.637 s | 0.0149 s |
| Build_MSTest | 4.0.2 | 1.796 s | 1.799 s | 0.0138 s |
| Build_xUnit3 | 3.2.1 | 1.728 s | 1.726 s | 0.0218 s |

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
  bar [2.046, 1.638, 1.796, 1.728]
```

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2026-01-11T00:32:36.340Z*
