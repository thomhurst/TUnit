---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-18** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.202
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 526.2 ns | 1.52 ns | 1.27 ns | 2.34 KB |
| Imposter | 774.6 ns | 5.12 ns | 4.79 ns | 6.12 KB |
| Mockolate | 436.4 ns | 2.77 ns | 2.46 ns | 2.03 KB |
| Moq | 414,031.5 ns | 1,378.42 ns | 1,221.93 ns | 28.64 KB |
| NSubstitute | 5,342.1 ns | 25.23 ns | 22.37 ns | 9.01 KB |
| FakeItEasy | 7,782.8 ns | 39.57 ns | 35.07 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 496838
  bar [526.2, 774.6, 436.4, 414031.5, 5342.1, 7782.8]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 698.3 ns | 13.77 ns | 21.84 ns | 2.93 KB |
| Imposter | 1,335.4 ns | 7.55 ns | 7.07 ns | 10.59 KB |
| Mockolate | 703.4 ns | 3.15 ns | 2.95 ns | 3.07 KB |
| Moq | 112,046.5 ns | 161.40 ns | 134.78 ns | 16.53 KB |
| NSubstitute | 11,281.6 ns | 80.32 ns | 75.13 ns | 20.31 KB |
| FakeItEasy | 7,747.7 ns | 35.32 ns | 31.31 ns | 11.79 KB |

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
  y-axis "Time (ns)" 0 --> 134456
  bar [698.3, 1335.4, 703.4, 112046.5, 11281.6, 7747.7]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-18T03:21:40.293Z*
