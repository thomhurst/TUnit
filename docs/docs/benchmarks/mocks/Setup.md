---
title: "Mock Benchmark: Setup"
description: "Mock behavior configuration (returns, matchers) — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 6
---

# Setup Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Mock behavior configuration (returns, matchers):

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 542.7 ns | 5.43 ns | 5.08 ns | 1.99 KB |
| Imposter | 808.1 ns | 13.65 ns | 12.10 ns | 6.12 KB |
| Mockolate | 438.0 ns | 2.52 ns | 2.23 ns | 2.01 KB |
| Moq | 422,632.5 ns | 1,809.53 ns | 1,692.63 ns | 28.52 KB |
| NSubstitute | 5,494.1 ns | 57.29 ns | 53.59 ns | 9.01 KB |
| FakeItEasy | 8,080.6 ns | 47.84 ns | 44.75 ns | 10.45 KB |

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
  y-axis "Time (ns)" 0 --> 507159
  bar [542.7, 808.1, 438, 422632.5, 5494.1, 8080.6]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 786.5 ns | 12.16 ns | 10.78 ns | 2.75 KB |
| Imposter | 1,330.7 ns | 16.48 ns | 13.76 ns | 10.59 KB |
| Mockolate | 712.5 ns | 11.10 ns | 10.38 ns | 3.05 KB |
| Moq | 113,644.6 ns | 718.76 ns | 672.33 ns | 16.53 KB |
| NSubstitute | 11,953.0 ns | 79.84 ns | 70.78 ns | 20.31 KB |
| FakeItEasy | 7,393.0 ns | 96.47 ns | 85.52 ns | 11.71 KB |

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
  y-axis "Time (ns)" 0 --> 136374
  bar [786.5, 1330.7, 712.5, 113644.6, 11953, 7393]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for mock behavior configuration (returns, matchers).

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-31T03:22:46.140Z*
