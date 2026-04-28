---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-28** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 740.42 ns | 8.547 ns | 7.995 ns | 2864 B |
| Imposter | 692.96 ns | 7.522 ns | 7.036 ns | 4688 B |
| Mockolate | 983.15 ns | 4.275 ns | 3.999 ns | 3152 B |
| Moq | 245,548.22 ns | 1,638.712 ns | 1,452.676 ns | 24324 B |
| NSubstitute | 6,129.86 ns | 27.462 ns | 25.688 ns | 10064 B |
| FakeItEasy | 6,924.68 ns | 52.570 ns | 46.602 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 294658
  bar [740.42, 692.96, 983.15, 245548.22, 6129.86, 6924.68]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 55.79 ns | 0.283 ns | 0.265 ns | 304 B |
| Imposter | 329.91 ns | 1.666 ns | 1.301 ns | 2400 B |
| Mockolate | 239.08 ns | 0.953 ns | 0.891 ns | 952 B |
| Moq | 62,794.71 ns | 411.932 ns | 365.167 ns | 6925 B |
| NSubstitute | 3,482.62 ns | 32.934 ns | 29.195 ns | 7088 B |
| FakeItEasy | 3,427.83 ns | 41.443 ns | 38.766 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 75354
  bar [55.79, 329.91, 239.08, 62794.71, 3482.62, 3427.83]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,248.17 ns | 11.122 ns | 9.860 ns | 4176 B |
| Imposter | 1,755.70 ns | 22.255 ns | 19.729 ns | 11192 B |
| Mockolate | 1,857.64 ns | 11.761 ns | 11.001 ns | 5496 B |
| Moq | 347,856.40 ns | 2,861.187 ns | 2,536.368 ns | 34699 B |
| NSubstitute | 10,614.17 ns | 70.824 ns | 62.783 ns | 16762 B |
| FakeItEasy | 11,868.98 ns | 190.277 ns | 168.676 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 417428
  bar [1248.17, 1755.7, 1857.64, 347856.4, 10614.17, 11868.98]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-28T03:25:54.642Z*
