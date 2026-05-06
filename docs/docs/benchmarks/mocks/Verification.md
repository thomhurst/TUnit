---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-06** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.203
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 692.20 ns | 12.586 ns | 11.773 ns | 2864 B |
| Imposter | 743.98 ns | 13.959 ns | 13.710 ns | 4688 B |
| Mockolate | 434.71 ns | 7.021 ns | 6.567 ns | 2224 B |
| Moq | 345,187.52 ns | 1,881.825 ns | 1,668.189 ns | 24325 B |
| NSubstitute | 6,509.70 ns | 114.123 ns | 106.751 ns | 10064 B |
| FakeItEasy | 7,584.79 ns | 42.067 ns | 39.350 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 414226
  bar [692.2, 743.98, 434.71, 345187.52, 6509.7, 7584.79]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.59 ns | 0.945 ns | 1.050 ns | 304 B |
| Imposter | 333.44 ns | 6.502 ns | 9.929 ns | 2400 B |
| Mockolate | 242.96 ns | 4.825 ns | 4.739 ns | 1240 B |
| Moq | 88,636.42 ns | 299.264 ns | 265.289 ns | 6918 B |
| NSubstitute | 3,716.60 ns | 42.887 ns | 40.116 ns | 7088 B |
| FakeItEasy | 3,666.47 ns | 72.521 ns | 77.596 ns | 5209 B |

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
  y-axis "Time (ns)" 0 --> 106364
  bar [52.59, 333.44, 242.96, 88636.42, 3716.6, 3666.47]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,233.32 ns | 16.516 ns | 15.449 ns | 4176 B |
| Imposter | 1,897.47 ns | 34.183 ns | 31.975 ns | 11192 B |
| Mockolate | 1,192.00 ns | 23.634 ns | 41.392 ns | 5408 B |
| Moq | 467,825.85 ns | 2,972.337 ns | 2,780.326 ns | 34699 B |
| NSubstitute | 11,477.50 ns | 69.517 ns | 58.050 ns | 16762 B |
| FakeItEasy | 13,761.56 ns | 216.979 ns | 192.346 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 561392
  bar [1233.32, 1897.47, 1192, 467825.85, 11477.5, 13761.56]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-06T03:25:44.139Z*
