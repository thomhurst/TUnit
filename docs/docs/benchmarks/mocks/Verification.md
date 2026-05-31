---
title: "Mock Benchmark: Verification"
description: "Verifying mock method calls — TUnit.Mocks vs Imposter vs Mockolate vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: 7
---

# Verification Benchmark

:::info Last Updated
This benchmark was automatically generated on **2026-05-31** from the latest CI run.

**Environment:** Ubuntu Latest • .NET SDK 10.0.300
:::

## 📊 Results

Verifying mock method calls:

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 737.95 ns | 2.181 ns | 2.040 ns | 2968 B |
| Imposter | 702.46 ns | 10.109 ns | 9.456 ns | 4688 B |
| Mockolate | 406.58 ns | 3.081 ns | 2.882 ns | 2240 B |
| Moq | 242,472.66 ns | 1,666.833 ns | 1,477.604 ns | 24324 B |
| NSubstitute | 5,919.85 ns | 64.437 ns | 60.275 ns | 10064 B |
| FakeItEasy | 6,583.51 ns | 113.178 ns | 105.867 ns | 10722 B |

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
  y-axis "Time (ns)" 0 --> 290968
  bar [737.95, 702.46, 406.58, 242472.66, 5919.85, 6583.51]
```

---

### Never

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 51.21 ns | 0.203 ns | 0.190 ns | 304 B |
| Imposter | 319.20 ns | 1.210 ns | 1.073 ns | 2400 B |
| Mockolate | 241.86 ns | 0.990 ns | 0.926 ns | 1240 B |
| Moq | 62,247.47 ns | 350.676 ns | 310.865 ns | 6925 B |
| NSubstitute | 3,379.45 ns | 16.094 ns | 13.439 ns | 7088 B |
| FakeItEasy | 3,176.04 ns | 15.995 ns | 13.356 ns | 5210 B |

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
  y-axis "Time (ns)" 0 --> 74697
  bar [51.21, 319.2, 241.86, 62247.47, 3379.45, 3176.04]
```

---

### Multiple

| Library | Mean | Error | StdDev | Allocated |
|---------|------|-------|--------|-----------|
| **TUnit.Mocks** | 1,244.22 ns | 14.101 ns | 13.191 ns | 4384 B |
| Imposter | 1,692.92 ns | 15.157 ns | 14.178 ns | 11192 B |
| Mockolate | 1,095.87 ns | 5.853 ns | 5.189 ns | 5376 B |
| Moq | 353,257.44 ns | 4,332.795 ns | 4,052.899 ns | 34699 B |
| NSubstitute | 10,131.48 ns | 30.978 ns | 28.977 ns | 16762 B |
| FakeItEasy | 11,509.26 ns | 70.172 ns | 65.639 ns | 19232 B |

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
  y-axis "Time (ns)" 0 --> 423909
  bar [1244.22, 1692.92, 1095.87, 353257.44, 10131.48, 11509.26]
```

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for verifying mock method calls.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: 2026-05-31T03:32:45.264Z*
