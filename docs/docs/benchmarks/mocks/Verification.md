---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-05** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 714.61 ns | 5.465 ns | 5.112 ns | 2864 B |
| Imposter | 664.96 ns | 3.310 ns | 2.764 ns | 4688 B |
| Mockolate | 402.97 ns | 1.441 ns | 1.348 ns | 2224 B |
| Moq | 236,931.26 ns | 1,505.857 ns | 1,334.903 ns | 24324 B |
| NSubstitute | 5,970.47 ns | 59.028 ns | 55.215 ns | 10064 B |
| FakeItEasy | 6,607.76 ns | 126.499 ns | 118.327 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 284318
  bar [714.61, 664.96, 402.97, 236931.26, 5970.47, 6607.76]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 54.55 ns | 0.669 ns | 0.593 ns | 304 B |
| Imposter | 358.76 ns | 6.906 ns | 7.092 ns | 2400 B |
| Mockolate | 264.08 ns | 2.023 ns | 1.892 ns | 1240 B |
| Moq | 64,508.90 ns | 412.242 ns | 365.442 ns | 7037 B |
| NSubstitute | 3,563.08 ns | 67.991 ns | 66.777 ns | 7088 B |
| FakeItEasy | 3,376.02 ns | 21.737 ns | 20.333 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 77411
  bar [54.55, 358.76, 264.08, 64508.9, 3563.08, 3376.02]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,166.26 ns | 12.320 ns | 11.524 ns | 4176 B |
| Imposter | 1,863.59 ns | 36.946 ns | 103.602 ns | 11192 B |
| Mockolate | 1,182.54 ns | 22.667 ns | 49.754 ns | 5408 B |
| Moq | 346,679.12 ns | 2,333.038 ns | 2,182.325 ns | 34699 B |
| NSubstitute | 10,394.53 ns | 65.161 ns | 60.952 ns | 16762 B |
| FakeItEasy | 11,620.16 ns | 115.668 ns | 108.196 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 416015
  bar [1166.26, 1863.59, 1182.54, 346679.12, 10394.53, 11620.16]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-05T03:26:21.616Z*
