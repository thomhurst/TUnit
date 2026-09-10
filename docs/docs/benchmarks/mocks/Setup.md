---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

> Mock behavior configuration (returns, matchers) — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-09** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 507.1 ns | 1.45 ns | 1.21 ns | 2.34 KB |
| Imposter | 786.7 ns | 3.85 ns | 3.41 ns | 6.12 KB |
| Mockolate | 298.4 ns | 1.65 ns | 1.54 ns | 1.41 KB |
| Moq | 433,702.9 ns | 1,551.69 ns | 1,451.45 ns | 28.67 KB |
| NSubstitute | 6,057.1 ns | 17.97 ns | 15.93 ns | 9.06 KB |
| FakeItEasy | 8,423.1 ns | 20.84 ns | 18.48 ns | 10.56 KB |

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
  y-axis "Time (ns)" 0 --> 520444
  bar [507.1, 786.7, 298.4, 433702.9, 6057.1, 8423.1]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 745.2 ns | 7.59 ns | 7.10 ns | 3.15 KB |
| Imposter | 1,320.4 ns | 11.31 ns | 10.58 ns | 10.59 KB |
| Mockolate | 523.3 ns | 1.78 ns | 1.49 ns | 2.35 KB |
| Moq | 115,726.9 ns | 1,245.23 ns | 1,164.79 ns | 16.53 KB |
| NSubstitute | 12,755.5 ns | 72.49 ns | 64.26 ns | 20.5 KB |
| FakeItEasy | 7,401.4 ns | 68.43 ns | 60.66 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 138873
  bar [745.2, 1320.4, 523.3, 115726.9, 12755.5, 7401.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-09T02:32:56.707Z*
