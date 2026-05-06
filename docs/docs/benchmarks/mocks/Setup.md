---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 420.4 ns | 3.42 ns | 3.20 ns | 2.01 KB |
| Imposter | 761.4 ns | 10.18 ns | 9.53 ns | 6.12 KB |
| Mockolate | 361.2 ns | 7.15 ns | 9.79 ns | 1.68 KB |
| Moq | 426,086.4 ns | 1,616.95 ns | 1,512.49 ns | 28.69 KB |
| NSubstitute | 5,499.1 ns | 78.07 ns | 73.02 ns | 9.01 KB |
| FakeItEasy | 8,184.3 ns | 100.19 ns | 88.82 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 511304
  bar [420.4, 761.4, 361.2, 426086.4, 5499.1, 8184.3]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 603.1 ns | 6.26 ns | 5.55 ns | 2.59 KB |
| Imposter | 1,351.6 ns | 25.99 ns | 25.53 ns | 10.59 KB |
| Mockolate | 603.6 ns | 3.16 ns | 2.95 ns | 2.82 KB |
| Moq | 112,461.1 ns | 464.25 ns | 434.26 ns | 16.53 KB |
| NSubstitute | 11,790.2 ns | 90.50 ns | 80.23 ns | 20.31 KB |
| FakeItEasy | 7,663.9 ns | 78.05 ns | 65.17 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 134954
  bar [603.1, 1351.6, 603.6, 112461.1, 11790.2, 7663.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-06T03:25:44.139Z*
