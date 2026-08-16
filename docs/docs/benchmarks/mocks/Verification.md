---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-08-16** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.400
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 776.64 ns | 5.766 ns | 5.111 ns | 3008 B |
| Imposter | 695.26 ns | 3.525 ns | 3.125 ns | 4688 B |
| Mockolate | 416.12 ns | 4.052 ns | 3.791 ns | 2128 B |
| Moq | 242,450.20 ns | 1,309.541 ns | 1,224.946 ns | 24324 B |
| NSubstitute | 6,722.29 ns | 99.211 ns | 82.846 ns | 10064 B |
| FakeItEasy | 6,557.22 ns | 39.577 ns | 30.899 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 290941
  bar [776.64, 695.26, 416.12, 242450.2, 6722.29, 6557.22]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 59.05 ns | 0.719 ns | 0.672 ns | 320 B |
| Imposter | 359.41 ns | 4.485 ns | 4.196 ns | 2400 B |
| Mockolate | 243.52 ns | 1.363 ns | 1.275 ns | 1144 B |
| Moq | 61,637.22 ns | 414.111 ns | 387.360 ns | 6925 B |
| NSubstitute | 3,594.09 ns | 21.584 ns | 19.134 ns | 7088 B |
| FakeItEasy | 3,282.84 ns | 38.710 ns | 34.316 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 73965
  bar [59.05, 359.41, 243.52, 61637.22, 3594.09, 3282.84]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,257.50 ns | 13.089 ns | 12.243 ns | 4472 B |
| Imposter | 1,732.96 ns | 25.032 ns | 22.190 ns | 11192 B |
| Mockolate | 1,162.46 ns | 13.911 ns | 13.013 ns | 5240 B |
| Moq | 348,661.10 ns | 1,809.567 ns | 1,412.791 ns | 34922 B |
| NSubstitute | 11,374.18 ns | 53.175 ns | 47.138 ns | 16762 B |
| FakeItEasy | 11,764.40 ns | 152.262 ns | 142.426 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 418394
  bar [1257.5, 1732.96, 1162.46, 348661.1, 11374.18, 11764.4]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-08-16T02:49:35.790Z*
