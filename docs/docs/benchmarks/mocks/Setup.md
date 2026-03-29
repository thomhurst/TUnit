---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1.920 μs | 0.0196 μs | 0.0174 μs | 3.36 KB |
| Moq | 430.805 μs | 3.1114 μs | 2.9104 μs | 28.52 KB |
| NSubstitute | 5.547 μs | 0.0392 μs | 0.0367 μs | 9.06 KB |
| FakeItEasy | 8.111 μs | 0.0610 μs | 0.0540 μs | 10.45 KB |
| **'TUnit.Mocks (Multiple)'** | 2.213 μs | 0.0345 μs | 0.0306 μs | 4.43 KB |
| 'Moq (Multiple)' | 116.728 μs | 0.6735 μs | 0.6300 μs | 16.53 KB |
| 'NSubstitute (Multiple)' | 12.361 μs | 0.0943 μs | 0.0836 μs | 20.5 KB |
| 'FakeItEasy (Multiple)' | 7.867 μs | 0.0858 μs | 0.0760 μs | 11.71 KB |

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
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1'
  }
}}%%
xychart-beta
  title "Setup Performance Comparison"
  x-axis ["TUnit.Mocks", "Moq", "NSubstitute", "FakeItEasy", "'TUnit.Mocks (Multiple)'", "'Moq (Multiple)'", "'NSubstitute (Multiple)'", "'FakeItEasy (Multiple)'"]
  y-axis "Time (μs)" 0 --> 517
  bar [1.92, 430.805, 5.547, 8.111, 2.213, 116.728, 12.361, 7.867]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-29T03:29:47.877Z*
