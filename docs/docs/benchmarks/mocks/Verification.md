---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-04-27** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 645.97 ns | 7.027 ns | 5.486 ns | 2864 B |
| Imposter | 699.73 ns | 13.369 ns | 11.851 ns | 4688 B |
| Mockolate | 899.04 ns | 5.535 ns | 5.177 ns | 3152 B |
| Moq | 341,240.49 ns | 1,984.367 ns | 1,856.178 ns | 24644 B |
| NSubstitute | 6,167.33 ns | 25.269 ns | 22.400 ns | 10064 B |
| FakeItEasy | 7,174.88 ns | 40.768 ns | 38.134 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 409489
  bar [645.97, 699.73, 899.04, 341240.49, 6167.33, 7174.88]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 48.76 ns | 0.520 ns | 0.434 ns | 304 B |
| Imposter | 316.16 ns | 2.564 ns | 2.398 ns | 2400 B |
| Mockolate | 220.04 ns | 4.454 ns | 4.574 ns | 952 B |
| Moq | 86,274.10 ns | 460.461 ns | 430.715 ns | 6918 B |
| NSubstitute | 3,689.52 ns | 71.868 ns | 70.584 ns | 7088 B |
| FakeItEasy | 3,691.45 ns | 28.284 ns | 26.457 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 103529
  bar [48.76, 316.16, 220.04, 86274.1, 3689.52, 3691.45]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,203.29 ns | 23.453 ns | 23.034 ns | 4176 B |
| Imposter | 1,803.81 ns | 35.867 ns | 78.729 ns | 11192 B |
| Mockolate | 1,847.85 ns | 19.823 ns | 16.553 ns | 5496 B |
| Moq | 473,017.88 ns | 3,688.268 ns | 3,450.008 ns | 35034 B |
| NSubstitute | 11,482.70 ns | 98.573 ns | 92.206 ns | 16763 B |
| FakeItEasy | 13,534.29 ns | 216.014 ns | 191.491 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 567622
  bar [1203.29, 1803.81, 1847.85, 473017.88, 11482.7, 13534.29]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-04-27T03:25:25.011Z*
