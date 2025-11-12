const fs = require('fs');
const path = require('path');

const RUNTIME_DIR = 'benchmark-results/runtime';
const BUILD_DIR = 'benchmark-results/build';
const OUTPUT_DIR = 'docs/docs/benchmarks';
const STATIC_DIR = 'docs/static/benchmarks';

console.log('🚀 Processing benchmark results...\n');

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
  const tableStart = lines.findIndex(l => l.includes('| Method'));
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
        row[header] = values[idx];
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

function parseMeanValue(meanStr) {
  // Parse "352.5 ms" or "1,211.6 ms" -> 352.5 or 1211.6
  // Remove commas, then extract number
  const cleaned = meanStr.replace(/,/g, '');
  const match = cleaned.match(/[\d.]+/);
  return match ? parseFloat(match[0]) : 0;
}

// Process runtime benchmarks
const categories = {
  runtime: {},
  build: {}
};

let environmentInfo = {};

console.log('📊 Processing runtime benchmarks...');
const runtimeFiles = findMarkdownFiles(RUNTIME_DIR);
console.log(`  Found ${runtimeFiles.length} runtime benchmark files`);

runtimeFiles.forEach(file => {
  const content = fs.readFileSync(file, 'utf8');
  const data = parseMarkdownTable(content);

  if (!environmentInfo.os) {
    environmentInfo = extractEnvironmentInfo(content);
  }

  if (data) {
    // Extract test category from path
    const match = file.match(/run_time_([A-Za-z]+Tests)/);
    const category = match ? match[1] : path.basename(path.dirname(file));

    categories.runtime[category] = data;
    console.log(`  ✓ Processed ${category}: ${data.length} frameworks`);
  }
});

console.log('\n🔨 Processing build benchmarks...');
const buildFiles = findMarkdownFiles(BUILD_DIR);
console.log(`  Found ${buildFiles.length} build benchmark files`);

buildFiles.forEach(file => {
  const content = fs.readFileSync(file, 'utf8');
  const data = parseMarkdownTable(content);

  if (data) {
    categories.build['BuildTime'] = data;
    console.log(`  ✓ Processed build benchmarks: ${data.length} frameworks`);
  }
});

// Calculate statistics
const stats = {
  runtimeCategories: Object.keys(categories.runtime).length,
  buildCategories: Object.keys(categories.build).length,
  totalBenchmarks: runtimeFiles.length + buildFiles.length,
  lastUpdated: new Date().toISOString()
};

console.log('\n📊 Preparing benchmark data...');
console.log('\n📝 Generating documentation...');

const timestamp = new Date().toISOString().split('T')[0];
const sampleData = Object.values(categories.runtime)[0] || [];
const frameworks = {
  tunit: sampleData.find(d => d.Method === 'TUnit')?.Version || 'latest',
  xunit: sampleData.find(d => d.Method === 'xUnit3')?.Version || 'latest',
  nunit: sampleData.find(d => d.Method === 'NUnit')?.Version || 'latest',
  mstest: sampleData.find(d => d.Method === 'MSTest')?.Version || 'latest'
};

// Generate individual benchmark pages for each runtime category
Object.entries(categories.runtime).forEach(([testClass, data]) => {
  const benchmarkPage = `---
title: ${testClass}
description: Performance benchmark results for ${testClass}
sidebar_position: ${Object.keys(categories.runtime).indexOf(testClass) + 2}
---

# ${testClass} Benchmark

:::info Last Updated
This benchmark was automatically generated on **${timestamp}** from the latest CI run.

**Environment:** ${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}
:::

## 📊 Results

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
${data.map(row => {
  const name = row.Method.includes('TUnit_AOT') ? '**TUnit (AOT)**' : row.Method.includes('TUnit') ? '**TUnit**' : row.Method;
  return `| ${name} | ${row.Version || 'N/A'} | ${row.Mean} | ${row.Median || 'N/A'} | ${row.StdDev || 'N/A'} |`;
}).join('\n')}

## 📈 Visual Comparison

\`\`\`mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#ffffff',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'mainBkg': '#2563eb',
    'secondBkg': '#7c3aed',
    'tertiaryBkg': '#dc2626',
    'clusterBkg': '#f3f4f6',
    'edgeLabelBackground': '#ffffff',
    'tertiaryTextColor': '#1f2937',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1',
    'pie9': '#84cc16',
    'pie10': '#f97316',
    'pie11': '#14b8a6',
    'pie12': '#a855f7'
  }
}}%%
xychart-beta
  title "${testClass} Performance Comparison"
  x-axis [${data.map(d => `"${d.Method}"`).join(', ')}]
  y-axis "Time (${data[0]?.Mean.includes(' s') ? 's' : 'ms'})" 0 --> ${Math.ceil(Math.max(...data.map(d => parseMeanValue(d.Mean))) * 1.2)}
  bar [${data.map(d => parseMeanValue(d.Mean)).join(', ')}]
\`\`\`

## 🎯 Key Insights

${(() => {
  const tunitResult = data.find(d => d.Method === 'TUnit');
  const tunitAotResult = data.find(d => d.Method === 'TUnit_AOT');
  const otherResults = data.filter(d => !d.Method.includes('TUnit'));

  if (!tunitResult) return '- TUnit data not available';

  const tunitMean = parseMeanValue(tunitResult.Mean);
  const insights = [];

  otherResults.forEach(other => {
    const otherMean = parseMeanValue(other.Mean);
    const speedup = (otherMean / tunitMean).toFixed(2);
    if (speedup > 1) {
      insights.push(`- **${speedup}x faster** than ${other.Method} (${other.Version})`);
    }
  });

  if (tunitAotResult) {
    const aotMean = parseMeanValue(tunitAotResult.Mean);
    const aotSpeedup = (tunitMean / aotMean).toFixed(2);
    insights.push(`- **${aotSpeedup}x faster** with Native AOT compilation`);
  }

  return insights.join('\n');
})()}

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: ${new Date().toISOString()}*
`;

  fs.writeFileSync(path.join(OUTPUT_DIR, `${testClass}.md`), benchmarkPage);
  console.log(`  ✓ Created ${OUTPUT_DIR}/${testClass}.md`);

  // Generate individual JSON file for each benchmark
  const benchmarkJson = {
    timestamp: new Date().toISOString(),
    category: testClass,
    environment: environmentInfo,
    results: data
  };

  fs.writeFileSync(
    path.join(STATIC_DIR, `${testClass}.json`),
    JSON.stringify(benchmarkJson, null, 2)
  );
  console.log(`  ✓ Created ${STATIC_DIR}/${testClass}.json`);
});

