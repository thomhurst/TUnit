---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 427.3 ns | 8.58 ns | 11.46 ns | 2.01 KB |
| Imposter | 802.7 ns | 10.17 ns | 8.49 ns | 6.12 KB |
| Mockolate | 343.8 ns | 6.60 ns | 8.35 ns | 1.65 KB |
| Moq | 432,282.5 ns | 1,965.21 ns | 1,838.26 ns | 28.94 KB |
| NSubstitute | 5,508.7 ns | 52.49 ns | 49.10 ns | 9.01 KB |
| FakeItEasy | 8,074.3 ns | 51.59 ns | 45.74 ns | 10.57 KB |

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
  y-axis "Time (ns)" 0 --> 518739
  bar [427.3, 802.7, 343.8, 432282.5, 5508.7, 8074.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 613.3 ns | 12.33 ns | 11.53 ns | 2.59 KB |
| Imposter | 1,408.6 ns | 27.22 ns | 40.74 ns | 10.59 KB |
| Mockolate | 567.4 ns | 11.13 ns | 11.91 ns | 2.6 KB |
| Moq | 116,209.3 ns | 786.76 ns | 735.94 ns | 16.53 KB |
| NSubstitute | 11,925.2 ns | 187.77 ns | 175.64 ns | 20.5 KB |
| FakeItEasy | 7,573.8 ns | 83.15 ns | 69.44 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 139452
  bar [613.3, 1408.6, 567.4, 116209.3, 11925.2, 7573.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-18T03:29:10.052Z*
