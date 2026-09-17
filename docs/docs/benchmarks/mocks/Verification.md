---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-09-17** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.401
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 692.03 ns | 4.649 ns | 4.349 ns | 3008 B |
| Imposter | 643.19 ns | 4.247 ns | 3.973 ns | 4688 B |
| Mockolate | 396.32 ns | 3.505 ns | 2.927 ns | 2128 B |
| Moq | 351,259.55 ns | 1,998.611 ns | 1,771.717 ns | 24325 B |
| NSubstitute | 7,110.47 ns | 103.951 ns | 97.235 ns | 10064 B |
| FakeItEasy | 7,306.01 ns | 41.422 ns | 36.719 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 421512
  bar [692.03, 643.19, 396.32, 351259.55, 7110.47, 7306.01]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 49.92 ns | 0.369 ns | 0.345 ns | 320 B |
| Imposter | 312.81 ns | 2.431 ns | 2.274 ns | 2400 B |
| Mockolate | 223.05 ns | 2.474 ns | 2.066 ns | 1144 B |
| Moq | 89,157.37 ns | 604.490 ns | 535.865 ns | 6918 B |
| NSubstitute | 3,779.39 ns | 46.288 ns | 43.298 ns | 7088 B |
| FakeItEasy | 3,427.83 ns | 36.753 ns | 30.691 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106989
  bar [49.92, 312.81, 223.05, 89157.37, 3779.39, 3427.83]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,249.41 ns | 3.615 ns | 3.204 ns | 4472 B |
| Imposter | 1,652.11 ns | 4.490 ns | 4.200 ns | 11192 B |
| Mockolate | 1,054.37 ns | 3.069 ns | 2.721 ns | 5240 B |
| Moq | 474,054.16 ns | 1,184.335 ns | 924.651 ns | 34699 B |
| NSubstitute | 11,975.96 ns | 43.025 ns | 40.245 ns | 16762 B |
| FakeItEasy | 13,382.54 ns | 144.003 ns | 127.655 ns | 19345 B |

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
  y-axis "Time (ns)" 0 --> 568865
  bar [1249.41, 1652.11, 1054.37, 474054.16, 11975.96, 13382.54]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-09-17T02:33:27.459Z*
