---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-03-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.201
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 747.01 ns | 4.703 ns | 4.400 ns | 3080 B |
| Imposter | 670.44 ns | 5.770 ns | 5.115 ns | 4688 B |
| Mockolate | 904.99 ns | 3.528 ns | 3.300 ns | 3104 B |
| Moq | 337,659.45 ns | 2,363.046 ns | 2,094.779 ns | 24325 B |
| NSubstitute | 6,271.24 ns | 33.911 ns | 28.318 ns | 10064 B |
| FakeItEasy | 7,057.80 ns | 28.124 ns | 26.308 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 405192
  bar [747.01, 670.44, 904.99, 337659.45, 6271.24, 7057.8]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 77.40 ns | 0.343 ns | 0.321 ns | 384 B |
| Imposter | 314.22 ns | 3.486 ns | 3.261 ns | 2400 B |
| Mockolate | 205.99 ns | 0.353 ns | 0.330 ns | 904 B |
| Moq | 86,766.43 ns | 631.587 ns | 590.787 ns | 6918 B |
| NSubstitute | 3,506.37 ns | 20.693 ns | 19.357 ns | 7088 B |
| FakeItEasy | 3,644.70 ns | 24.479 ns | 22.898 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 104120
  bar [77.4, 314.22, 205.99, 86766.43, 3506.37, 3644.7]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,309.49 ns | 4.164 ns | 3.692 ns | 4544 B |
| Imposter | 1,709.63 ns | 11.859 ns | 10.513 ns | 11192 B |
| Mockolate | 1,805.02 ns | 11.725 ns | 10.967 ns | 5400 B |
| Moq | 459,332.59 ns | 890.134 ns | 743.302 ns | 34699 B |
| NSubstitute | 11,011.90 ns | 54.151 ns | 50.653 ns | 16762 B |
| FakeItEasy | 12,832.90 ns | 123.675 ns | 103.274 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 551200
  bar [1309.49, 1709.63, 1805.02, 459332.59, 11011.9, 12832.9]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-03-31T03:22:46.140Z*
