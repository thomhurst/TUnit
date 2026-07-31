---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

> Verifying mock method calls — comparing **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries.

:::info Last Updated
This benchmark was automatically generated on **2026-07-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.302
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 698.42 ns | 13.663 ns | 17.280 ns | 3008 B |
| Imposter | 693.15 ns | 8.339 ns | 7.392 ns | 4688 B |
| Mockolate | 405.16 ns | 3.813 ns | 3.567 ns | 2128 B |
| Moq | 346,124.34 ns | 1,654.392 ns | 1,466.576 ns | 24325 B |
| NSubstitute | 6,185.63 ns | 37.195 ns | 32.972 ns | 10064 B |
| FakeItEasy | 7,413.16 ns | 33.781 ns | 31.599 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 415350
  bar [698.42, 693.15, 405.16, 346124.34, 6185.63, 7413.16]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 52.16 ns | 0.197 ns | 0.174 ns | 320 B |
| Imposter | 329.98 ns | 3.230 ns | 3.021 ns | 2400 B |
| Mockolate | 235.82 ns | 3.340 ns | 2.789 ns | 1144 B |
| Moq | 88,641.61 ns | 831.000 ns | 736.660 ns | 6918 B |
| NSubstitute | 3,748.95 ns | 45.185 ns | 40.055 ns | 7088 B |
| FakeItEasy | 3,666.96 ns | 35.365 ns | 31.351 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 106370
  bar [52.16, 329.98, 235.82, 88641.61, 3748.95, 3666.96]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,241.23 ns | 12.294 ns | 11.500 ns | 4472 B |
| Imposter | 1,789.06 ns | 32.735 ns | 27.335 ns | 11192 B |
| Mockolate | 1,130.26 ns | 6.496 ns | 5.758 ns | 5240 B |
| Moq | 485,273.95 ns | 3,149.422 ns | 2,945.971 ns | 34842 B |
| NSubstitute | 11,747.00 ns | 115.803 ns | 102.657 ns | 16763 B |
| FakeItEasy | 13,947.57 ns | 171.313 ns | 151.865 ns | 19233 B |

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
  y-axis "Time (ns)" 0 --> 582329
  bar [1241.23, 1789.06, 1130.26, 485273.95, 11747, 13947.57]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-07-31T03:21:39.823Z*
