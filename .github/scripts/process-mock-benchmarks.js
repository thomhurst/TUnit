const fs = require('fs');
const path = require('path');

const RESULTS_DIR = 'mock-benchmark-results';
const OUTPUT_DIR = 'docs/docs/benchmarks/mocks';
const STATIC_DIR = 'docs/static/benchmarks/mocks';

console.log('🚀 Processing mock benchmark results...\n');

// Ensure output directories exist
[OUTPUT_DIR, STATIC_DIR].forEach(dir => {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
});

function findMarkdownFiles(dir) {
  const files = [];

  function walk(currentPath) {
    if (!fs.existsSync(currentPath)) return;

    const entries = fs.readdirSync(currentPath, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(currentPath, entry.name);
      if (entry.isDirectory()) {
        walk(fullPath);
      } else if (entry.name.endsWith('.md')) {
        files.push(fullPath);
      }
    }
  }

  walk(dir);
  return files;
}

function parseMarkdownTable(content) {
  const lines = content.split('\n');
  const tableStart = lines.findIndex(l => l.includes('| Method') || l.includes('| Description'));
  if (tableStart === -1) return null;

  const headers = lines[tableStart].split('|').map(h => h.trim()).filter(Boolean);
  const data = [];

  for (let i = tableStart + 2; i < lines.length; i++) {
    const line = lines[i].trim();
    if (!line.startsWith('|')) break;

    const values = line.split('|').map(v => v.trim()).filter(Boolean);
    if (values.length === headers.length) {
      const row = {};
      headers.forEach((header, idx) => {
        row[header] = decodeHtmlEntities(values[idx]);
      });
      data.push(row);
    }
  }

  return data;
}

function extractEnvironmentInfo(content) {
  const lines = content.split('\n');
  const envStart = lines.findIndex(l => l.includes('BenchmarkDotNet'));
  if (envStart === -1) return {};

  const info = {};
  for (let i = envStart; i < Math.min(envStart + 10, lines.length); i++) {
    const line = lines[i];
    if (line.includes('BenchmarkDotNet')) info.benchmarkDotNetVersion = line.trim();
    if (line.includes('OS:')) info.os = line.split(':')[1]?.trim();
    if (line.includes('.NET SDK')) info.sdk = line.trim();
    if (line.includes('Host')) info.host = line.split(':')[1]?.trim();
  }

  return info;
}

function decodeHtmlEntities(str) {
  if (!str || !str.includes('&')) return str;
  return str
    .replace(/&#39;/g, "'")
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, '&');
}

function parseMeanValue(meanStr) {
  if (!meanStr) return 0;
  const cleaned = meanStr.replace(/,/g, '');
  const match = cleaned.match(/[\d.]+/);
  return match ? parseFloat(match[0]) : 0;
}

function getUnit(meanStr) {
  if (!meanStr) return 'ns';
  if (meanStr.includes(' s') && !meanStr.includes('ms') && !meanStr.includes('ns') && !meanStr.includes('μs')) return 's';
  if (meanStr.includes('ms')) return 'ms';
  if (meanStr.includes('μs') || meanStr.includes('us')) return 'μs';
  return 'ns';
}

// Map from benchmark class names to friendly category names
const categoryMap = {
  'MockCreationBenchmarks': 'MockCreation',
  'SetupBenchmarks': 'Setup',
  'InvocationBenchmarks': 'Invocation',
  'VerificationBenchmarks': 'Verification',
  'CallbackBenchmarks': 'Callback',
  'CombinedWorkflowBenchmarks': 'CombinedWorkflow'
};

const categoryDescriptions = {
  'MockCreation': 'Mock instance creation performance',
  'Setup': 'Mock behavior configuration (returns, matchers)',
  'Invocation': 'Calling methods on mock objects',
  'Verification': 'Verifying mock method calls',
  'Callback': 'Callback registration and execution',
  'CombinedWorkflow': 'Full workflow: create → setup → invoke → verify'
};

// Process results
const categories = {};
let environmentInfo = {};

console.log('📊 Processing mock benchmark results...');
const allFiles = findMarkdownFiles(RESULTS_DIR);
console.log(`  Found ${allFiles.length} markdown files`);

if (allFiles.length > 0) {
  console.log('  Sample paths:');
  allFiles.slice(0, 3).forEach(f => console.log(`    ${f}`));
}