// Generate build benchmark page if available
if (Object.keys(categories.build).length > 0) {
  Object.entries(categories.build).forEach(([testClass, data]) => {
    const benchmarkPage = `---
title: Build Performance
description: Compilation time benchmark results
sidebar_position: ${Object.keys(categories.runtime).length + 2}
---

# Build Performance Benchmark

:::info Last Updated
This benchmark was automatically generated on **${timestamp}** from the latest CI run.

**Environment:** ${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}
:::

## 📊 Results

Compilation time comparison across frameworks:

| Framework | Version | Mean | Median | StdDev |
|-----------|---------|------|--------|--------|
${data.map(row => {
  const name = row.Method.includes('TUnit') ? '**TUnit**' : row.Method;
  return `| ${name} | ${row.Version || 'N/A'} | ${row.Mean} | ${row.Median || 'N/A'} | ${row.StdDev || 'N/A'} |`;
}).join('\n')}

## 📈 Visual Comparison

\`\`\`mermaid
%%{init: {
  'theme':'base',
  'themeVariables': {
    'primaryColor': '#2563eb',
    'primaryTextColor': '#ffffff',
    'primaryBorderColor': '#1e40af',
    'lineColor': '#6b7280',
    'secondaryColor': '#7c3aed',
    'tertiaryColor': '#dc2626',
    'background': '#ffffff',
    'mainBkg': '#2563eb',
    'secondBkg': '#7c3aed',
    'tertiaryBkg': '#dc2626',
    'clusterBkg': '#f3f4f6',
    'edgeLabelBackground': '#ffffff',
    'tertiaryTextColor': '#1f2937',
    'pie1': '#2563eb',
    'pie2': '#7c3aed',
    'pie3': '#dc2626',
    'pie4': '#f59e0b',
    'pie5': '#10b981',
    'pie6': '#06b6d4',
    'pie7': '#ec4899',
    'pie8': '#6366f1',
    'pie9': '#84cc16',
    'pie10': '#f97316',
    'pie11': '#14b8a6',
    'pie12': '#a855f7'
  }
}}%%
xychart-beta
  title "Build Time Comparison"
  x-axis [${data.map(d => `"${d.Method}"`).join(', ')}]
  y-axis "Time (${data[0]?.Mean.includes(' s') ? 's' : 'ms'})" 0 --> ${Math.ceil(Math.max(...data.map(d => parseMeanValue(d.Mean))) * 1.2)}
  bar [${data.map(d => parseMeanValue(d.Mean)).join(', ')}]
\`\`\`

---

:::note Methodology
View the [benchmarks overview](/docs/benchmarks) for methodology details and environment information.
:::

*Last generated: ${new Date().toISOString()}*
`;

    fs.writeFileSync(path.join(OUTPUT_DIR, 'BuildTime.md'), benchmarkPage);
    console.log(`  ✓ Created ${OUTPUT_DIR}/BuildTime.md`);

    // Generate build benchmark JSON
    const buildJson = {
      timestamp: new Date().toISOString(),
      category: 'BuildTime',
      environment: environmentInfo,
      results: data
    };

    fs.writeFileSync(
      path.join(STATIC_DIR, 'BuildTime.json'),
      JSON.stringify(buildJson, null, 2)
    );
    console.log(`  ✓ Created ${STATIC_DIR}/BuildTime.json`);
  });
}

