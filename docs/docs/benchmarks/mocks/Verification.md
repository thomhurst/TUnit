---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-29** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1.674 μs | 0.0300 μs | 0.0281 μs | 4.11 KB |
| Moq | 255.272 μs | 1.0882 μs | 0.9646 μs | 23.74 KB |
| NSubstitute | 6.264 μs | 0.0382 μs | 0.0357 μs | 9.83 KB |
| FakeItEasy | 7.047 μs | 0.0297 μs | 0.0263 μs | 10.48 KB |
| **'TUnit.Mocks (Never)'** | 1.193 μs | 0.0182 μs | 0.0170 μs | 1.58 KB |
| 'Moq (Never)' | 66.322 μs | 0.3183 μs | 0.2978 μs | 6.76 KB |
| 'NSubstitute (Never)' | 3.397 μs | 0.0226 μs | 0.0211 μs | 6.92 KB |
| 'FakeItEasy (Never)' | 3.475 μs | 0.0475 μs | 0.0445 μs | 5.1 KB |
| **'TUnit.Mocks (Multiple)'** | 2.462 μs | 0.0491 μs | 0.1077 μs | 6.26 KB |
| 'Moq (Multiple)' | 350.992 μs | 2.0233 μs | 1.7936 μs | 33.86 KB |
| 'NSubstitute (Multiple)' | 11.517 μs | 0.0277 μs | 0.0246 μs | 16.49 KB |
| 'FakeItEasy (Multiple)' | 12.344 μs | 0.1133 μs | 0.1060 μs | 18.79 KB |

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
  title "Verification Performance Comparison"
  x-axis ["TUnit.Mocks", "Moq", "NSubstitute", "FakeItEasy", "'TUnit.Mocks (Never)'", "'Moq (Never)'", "'NSubstitute (Never)'", "'FakeItEasy (Never)'", "'TUnit.Mocks (Multiple)'", "'Moq (Multiple)'", "'NSubstitute (Multiple)'", "'FakeItEasy (Multiple)'"]
  y-axis "Time (μs)" 0 --> 422
  bar [1.674, 255.272, 6.264, 7.047, 1.193, 66.322, 3.397, 3.475, 2.462, 350.992, 11.517, 12.344]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-29T03:29:47.877Z*
