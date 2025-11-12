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
| **TUnit** | 1.0.78 | 2.061 s | 2.060 s | 0.0202 s |
| Build_NUnit | 4.4.0 | 1.645 s | 1.645 s | 0.0147 s |
| Build_MSTest | 4.0.2 | 1.712 s | 1.710 s | 0.0132 s |
| Build_xUnit3 | 3.2.0 | 1.615 s | 1.619 s | 0.0149 s |

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
  bar [2.061, 1.645, 1.712, 1.615]
```

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: 2025-11-12T00:44:12.201Z*
