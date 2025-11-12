---
title: Build Performance
description: Compilation time benchmark results
sidebar_position: 3
---

# Build Performance Benchmark

:::info Last Updated
This benchmark was automatically generated on **2025-11-12** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.100
:::

## 📊 Results

Compilation time comparison across frameworks:

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
| **TUnit** | 1.1.0 | 2.033 s | 2.032 s | 0.0274 s |
| Build_NUnit | 4.4.0 | 1.626 s | 1.628 s | 0.0146 s |
| Build_MSTest | 4.0.2 | 1.700 s | 1.694 s | 0.0153 s |
| Build_xUnit3 | 3.2.0 | 1.607 s | 1.602 s | 0.0149 s |

## 📈 Visual Comparison

```mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#10b981',
    'primaryTextColor': '#fff',
    'primaryBorderColor': '#059669',
    'lineColor': '#d1d5db',
    'secondaryColor': '#3b82f6',
    'tertiaryColor': '#f59e0b',
    'background': '#ffffff',
    'mainBkg': '#10b981',
    'secondBkg': '#ef4444',
    'tertiaryBkg': '#f59e0b'
  }
}}%%
xychart-beta
  title "Build Time Comparison"
  x-axis ["Build_TUnit", "Build_NUnit", "Build_MSTest", "Build_xUnit3"]
  y-axis "Time (s)" 0 --> 3
  bar [2.033, 1.626, 1.7, 1.607]
```

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-11-12T20:28:34.075Z*