// Generate index/overview page
const indexPage = `---
title: Performance Benchmarks
description: Real-world performance comparisons between TUnit and other .NET testing frameworks
sidebar_position: 1
---

# Performance Benchmarks

:::info Last Updated
These benchmarks were automatically generated on **${timestamp}** from the latest CI run.

**Environment:** ${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}
:::

## 🚀 Runtime Benchmarks

Click on any benchmark to view detailed results:

${Object.keys(categories.runtime).map(testClass =>
  `- [${testClass}](${testClass}) - Detailed performance analysis`
).join('\n')}

${Object.keys(categories.build).length > 0 ? `
## 🔨 Build Benchmarks

- [Build Performance](BuildTime) - Compilation time comparison
` : ''}

---

## 📊 Methodology

These benchmarks compare TUnit against the most popular .NET testing frameworks:

| Framework | Version Tested |
|-----------|----------------|
| **TUnit** | ${frameworks.tunit} |
| **xUnit v3** | ${frameworks.xunit} |
| **NUnit** | ${frameworks.nunit} |
| **MSTest** | ${frameworks.mstest} |

### Test Scenarios

The benchmarks measure real-world testing patterns:

- **DataDrivenTests**: Parameterized tests with multiple data sources
- **AsyncTests**: Realistic async/await patterns with I/O simulation
- **ScaleTests**: Large test suites (150+ tests) measuring scalability
- **MatrixTests**: Combinatorial test generation and execution
- **MassiveParallelTests**: Parallel execution stress tests
- **SetupTeardownTests**: Expensive test fixtures with setup/teardown overhead

### Environment

- **OS**: ${environmentInfo.os || 'Ubuntu Latest (GitHub Actions)'}
- **Runtime**: ${environmentInfo.host || '.NET 10'}
- **SDK**: ${environmentInfo.sdk || '.NET 10 SDK'}
- **Hardware**: GitHub Actions Standard Runner (Ubuntu)
- **Tool**: ${environmentInfo.benchmarkDotNetVersion || 'BenchmarkDotNet'}

### Why These Numbers Matter

- **No Mocking**: All tests use realistic patterns, not artificial micro-benchmarks
- **Equivalent Logic**: Each framework implements identical test scenarios
- **Warm-Up Excluded**: Measurements exclude JIT warm-up overhead
- **Statistical Rigor**: Multiple iterations with outlier detection

### Source Code

All benchmark source code is available in the [\`tools/speed-comparison\`](https://github.com/thomhurst/TUnit/tree/main/tools/speed-comparison) directory.

---

:::note Continuous Benchmarking
These benchmarks run automatically daily via [GitHub Actions](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml).

Each benchmark runs multiple iterations with statistical analysis to ensure accuracy. Results may vary based on hardware and test characteristics.
:::

*Last generated: ${new Date().toISOString()}*
`;

fs.writeFileSync(path.join(OUTPUT_DIR, 'index.md'), indexPage);
console.log(`  ✓ Created ${OUTPUT_DIR}/index.md`);

// Generate JSON for interactive components
const benchmarkData = {
  timestamp: new Date().toISOString(),
  environment: environmentInfo,
  categories: categories.runtime,
  build: categories.build,
  stats
};

fs.writeFileSync(
  path.join(STATIC_DIR, 'latest.json'),
  JSON.stringify(benchmarkData, null, 2)
);
console.log(`  ✓ Created ${STATIC_DIR}/latest.json`);

// Update historical data
const historicalFile = path.join(STATIC_DIR, 'historical.json');
let historical = [];

if (fs.existsSync(historicalFile)) {
  try {
    historical = JSON.parse(fs.readFileSync(historicalFile, 'utf8'));
  } catch (e) {
    console.warn('  ⚠️  Could not parse historical data, creating new file');
    historical = [];
  }
}

// Add new data point
historical.push({
  date: new Date().toISOString().split('T')[0],
  environment: environmentInfo.os || 'Ubuntu'
});

// Keep last 90 days
historical = historical.slice(-90);

fs.writeFileSync(
  historicalFile,
  JSON.stringify(historical, null, 2)
);
console.log(`  ✓ Updated ${historicalFile} (${historical.length} data points)`);

// Generate benchmark summary for PR body
const benchmarkSummary = {
  runtime: Object.keys(categories.runtime),
  build: Object.keys(categories.build),
  timestamp: timestamp,
  environment: `${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}`
};

fs.writeFileSync(
  path.join(STATIC_DIR, 'summary.json'),
  JSON.stringify(benchmarkSummary, null, 2)
);
console.log(`  ✓ Created ${STATIC_DIR}/summary.json`);

console.log('\n✅ Benchmark processing complete!\n');
console.log(`Summary:`);
console.log(`  - Runtime categories: ${stats.runtimeCategories}`);
console.log(`  - Build categories: ${stats.buildCategories}`);
console.log(`  - Total benchmarks: ${stats.totalBenchmarks}`);
console.log(`  - Output files: 4 (markdown + 3 JSON files)`);
console.log(`\n📊 Benchmarks produced:`);
console.log(`\nRuntime Benchmarks:`);
Object.keys(categories.runtime).forEach(cat => console.log(`  - ${cat}`));
if (Object.keys(categories.build).length > 0) {
  console.log(`\nBuild Benchmarks:`);
  Object.keys(categories.build).forEach(cat => console.log(`  - ${cat}`));
}
