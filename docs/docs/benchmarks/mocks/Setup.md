---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-14** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 537.4 ns | 5.93 ns | 5.55 ns | 2.34 KB |
| Imposter | 778.6 ns | 14.40 ns | 13.47 ns | 6.12 KB |
| Mockolate | 434.5 ns | 7.28 ns | 6.81 ns | 2.03 KB |
| Moq | 416,271.5 ns | 1,124.65 ns | 939.14 ns | 28.76 KB |
| NSubstitute | 5,555.9 ns | 25.83 ns | 24.16 ns | 9.01 KB |
| FakeItEasy | 7,968.6 ns | 44.46 ns | 34.71 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 499526
  bar [537.4, 778.6, 434.5, 416271.5, 5555.9, 7968.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 738.8 ns | 14.28 ns | 15.28 ns | 2.93 KB |
| Imposter | 1,583.2 ns | 11.70 ns | 10.94 ns | 10.59 KB |
| Mockolate | 674.4 ns | 6.20 ns | 5.18 ns | 3.07 KB |
| Moq | 111,427.5 ns | 469.64 ns | 439.30 ns | 16.53 KB |
| NSubstitute | 12,198.4 ns | 83.42 ns | 73.95 ns | 20.31 KB |
| FakeItEasy | 7,506.3 ns | 110.46 ns | 103.32 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 133713
  bar [738.8, 1583.2, 674.4, 111427.5, 12198.4, 7506.3]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-14T03:22:19.526Z*
