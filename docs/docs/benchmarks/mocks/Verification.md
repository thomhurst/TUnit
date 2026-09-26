---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-26** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 690.90 ns | 13.011 ns | 12.171 ns | 3008 B |
| Imposter | 700.44 ns | 6.041 ns | 5.651 ns | 4688 B |
| Mockolate | 429.77 ns | 2.165 ns | 2.025 ns | 2128 B |
| Moq | 346,157.06 ns | 2,838.712 ns | 2,655.333 ns | 24325 B |
| NSubstitute | 7,211.08 ns | 77.829 ns | 64.991 ns | 10064 B |
| FakeItEasy | 7,376.66 ns | 43.471 ns | 38.536 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 415389
  bar [690.9, 700.44, 429.77, 346157.06, 7211.08, 7376.66]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 49.56 ns | 0.329 ns | 0.308 ns | 320 B |
| Imposter | 303.04 ns | 1.723 ns | 1.527 ns | 2400 B |
| Mockolate | 221.54 ns | 1.973 ns | 1.749 ns | 1144 B |
| Moq | 88,512.84 ns | 417.214 ns | 369.849 ns | 6918 B |
| NSubstitute | 3,776.71 ns | 60.900 ns | 53.986 ns | 7088 B |
| FakeItEasy | 3,512.67 ns | 15.398 ns | 13.650 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106216
  bar [49.56, 303.04, 221.54, 88512.84, 3776.71, 3512.67]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,209.84 ns | 6.435 ns | 5.704 ns | 4472 B |
| Imposter | 1,798.95 ns | 13.030 ns | 11.551 ns | 11192 B |
| Mockolate | 1,050.90 ns | 4.272 ns | 3.996 ns | 5240 B |
| Moq | 463,982.03 ns | 2,588.188 ns | 2,294.361 ns | 34699 B |
| NSubstitute | 12,207.20 ns | 55.883 ns | 49.539 ns | 16763 B |
| FakeItEasy | 13,114.13 ns | 109.002 ns | 96.628 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 556779
  bar [1209.84, 1798.95, 1050.9, 463982.03, 12207.2, 13114.13]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-26T02:32:00.095Z*