allFiles.forEach(file => {
  const content = fs.readFileSync(file, 'utf8');
  const data = parseMarkdownTable(content);

  if (!environmentInfo.os) {
    environmentInfo = extractEnvironmentInfo(content);
  }

  if (data && data.length > 0) {
    // Extract category from artifact directory path
    // Path structure: mock-benchmark-results/mock_benchmark_<BenchmarkClass>/.../*.md
    let category = null;
    for (const [className, catName] of Object.entries(categoryMap)) {
      if (file.includes(className) || file.includes(`mock_benchmark_${className}`)) {
        category = catName;
        break;
      }
    }

    if (category) {
      // Merge results if we already have some for this category
      if (categories[category]) {
        categories[category] = categories[category].concat(data);
      } else {
        categories[category] = data;
      }
      console.log(`  ✓ Processed ${category}: ${data.length} entries`);
    } else {
      console.warn(`  ⚠️  Could not extract category from file path: ${file}`);
    }
  }
});

const timestamp = new Date().toISOString().split('T')[0];

// Generate individual benchmark pages for each category
console.log('\n📝 Generating documentation...');

Object.entries(categories).forEach(([category, data], index) => {
  const description = categoryDescriptions[category] || category;
  const unit = getUnit(data[0]?.Mean);
  const maxMean = Math.max(...data.map(d => parseMeanValue(d.Mean)));

  const benchmarkPage = `---
title: "Mock Benchmark: ${category}"
description: "${description} — TUnit.Mocks vs Moq vs NSubstitute vs FakeItEasy"
sidebar_position: ${index + 2}
---

# ${category} Benchmark

:::info Last Updated
This benchmark was automatically generated on **${timestamp}** from the latest CI run.

**Environment:** ${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}
:::

## 📊 Results

${description}:

| Method | Mean | Error | StdDev | Allocated |
|--------|------|-------|--------|-----------|
${data.map(row => {
  const name = (row.Description || row.Method || '').includes('TUnit') ? `**${row.Description || row.Method}**` : (row.Description || row.Method);
  return `| ${name} | ${row.Mean || 'N/A'} | ${row.Error || 'N/A'} | ${row.StdDev || 'N/A'} | ${row.Allocated || 'N/A'} |`;
}).join('\n')}

## 📈 Visual Comparison

\`\`\`mermaid
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
  title "${category} Performance Comparison"
  x-axis [${data.map(d => `"${(d.Description || d.Method || '').replace(/"/g, "'")}"` ).join(', ')}]
  y-axis "Time (${unit})" 0 --> ${Math.ceil(maxMean * 1.2) || 100}
  bar [${data.map(d => parseMeanValue(d.Mean)).join(', ')}]
\`\`\`

## 🎯 Key Insights

This benchmark compares **TUnit.Mocks** (source-generated) against runtime proxy-based mocking libraries for ${description.toLowerCase()}.

---

:::note Methodology
View the [mock benchmarks overview](/docs/benchmarks/mocks) for methodology details and environment information.
:::

*Last generated: ${new Date().toISOString()}*
`;

  fs.writeFileSync(path.join(OUTPUT_DIR, `${category}.md`), benchmarkPage);
  console.log(`  ✓ Created ${OUTPUT_DIR}/${category}.md`);

  // Generate individual JSON file
  const benchmarkJson = {
    timestamp: new Date().toISOString(),
    category,
    description,
    environment: environmentInfo,
    results: data
  };

  fs.writeFileSync(
    path.join(STATIC_DIR, `${category}.json`),
    JSON.stringify(benchmarkJson, null, 2)
  );
  console.log(`  ✓ Created ${STATIC_DIR}/${category}.json`);
});

