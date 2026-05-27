---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 747.61 ns | 2.649 ns | 2.348 ns | 2968 B |
| Imposter | 692.33 ns | 8.358 ns | 7.818 ns | 4688 B |
| Mockolate | 422.96 ns | 5.485 ns | 5.130 ns | 2240 B |
| Moq | 248,130.32 ns | 602.273 ns | 563.367 ns | 24324 B |
| NSubstitute | 5,863.08 ns | 34.903 ns | 32.649 ns | 10176 B |
| FakeItEasy | 6,495.93 ns | 51.650 ns | 45.786 ns | 10722 B |

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
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 297757
  bar [747.61, 692.33, 422.96, 248130.32, 5863.08, 6495.93]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.82 ns | 0.155 ns | 0.137 ns | 304 B |
| Imposter | 323.52 ns | 1.534 ns | 1.360 ns | 2400 B |
| Mockolate | 249.31 ns | 1.617 ns | 1.512 ns | 1240 B |
| Moq | 61,962.71 ns | 566.032 ns | 501.772 ns | 6925 B |
| NSubstitute | 3,445.53 ns | 24.295 ns | 22.725 ns | 7088 B |
| FakeItEasy | 3,465.76 ns | 66.860 ns | 82.111 ns | 5290 B |

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
  title "Verification (Never) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 74356
  bar [51.82, 323.52, 249.31, 61962.71, 3445.53, 3465.76]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,248.69 ns | 4.943 ns | 4.382 ns | 4384 B |
| Imposter | 1,675.38 ns | 10.927 ns | 10.221 ns | 11192 B |
| Mockolate | 1,111.26 ns | 19.421 ns | 21.587 ns | 5376 B |
| Moq | 352,523.72 ns | 4,464.400 ns | 4,176.002 ns | 34699 B |
| NSubstitute | 10,333.48 ns | 115.001 ns | 107.572 ns | 16762 B |
| FakeItEasy | 11,714.57 ns | 89.042 ns | 74.354 ns | 19344 B |

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
  title "Verification (Multiple) Performance Comparison"
  x-axis ["TUnit.Mocks", "Imposter", "Mockolate", "Moq", "NSubstitute", "FakeItEasy"]
  y-axis "Time (ns)" 0 --> 423029
  bar [1248.69, 1675.38, 1111.26, 352523.72, 10333.48, 11714.57]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-27T03:29:35.677Z*
