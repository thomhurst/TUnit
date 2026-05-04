---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-04** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 445.0 ns | 8.92 ns | 11.91 ns | 2.01 KB |
| Imposter | 842.6 ns | 16.50 ns | 27.11 ns | 6.12 KB |
| Mockolate | 364.0 ns | 5.84 ns | 5.46 ns | 1.68 KB |
| Moq | 427,515.9 ns | 2,142.03 ns | 1,898.86 ns | 28.6 KB |
| NSubstitute | 5,761.0 ns | 67.82 ns | 63.44 ns | 9.06 KB |
| FakeItEasy | 8,288.3 ns | 44.02 ns | 39.02 ns | 10.45 KB |

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
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 513020
  bar [445, 842.6, 364, 427515.9, 5761, 8288.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 643.0 ns | 12.56 ns | 17.19 ns | 2.59 KB |
| Imposter | 1,423.6 ns | 27.85 ns | 28.60 ns | 10.59 KB |
| Mockolate | 658.6 ns | 11.05 ns | 9.80 ns | 2.82 KB |
| Moq | 116,178.4 ns | 975.27 ns | 912.27 ns | 16.64 KB |
| NSubstitute | 12,327.7 ns | 84.31 ns | 78.86 ns | 20.34 KB |
| FakeItEasy | 8,025.4 ns | 60.87 ns | 53.96 ns | 11.71 KB |

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
  title "Setup (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 139415
  bar [643, 1423.6, 658.6, 116178.4, 12327.7, 8025.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-04T03:27:14.154Z*