// Generate index/overview page
const indexPage = `---
title: Mock Library Benchmarks
description: Performance comparisons between TUnit.Mocks, Moq, NSubstitute, and FakeItEasy
sidebar_position: 1
---

# Mock Library Benchmarks

:::info Last Updated
These benchmarks were automatically generated on **${timestamp}** from the latest CI run.

**Environment:** ${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}
:::

## 🚀 Overview

These benchmarks compare **TUnit.Mocks** (source-generated, AOT-compatible) against the most popular .NET mocking libraries that use runtime proxy generation:

| Library | Approach | AOT Compatible |
|---------|----------|----------------|
| **TUnit.Mocks** | Source-generated at compile time | ✅ Yes |
| **Moq** | Runtime proxy via Castle.DynamicProxy | ❌ No |
| **NSubstitute** | Runtime proxy via Castle.DynamicProxy | ❌ No |
| **FakeItEasy** | Runtime proxy via Castle.DynamicProxy | ❌ No |

## 📊 Benchmark Categories

Click on any benchmark to view detailed results:

${Object.keys(categories).map(category =>
  `- [${category}](${category}) - ${categoryDescriptions[category] || category}`
).join('\n')}

## 📈 What's Measured

Each benchmark category tests a specific aspect of mocking library usage:

- **MockCreation** — How fast can each library create a mock instance?
- **Setup** — How fast can you configure return values and argument matchers?
- **Invocation** — Once set up, how fast are method calls on the mock?
- **Verification** — How fast can you verify that methods were called correctly?
- **Callback** — How fast are callbacks triggered during mock invocations?
- **CombinedWorkflow** — The full real-world pattern: create → setup → invoke → verify

## 🔧 Methodology

- **Tool**: ${environmentInfo.benchmarkDotNetVersion || 'BenchmarkDotNet'}
- **OS**: ${environmentInfo.os || 'Ubuntu Latest (GitHub Actions)'}
- **Runtime**: ${environmentInfo.host || '.NET 10'}
- **Statistical Rigor**: Multiple iterations with warm-up and outlier detection
- **Memory**: Allocation tracking enabled via \`[MemoryDiagnoser]\`

### Why Source-Generated Mocks?

TUnit.Mocks generates mock implementations at compile time, eliminating:
- Runtime proxy generation overhead
- Dynamic assembly emission
- Reflection-based method dispatch

This makes TUnit.Mocks compatible with **Native AOT** and **IL trimming**, while also providing performance benefits for standard .NET execution.

### Source Code

All benchmark source code is available in the [\`TUnit.Mocks.Benchmarks\`](https://github.com/thomhurst/TUnit/tree/main/TUnit.Mocks.Benchmarks) directory.

---

:::note Continuous Benchmarking
These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/mock-benchmarks.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy. Results may vary based on hardware and test characteristics.
:::

*Last generated: ${new Date().toISOString()}*
`;

fs.writeFileSync(path.join(OUTPUT_DIR, 'index.md'), indexPage);
console.log(`  ✓ Created ${OUTPUT_DIR}/index.md`);

// Generate latest.json
const benchmarkData = {
  timestamp: new Date().toISOString(),
  environment: environmentInfo,
  categories,
  stats: {
    categoryCount: Object.keys(categories).length,
    totalBenchmarks: Object.values(categories).reduce((sum, arr) => sum + arr.length, 0),
    lastUpdated: new Date().toISOString()
  }
};

fs.writeFileSync(
  path.join(STATIC_DIR, 'latest.json'),
  JSON.stringify(benchmarkData, null, 2)
);
console.log(`  ✓ Created ${STATIC_DIR}/latest.json`);

// Generate summary.json
const summary = {
  categories: Object.keys(categories),
  timestamp,
  environment: `${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}`,
  libraries: ['TUnit.Mocks', 'Moq', 'NSubstitute', 'FakeItEasy']
};

fs.writeFileSync(
  path.join(STATIC_DIR, 'summary.json'),
  JSON.stringify(summary, null, 2)
);
console.log(`  ✓ Created ${STATIC_DIR}/summary.json`);

// Summary
console.log('\n✅ Mock benchmark processing complete!\n');
console.log(`Summary:`);
console.log(`  - Categories: ${Object.keys(categories).length}`);
console.log(`  - Total entries: ${Object.values(categories).reduce((sum, arr) => sum + arr.length, 0)}`);
console.log(`  - Markdown pages generated: ${Object.keys(categories).length + 1}`);
console.log(`  - JSON files generated: ${Object.keys(categories).length + 2}`);

console.log(`\n📊 Mock benchmarks produced:`);
Object.keys(categories).forEach(cat => console.log(`  - ${cat} (${categories[cat].length} entries)`));

if (Object.keys(categories).length === 0) {
  console.warn('\n⚠️  WARNING: No mock benchmark categories were found!');
  console.warn('This likely means the artifact directory structure is not as expected.');
  console.warn(`Expected structure: ${RESULTS_DIR}/mock_benchmark_<BenchmarkClass>/`);
}
