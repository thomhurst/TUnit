---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-23** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 505.5 ns | 3.56 ns | 3.33 ns | 2.31 KB |
| Imposter | 766.9 ns | 4.68 ns | 3.91 ns | 6.12 KB |
| Mockolate | 322.4 ns | 1.49 ns | 1.17 ns | 1.65 KB |
| Moq | 425,511.3 ns | 1,122.34 ns | 937.21 ns | 28.55 KB |
| NSubstitute | 5,529.3 ns | 31.18 ns | 27.64 ns | 9.01 KB |
| FakeItEasy | 8,191.7 ns | 68.75 ns | 64.31 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 510614
  bar [505.5, 766.9, 322.4, 425511.3, 5529.3, 8191.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 740.6 ns | 3.25 ns | 3.04 ns | 3.09 KB |
| Imposter | 1,331.4 ns | 26.31 ns | 28.15 ns | 10.59 KB |
| Mockolate | 539.6 ns | 2.38 ns | 2.11 ns | 2.6 KB |
| Moq | 113,475.6 ns | 327.66 ns | 273.61 ns | 16.53 KB |
| NSubstitute | 11,943.4 ns | 153.18 ns | 135.79 ns | 20.31 KB |
| FakeItEasy | 8,207.8 ns | 149.09 ns | 139.46 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 136171
  bar [740.6, 1331.4, 539.6, 113475.6, 11943.4, 8207.8]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-23T03:25:20.859Z*
