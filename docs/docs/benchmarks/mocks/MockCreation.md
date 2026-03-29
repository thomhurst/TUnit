---
title: "Mock Benchmark: MockCreation"
description: "Mock instance creation performance — TUnit.Mocks vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 5
---

# MockCreation Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock instance creation performance:

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1.277 μs | 0.0104 μs | 0.0097 μs | 1.12 KB |
| Moq | 1.382 μs | 0.0143 μs | 0.0134 μs | 2 KB |
| NSubstitute | 1.902 μs | 0.0084 μs | 0.0078 μs | 4.88 KB |
| FakeItEasy | 1.862 μs | 0.0114 μs | 0.0101 μs | 2.65 KB |
| **'TUnit.Mocks (Repository)'** | 1.270 μs | 0.0072 μs | 0.0067 μs | 1.12 KB |
| 'Moq (Repository)' | 1.319 μs | 0.0048 μs | 0.0043 μs | 1.87 KB |
| 'NSubstitute (Repository)' | 1.911 μs | 0.0093 μs | 0.0087 μs | 4.88 KB |
| 'FakeItEasy (Repository)' | 1.883 μs | 0.0136 μs | 0.0114 μs | 2.65 KB |

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
  title "MockCreation Performance Comparison"
  x-axis ["TUnit.Mocks", "Moq", "NSubstitute", "FakeItEasy", "'TUnit.Mocks (Repository)'", "'Moq (Repository)'", "'NSubstitute (Repository)'", "'FakeItEasy (Repository)'"]
  y-axis "Time (μs)" 0 --> 3
  bar [1.277, 1.382, 1.902, 1.862, 1.27, 1.319, 1.911, 1.883]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock instance creation performance.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-29T03:29:47.877Z*
