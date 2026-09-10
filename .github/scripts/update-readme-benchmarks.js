const fs = require('fs');

const LATEST_JSON = 'docs/static/benchmarks/latest.json';
const README = 'README.md';
const START_MARKER = '<!-- benchmarks:start -->';
const END_MARKER = '<!-- benchmarks:end -->';

const SCENARIOS = {
  DataDrivenTests: 'Data-driven tests',
  AsyncTests: 'Async-heavy tests',
  MatrixTests: 'Matrix combinations',
  ScaleTests: 'Large suites (scale)',
  MassiveParallelTests: 'Massive parallelism',
  SetupTeardownTests: 'Setup/teardown lifecycle',
};

const COLUMNS = ['TUnit_AOT', 'TUnit', 'xUnit3', 'NUnit', 'MSTest'];
const COLUMN_LABELS = { TUnit_AOT: 'TUnit (AOT)', TUnit: 'TUnit', xUnit3: 'xUnit v3', NUnit: 'NUnit', MSTest: 'MSTest' };

console.log('📝 Updating README benchmark section...');

const latest = JSON.parse(fs.readFileSync(LATEST_JSON, 'utf8'));

function findResult(category, method) {
  return (latest.categories[category] || []).find(r => r.Method === method);
}

const rows = Object.entries(SCENARIOS)
  .filter(([key]) => latest.categories[key])
  .map(([key, label]) => {
    const cells = COLUMNS.map(col => {
      const mean = findResult(key, col)?.Mean;
      return mean && mean !== 'NA' ? mean : '—';
    });
    return `| ${label} | ${cells.join(' | ')} |`;
  });

if (rows.length === 0) {
  console.error('❌ No runtime benchmark categories found in latest.json');
  process.exit(1);
}

const versions = COLUMNS.map(col => {
  for (const category of Object.keys(latest.categories)) {
    const result = findResult(category, col);
    if (result?.Version) return `${COLUMN_LABELS[col]} ${result.Version}`;
  }
  return null;
}).filter(Boolean);

const date = (latest.timestamp || '').slice(0, 10);
const environment = [latest.environment?.sdk, latest.environment?.host].filter(Boolean).join(', ');

const section = [
  `| Scenario | ${COLUMNS.map(c => COLUMN_LABELS[c]).join(' | ')} |`,
  `|----------|${COLUMNS.map(() => '---').join('|')}|`,
  ...rows,
  '',
  `<sub>Mean wall-clock time to run the same test suite. ${versions.join(' · ')}. ${environment}. Updated ${date} — regenerated weekly by the [Speed Comparison workflow](https://github.com/thomhurst/TUnit/actions/workflows/speed-comparison.yml). Full results and methodology: [tunit.dev/docs/benchmarks](https://tunit.dev/docs/benchmarks/).</sub>`,
].join('\n');

const readme = fs.readFileSync(README, 'utf8');
const startIndex = readme.indexOf(START_MARKER);
const endIndex = readme.indexOf(END_MARKER);

if (startIndex === -1 || endIndex === -1 || endIndex < startIndex) {
  console.error(`❌ Could not find ${START_MARKER} / ${END_MARKER} markers in README.md`);
  process.exit(1);
}

const updated =
  readme.slice(0, startIndex + START_MARKER.length) +
  '\n' + section + '\n' +
  readme.slice(endIndex);

if (updated === readme) {
  console.log('✅ README benchmark section already up to date');
} else {
  fs.writeFileSync(README, updated);
  console.log(`✅ README benchmark section updated (${rows.length} scenarios, data from ${date})`);
}
