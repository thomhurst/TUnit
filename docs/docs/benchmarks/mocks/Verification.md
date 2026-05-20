---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-20** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 651.56 ns | 5.041 ns | 4.468 ns | 2864 B |
| Imposter | 695.01 ns | 13.250 ns | 11.746 ns | 4688 B |
| Mockolate | 417.46 ns | 8.388 ns | 7.847 ns | 2240 B |
| Moq | 344,775.19 ns | 2,227.798 ns | 2,083.884 ns | 24325 B |
| NSubstitute | 6,517.01 ns | 128.982 ns | 138.009 ns | 10064 B |
| FakeItEasy | 7,638.69 ns | 32.642 ns | 28.936 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 413731
  bar [651.56, 695.01, 417.46, 344775.19, 6517.01, 7638.69]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 47.69 ns | 0.314 ns | 0.293 ns | 304 B |
| Imposter | 319.25 ns | 3.440 ns | 2.872 ns | 2400 B |
| Mockolate | 237.90 ns | 3.017 ns | 2.822 ns | 1240 B |
| Moq | 88,660.85 ns | 216.365 ns | 180.675 ns | 6918 B |
| NSubstitute | 3,605.08 ns | 22.140 ns | 19.627 ns | 7088 B |
| FakeItEasy | 3,623.08 ns | 44.591 ns | 37.235 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106394
  bar [47.69, 319.25, 237.9, 88660.85, 3605.08, 3623.08]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,160.38 ns | 18.685 ns | 17.478 ns | 4176 B |
| Imposter | 1,848.52 ns | 35.240 ns | 32.964 ns | 11192 B |
| Mockolate | 1,146.67 ns | 22.835 ns | 21.360 ns | 5376 B |
| Moq | 470,144.93 ns | 1,264.265 ns | 1,120.738 ns | 34842 B |
| NSubstitute | 11,362.98 ns | 39.163 ns | 32.703 ns | 16762 B |
| FakeItEasy | 14,000.05 ns | 167.343 ns | 156.533 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 564174
  bar [1160.38, 1848.52, 1146.67, 470144.93, 11362.98, 14000.05]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-20T03:28:07.578Z*
