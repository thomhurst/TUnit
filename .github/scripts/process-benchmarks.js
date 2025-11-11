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

// Generate main benchmarks page
console.log('\n📝 Generating documentation...');

const timestamp = new Date().toISOString().split('T')[0];

let mainPage = `---
title: Performance Benchmarks
description: Real-world performance comparisons between TUnit and other .NET testing frameworks
sidebar_position: 1
---

# Performance Benchmarks

:::info Last Updated
These benchmarks were automatically generated on **${timestamp}** from the latest CI run.

**Environment:** ${environmentInfo.os || 'Ubuntu Latest'} • ${environmentInfo.sdk || '.NET 10'}
:::

## 🚀 Runtime Performance

`;

// Add runtime results
Object.entries(categories.runtime).forEach(([testClass, data]) => {
  mainPage += `\n### ${testClass}\n\n`;

  // Add table
  mainPage += `| Framework | Version | Mean | Median | StdDev |\n`;
  mainPage += `|-----------|---------|------|--------|--------|\n`;

  data.forEach(row => {
    const name = row.Method.includes('TUnit_AOT') ? '**TUnit (AOT)**' : row.Method.includes('TUnit') ? '**TUnit**' : row.Method;
    mainPage += `| ${name} | ${row.Version || 'N/A'} | ${row.Mean} | ${row.Median || 'N/A'} | ${row.StdDev || 'N/A'} |\n`;
  });

  mainPage += '\n';

  // Add Mermaid chart
  mainPage += `\`\`\`mermaid\n`;
  mainPage += `%%{init: {'theme':'base'}}%%\n`;
  mainPage += `xychart-beta\n`;
  mainPage += `  title "${testClass} Performance Comparison"\n`;

  // Detect time unit from the data
  const sampleMean = data[0]?.Mean || '';
  const timeUnit = sampleMean.includes(' s') ? 's' : 'ms';

  // Find max value for y-axis scaling
  const maxValue = Math.max(...data.map(d => parseMeanValue(d.Mean)));
  const yMax = Math.ceil(maxValue * 1.2); // 20% padding

  mainPage += `  x-axis [${data.map(d => `"${d.Method}"`).join(', ')}]\n`;
  mainPage += `  y-axis "Time (${timeUnit})" 0 --> ${yMax}\n`;
  mainPage += `  bar [${data.map(d => parseMeanValue(d.Mean)).join(', ')}]\n`;
  mainPage += `\`\`\`\n\n`;
});

// Add build time results
if (Object.keys(categories.build).length > 0) {
  mainPage += `\n---\n\n## 🔨 Build Performance\n\n`;
  mainPage += `Compilation time comparison across frameworks:\n\n`;

  Object.entries(categories.build).forEach(([testClass, data]) => {
    mainPage += `| Framework | Version | Mean | Median | StdDev |\n`;
    mainPage += `|-----------|---------|------|--------|--------|\n`;

    data.forEach(row => {
      const name = row.Method.includes('TUnit') ? '**TUnit**' : row.Method;
      mainPage += `| ${name} | ${row.Version || 'N/A'} | ${row.Mean} | ${row.Median || 'N/A'} | ${row.StdDev || 'N/A'} |\n`;
    });

    mainPage += '\n';

    // Add Mermaid chart for build performance
    mainPage += `\`\`\`mermaid\n`;
    mainPage += `%%{init: {'theme':'base'}}%%\n`;
    mainPage += `xychart-beta\n`;
    mainPage += `  title "Build Time Comparison"\n`;

    // Detect time unit from the data
    const sampleMean = data[0]?.Mean || '';
    const timeUnit = sampleMean.includes(' s') ? 's' : 'ms';

    // Find max value for y-axis scaling
    const maxValue = Math.max(...data.map(d => parseMeanValue(d.Mean)));
    const yMax = Math.ceil(maxValue * 1.2); // 20% padding

    mainPage += `  x-axis [${data.map(d => `"${d.Method}"`).join(', ')}]\n`;
    mainPage += `  y-axis "Time (${timeUnit})" 0 --> ${yMax}\n`;
    mainPage += `  bar [${data.map(d => parseMeanValue(d.Mean)).join(', ')}]\n`;
    mainPage += `\`\`\`\n\n`;
  });
}

// Add methodology section
const sampleData = Object.values(categories.runtime)[0] || [];
const frameworks = {
  tunit: sampleData.find(d => d.Method === 'TUnit')?.Version || 'latest',
  xunit: sampleData.find(d => d.Method === 'xUnit3')?.Version || 'latest',
  nunit: sampleData.find(d => d.Method === 'NUnit')?.Version || 'latest',
  mstest: sampleData.find(d => d.Method === 'MSTest')?.Version || 'latest'
};

mainPage += `
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

fs.writeFileSync(path.join(OUTPUT_DIR, 'index.md'), mainPage);
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
