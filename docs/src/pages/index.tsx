import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import {Highlight, themes} from 'prism-react-renderer';
import {useCallback, useEffect, useRef, useState, type CSSProperties, type JSX, type MouseEvent, type PointerEvent} from 'react';

import latestBenchmarks from '@site/static/benchmarks/latest.json';

import styles from './index.module.css';

/* ---------- The test wall: every square is one test in a simulated parallel run. ---------- */

const workerCount = 12;
const animationMs = 3200;
/* Durations below these thresholds get the lightest and middle shades of green. */
const quickTestMs = 4;
const steadyTestMs = 12;
/* Must match the page container in index.module.css so the headline lines up with the text below it. */
const containerMaxPx = 1180;

const classNames = [
  'OrderTests', 'CartTests', 'InvoiceTests', 'LoginTests', 'SearchTests', 'QueueTests', 'PaymentTests',
  'CalculatorTests', 'UserApiTests', 'SettingsTests', 'EmailTests', 'ReportTests', 'InventoryTests',
  'CheckoutTests', 'TokenTests', 'CacheTests', 'ShippingTests', 'AuditLogTests', 'PricingTests', 'WebhookTests',
];
const methodNames = [
  'Applies_discount', 'Rejects_empty_input', 'Returns_201_when_created', 'Retries_twice', 'Parses_iso_dates',
  'Rounds_to_two_places', 'Sends_confirmation', 'Expires_after_one_hour', 'Ranks_exact_matches_first',
  'Total_includes_tax', 'Handles_concurrent_writes', 'Survives_restart', 'Maps_to_dto', 'Logs_failed_attempts',
  'Renders_pdf', 'Caches_second_lookup', 'Requires_admin_role', 'Uses_utc', 'Streams_large_files', 'Is_idempotent',
];

/* The headline is set in a 5x7 dot-matrix face. Each dot is a block of tests on the wall. */
const glyphHeight = 7;
const glyphs: Record<string, string[]> = {
  K: ['10001', '10010', '10100', '11000', '10100', '10010', '10001'],
  E: ['11111', '10000', '10000', '11110', '10000', '10000', '11111'],
  P: ['11110', '10001', '10001', '11110', '10000', '10000', '10000'],
  I: ['11111', '00100', '00100', '00100', '00100', '00100', '11111'],
  T: ['11111', '00100', '00100', '00100', '00100', '00100', '00100'],
  G: ['01110', '10001', '10000', '10111', '10001', '10001', '01110'],
  R: ['11110', '10001', '10001', '11110', '10100', '10010', '10001'],
  N: ['10001', '11001', '10101', '10011', '10001', '10001', '10001'],
  '.': ['0', '0', '0', '0', '0', '0', '1'],
  ' ': ['000', '000', '000', '000', '000', '000', '000'],
};
const wideHeadline = ['KEEP IT', 'GREEN.'];
const narrowHeadline = ['KEEP', 'IT', 'GREEN.'];

function lineUnits(line: string): number {
  let units = line.length - 1;
  for (const char of line) units += glyphs[char][0].length;
  return units;
}

type RunPlan = {
  width: number;
  cols: number;
  rows: number;
  pitch: number;
  cell: number;
  start: Float32Array;
  end: Float32Array;
  durationMs: Float32Array;
  testClass: Uint16Array;
  method: Uint16Array;
  letter: Uint8Array;
  makespanMs: number;
  sortedEnd: Float32Array;
};

function random(seed: number): () => number {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/* Tests are grouped into classes that sit together on the wall; workers pick classes in shuffled order. */
function planRun(width: number, viewportHeight: number): RunPlan {
  const lines = width < 620 ? narrowHeadline : wideHeadline;
  const headlineUnits = Math.max(...lines.map(lineUnits));
  const unitsWide = headlineUnits + 2;
  const unitsHigh = lines.length * (glyphHeight + 1) + 1;
  // The headline fills the width unless that would push the rest of the hero off the first screen.
  const unitPx = Math.min(width / unitsWide, viewportHeight * 0.5 / unitsHigh);
  // At least two squares per dot, so the letters stay solid on a phone.
  const scale = Math.max(2, Math.round(unitPx / 9));
  const pitch = Math.min(14, Math.max(4, Math.floor(unitPx / scale)));
  const cell = pitch - (pitch >= 9 ? 2 : 1);
  const cols = Math.ceil(width / pitch);
  const rows = unitsHigh * scale;
  const total = cols * rows;

  const gutter = width <= 640 ? 32 : 64;
  const containerLeft = (width - Math.min(containerMaxPx, width - gutter)) / 2;
  const left = Math.max(0, Math.min(Math.round(containerLeft / pitch), cols - headlineUnits * scale));
  const letter = new Uint8Array(total);
  lines.forEach((line, lineIndex) => {
    const top = (1 + lineIndex * (glyphHeight + 1)) * scale;
    let x = left;
    for (const char of line) {
      const glyph = glyphs[char];
      glyph.forEach((bits, y) => {
        for (let bit = 0; bit < bits.length; bit++) {
          if (bits[bit] !== '1') continue;
          for (let dy = 0; dy < scale; dy++) {
            for (let dx = 0; dx < scale; dx++) {
              const col = x + bit * scale + dx;
              if (col < cols) letter[(top + y * scale + dy) * cols + col] = 1;
            }
          }
        }
      });
      x += (glyph[0].length + 1) * scale;
    }
  });

  const next = random(1729);
  const groups: number[][] = [];
  const testClass = new Uint16Array(total);
  const method = new Uint16Array(total);
  for (let i = 0; i < total;) {
    const size = Math.min(total - i, 3 + Math.floor(next() * 12));
    const group: number[] = [];
    for (let j = 0; j < size; j++) {
      testClass[i + j] = groups.length % classNames.length;
      method[i + j] = Math.floor(next() * methodNames.length);
      group.push(i + j);
    }
    groups.push(group);
    i += size;
  }
  for (let i = groups.length - 1; i > 0; i--) {
    const j = Math.floor(next() * (i + 1));
    [groups[i], groups[j]] = [groups[j], groups[i]];
  }

  const durationMs = new Float32Array(total);
  const start = new Float32Array(total);
  const end = new Float32Array(total);
  const workers = new Array<number>(workerCount).fill(0);
  for (const group of groups) {
    for (const index of group) {
      let worker = 0;
      for (let w = 1; w < workerCount; w++) if (workers[w] < workers[worker]) worker = w;
      const duration = 1 + Math.pow(next(), 3) * 40;
      durationMs[index] = duration;
      start[index] = workers[worker];
      workers[worker] += duration;
      end[index] = workers[worker];
    }
  }
  const makespanMs = Math.max(...workers);
  for (let i = 0; i < total; i++) {
    start[i] /= makespanMs;
    end[i] /= makespanMs;
  }
  const sortedEnd = end.slice().sort();
  return {width, cols, rows, pitch, cell, start, end, durationMs, testClass, method, letter, makespanMs, sortedEnd};
}

function testName(run: RunPlan, index: number): string {
  return `${classNames[run.testClass[index]]}.${methodNames[run.method[index]]}`;
}

type Hover = {x: number; y: number; flip: boolean; name: string; status: string};

/* Number of tests that finished by time t; sortedEnd is ascending. */
function countPassed(run: RunPlan, t: number): number {
  let low = 0;
  let high = run.sortedEnd.length;
  while (low < high) {
    const mid = (low + high) >>> 1;
    if (run.sortedEnd[mid] <= t) low = mid + 1;
    else high = mid;
  }
  return low;
}

/* Canvas colours, in bucket order: waiting, running, three passed shades, headline, failed, then three red shades. */
const paletteTokens = [
  '--tunit-cell', '--tunit-wall-running', '--tunit-wall-quick', '--tunit-wall-steady', '--tunit-wall-slow',
  '--tunit-wall-letter', '--tunit-wall-failed', '--tunit-wall-red-quick', '--tunit-wall-red-steady', '--tunit-wall-red-slow',
];

function TestWall({onFailuresChange}: {onFailuresChange: (count: number) => void}): JSX.Element {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const passedRef = useRef<HTMLElement>(null);
  const timeRef = useRef<HTMLSpanElement>(null);
  const planRef = useRef<RunPlan | null>(null);
  const paletteRef = useRef<string[] | null>(null);
  const failedRef = useRef(new Set<number>());
  const progressRef = useRef(1);
  const frame = useRef(0);
  const replayed = useRef(false);
  const [done, setDone] = useState(true);
  const [announcement, setAnnouncement] = useState('');
  const [hover, setHover] = useState<Hover | null>(null);
  const [failures, setFailures] = useState<{count: number; last: string}>({count: 0, last: ''});

  const readPalette = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const css = getComputedStyle(canvas);
    paletteRef.current = paletteTokens.map(token => css.getPropertyValue(token).trim());
  }, []);

  // The canvas and the readout are updated directly each frame so React stays out of the animation loop.
  const draw = useCallback(() => {
    const canvas = canvasRef.current;
    const run = planRef.current;
    const palette = paletteRef.current;
    if (!canvas || !run || !palette) return;
    const context = canvas.getContext('2d');
    if (!context) return;
    const t = progressRef.current;
    const failed = failedRef.current;
    const ratio = window.devicePixelRatio || 1;
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    context.clearRect(0, 0, run.cols * run.pitch, run.rows * run.pitch);

    // Passed tests are shaded by duration, so slow tests stand out as darker squares.
    // One failure turns the whole bar red, the way a test runner's progress bar does.
    const shadeOffset = failed.size > 0 ? 5 : 0;
    const buckets: number[][] = paletteTokens.map(() => []);
    for (let i = 0; i < run.start.length; i++) {
      const bucket = failed.has(i) ? 6
        : t < run.start[i] ? 0
        : t < run.end[i] ? 1
        : run.letter[i] ? 5
        : (run.durationMs[i] < quickTestMs ? 2 : run.durationMs[i] < steadyTestMs ? 3 : 4) + shadeOffset;
      buckets[bucket].push(i);
    }
    buckets.forEach((cells, bucket) => {
      context.fillStyle = palette[bucket];
      // Headline squares close their gaps so the letters read as solid shapes; a failed square is drawn larger.
      const grow = bucket === 6 ? 2 : 0;
      const size = bucket === 5 ? run.pitch : run.cell + grow * 2;
      for (const i of cells) {
        context.fillRect((i % run.cols) * run.pitch - grow, Math.floor(i / run.cols) * run.pitch - grow, size, size);
      }
    });

    const passed = countPassed(run, t) - failed.size;
    if (passedRef.current) passedRef.current.textContent = passed.toLocaleString('en-US');
    if (timeRef.current) timeRef.current.textContent = `${(run.makespanMs * t / 1000).toFixed(2)}s`;
  }, []);

  const clearFailures = useCallback(() => {
    if (failedRef.current.size === 0) return;
    failedRef.current.clear();
    setFailures({count: 0, last: ''});
    onFailuresChange(0);
  }, [onFailuresChange]);

  const layout = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const width = canvas.getBoundingClientRect().width;
    const run = planRun(width, window.innerHeight);
    planRef.current = run;
    // A new plan renumbers the tests, so earlier failures no longer point at the same squares.
    clearFailures();
    const height = run.rows * run.pitch - (run.pitch - run.cell);
    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.round(width * ratio);
    canvas.height = Math.round(height * ratio);
    canvas.style.height = `${height}px`;
    draw();
  }, [draw, clearFailures]);

  const play = useCallback(() => {
    cancelAnimationFrame(frame.current);
    setHover(null);
    clearFailures();
    const finish = () => {
      progressRef.current = 1;
      draw();
      setDone(true);
      if (replayed.current && planRef.current) {
        setAnnouncement(`Run finished: ${planRef.current.start.length.toLocaleString('en-US')} tests passed, 0 failed.`);
      }
    };
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      finish();
      return;
    }
    setDone(false);
    setAnnouncement('');
    const begin = performance.now();
    const step = (now: number) => {
      progressRef.current = Math.min((now - begin) / animationMs, 1);
      if (progressRef.current < 1) {
        draw();
        frame.current = requestAnimationFrame(step);
      } else {
        finish();
      }
    };
    progressRef.current = 0;
    draw();
    frame.current = requestAnimationFrame(step);
  }, [draw, clearFailures]);

  const replay = () => {
    replayed.current = true;
    play();
  };

  useEffect(() => {
    readPalette();
    layout();
    play();
    let width = canvasRef.current?.getBoundingClientRect().width;
    const resize = new ResizeObserver(() => {
      const next = canvasRef.current?.getBoundingClientRect().width;
      if (next === width) return;
      width = next;
      // A new width means a new plan, so a run in progress starts again on the new wall.
      const running = progressRef.current < 1;
      layout();
      if (running) play();
    });
    if (canvasRef.current) resize.observe(canvasRef.current);
    const theme = new MutationObserver(() => {
      readPalette();
      draw();
    });
    theme.observe(document.documentElement, {attributes: true, attributeFilter: ['data-theme']});
    return () => {
      cancelAnimationFrame(frame.current);
      resize.disconnect();
      theme.disconnect();
    };
  }, [readPalette, layout, play, draw]);

  const locate = (event: MouseEvent<HTMLCanvasElement>) => {
    const run = planRef.current;
    if (!run) return null;
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - bounds.left;
    const y = event.clientY - bounds.top;
    // Map from the displayed size back to the plan's coordinates in case the two differ.
    const scale = bounds.width > 0 ? run.width / bounds.width : 1;
    const col = Math.floor(x * scale / run.pitch);
    const row = Math.floor(y / run.pitch);
    if (col < 0 || col >= run.cols || row < 0 || row >= run.rows) return null;
    return {run, index: row * run.cols + col, x, y, flip: x > bounds.width / 2};
  };

  const inspect = (event: PointerEvent<HTMLCanvasElement>) => {
    const hit = locate(event);
    if (!hit) {
      setHover(null);
      return;
    }
    const {run, index} = hit;
    const t = progressRef.current;
    const status = failedRef.current.has(index) ? 'Failed: you clicked it'
      : t >= run.end[index] ? `Passed in ${Math.max(1, Math.round(run.durationMs[index]))} ms`
      : t >= run.start[index] ? 'Running' : 'Waiting for a worker';
    setHover({x: hit.x, y: hit.y, flip: hit.flip, name: testName(run, index), status});
  };

  const fail = (run: RunPlan, index: number) => {
    failedRef.current.add(index);
    const name = testName(run, index);
    const count = failedRef.current.size;
    setFailures({count, last: name});
    setAnnouncement(`${name} failed. ${count.toLocaleString('en-US')} failed in total.`);
    onFailuresChange(count);
    draw();
  };

  const failClicked = (event: MouseEvent<HTMLCanvasElement>) => {
    const hit = locate(event);
    if (!hit || progressRef.current < hit.run.end[hit.index] || failedRef.current.has(hit.index)) return;
    fail(hit.run, hit.index);
    setHover(null);
  };

  /* The keyboard route to the same joke: the canvas itself is not focusable. */
  const failRandom = () => {
    const run = planRef.current;
    if (!run) return;
    for (let attempt = 0; attempt < 50; attempt++) {
      const index = Math.floor(Math.random() * run.start.length);
      if (!failedRef.current.has(index)) {
        fail(run, index);
        return;
      }
    }
  };

  const broken = failures.count > 0;
  return (
    <figure className={styles.wall}>
      <div className={styles.wallCanvas}>
        <canvas ref={canvasRef} onPointerMove={inspect} onPointerLeave={() => setHover(null)} onClick={failClicked} aria-hidden="true" />
        {hover && (
          <div className={hover.flip ? `${styles.tooltip} ${styles.tooltipFlip}` : styles.tooltip}
            style={{left: hover.x, top: hover.y} as CSSProperties} aria-hidden="true">
            <span>{hover.name}</span>
            <span>{hover.status}</span>
          </div>
        )}
      </div>
      <div className={`${styles.container} ${styles.readout}`}>
        <div className={styles.stats} aria-hidden="true">
          <span><strong ref={passedRef}>0</strong> passed</span>
          <span className={broken ? styles.statFailed : undefined}><strong>{failures.count.toLocaleString('en-US')}</strong> failed</span>
          <span ref={timeRef} className={styles.statTime}>0.00s</span>
        </div>
        <div className={styles.wallActions}>
          <button type="button" onClick={failRandom} disabled={!done}>Fail a test</button>
          <button type="button" className={broken ? styles.fixButton : undefined} onClick={replay} disabled={!done}>
            {broken ? 'Fix and rerun' : 'Run again'}
          </button>
        </div>
        <p className={styles.visuallyHidden} role="status">{announcement}</p>
        <figcaption className={styles.wallCaption}>
          {broken ? (
            <span className={styles.failure}>
              <b>Failed</b> {failures.last}
              <span>Expected the bar to stay green, but someone clicked a test.</span>
            </span>
          ) : (
            <span>
              Each square is one test in a simulated run across {workerCount} workers. Darker squares took longer.
              Point at one to see its name, or click it to break the build.
            </span>
          )}
        </figcaption>
      </div>
    </figure>
  );
}

function Hero(): JSX.Element {
  const [failed, setFailed] = useState(0);
  return (
    <header className={styles.hero} data-failed={failed > 0 ? '' : undefined}>
      <h1 className={styles.visuallyHidden}>TUnit: keep it green.</h1>
      <TestWall onFailuresChange={setFailed} />
      <div className={`${styles.container} ${styles.heroBody}`}>
        <p className={styles.lede}>
          TUnit is a testing framework for modern .NET. Tests are discovered while you build
          and run in parallel by default, so you get to green sooner.
        </p>
        <div className={styles.actions}>
          <Link className={styles.primaryButton} to="/docs/getting-started/installation">Get started</Link>
          <Link className={styles.textLink} to="/docs/guides/philosophy">Why TUnit?</Link>
        </div>
      </div>
    </header>
  );
}

/* ---------- The race: measured run times, replayed in real time. ---------- */

type BenchmarkRow = {Method: string; Version: string; Mean: string};

const benchmarkCategories = latestBenchmarks.categories as Record<string, BenchmarkRow[]>;

const scenarios = [
  {key: 'AsyncTests', label: 'Async', description: 'Async tests with simulated I/O'},
  {key: 'DataDrivenTests', label: 'Data-driven', description: 'Parameterized tests fed from several data sources'},
  {key: 'MassiveParallelTests', label: 'Parallel', description: 'A parallel execution stress test'},
  {key: 'MatrixTests', label: 'Matrix', description: 'Combinatorial test generation and execution'},
  {key: 'ScaleTests', label: 'Scale', description: 'A large suite of 150+ tests'},
  {key: 'SetupTeardownTests', label: 'Fixtures', description: 'Expensive fixtures with setup and teardown'},
].filter(scenario => benchmarkCategories[scenario.key]);

const runtimes = [
  {label: 'JIT', lanes: [['TUnit', 'TUnit'], ['xUnit3', 'xUnit v3'], ['NUnit', 'NUnit'], ['MSTest', 'MSTest']]},
  {label: 'Native AOT', lanes: [['TUnit_AOT', 'TUnit'], ['xUnit3_AOT', 'xUnit v3']]},
];

/* Means are published as text such as "1,327.5 ms" or "9.520 s". */
function parseMs(mean: string): number {
  const value = parseFloat(mean.replace(/,/g, ''));
  return /\bs$/.test(mean.trim()) ? value * 1000 : value;
}

function formatMs(ms: number): string {
  if (ms >= 1000) return `${(ms / 1000).toFixed(2)} s`;
  return `${ms < 100 ? ms.toFixed(1) : Math.round(ms)} ms`;
}

const benchmarkDate = new Date(latestBenchmarks.timestamp)
  .toLocaleDateString('en-GB', {day: 'numeric', month: 'long', year: 'numeric', timeZone: 'UTC'});

function Race(): JSX.Element | null {
  const sectionRef = useRef<HTMLElement>(null);
  const [scenarioIndex, setScenarioIndex] = useState(0);
  const [runtimeIndex, setRuntimeIndex] = useState(0);
  // Bars are at full length until a race starts, so the chart is complete without JavaScript.
  const [racing, setRacing] = useState(true);
  const frame = useRef(0);

  const start = useCallback(() => {
    cancelAnimationFrame(frame.current);
    setRacing(false);
    // Two frames: one to paint the empty lanes, one to start the transitions from them.
    frame.current = requestAnimationFrame(() => {
      frame.current = requestAnimationFrame(() => setRacing(true));
    });
  }, []);

  useEffect(() => {
    const section = sectionRef.current;
    if (!section) return;
    const observer = new IntersectionObserver(entries => {
      if (!entries.some(entry => entry.isIntersecting)) return;
      observer.disconnect();
      start();
    }, {threshold: 0.35});
    observer.observe(section);
    return () => {
      observer.disconnect();
      cancelAnimationFrame(frame.current);
    };
  }, [start]);

  if (scenarios.length === 0) return null;
  const scenario = scenarios[scenarioIndex];
  const rows = benchmarkCategories[scenario.key];
  const lanes = runtimes[runtimeIndex].lanes.flatMap(([method, label]) => {
    const row = rows.find(candidate => candidate.Method === method);
    return row ? [{method, label, version: row.Version, ms: parseMs(row.Mean)}] : [];
  });
  const tunit = lanes[0];
  const rivals = lanes.slice(1);
  if (!tunit || rivals.length === 0) return null;
  const slowest = Math.max(...lanes.map(lane => lane.ms));
  const closest = rivals.reduce((best, lane) => lane.ms < best.ms ? lane : best);
  const ahead = closest.ms >= tunit.ms;
  const factor = ahead ? closest.ms / tunit.ms : tunit.ms / closest.ms;

  const choose = (setIndex: (index: number) => void, index: number) => {
    setIndex(index);
    start();
  };

  return (
    <section ref={sectionRef} className={styles.race} aria-labelledby="race-title">
      <div className={styles.container}>
        <h2 id="race-title" className={styles.sectionTitle}>Raced in real time.</h2>
        <p className={styles.sectionLead}>
          Every bar grows for exactly as long as that framework took to run the same tests in the latest benchmark run.
        </p>
        <div className={styles.raceControls}>
          <div className={styles.segments} role="group" aria-label="Scenario">
            {scenarios.map((option, index) => (
              <button key={option.key} type="button" aria-pressed={index === scenarioIndex} onClick={() => choose(setScenarioIndex, index)}>
                {option.label}
              </button>
            ))}
          </div>
          <div className={styles.segments} role="group" aria-label="Runtime">
            {runtimes.map((option, index) => (
              <button key={option.label} type="button" aria-pressed={index === runtimeIndex} onClick={() => choose(setRuntimeIndex, index)}>
                {option.label}
              </button>
            ))}
          </div>
        </div>
        <div className={styles.raceBody}>
          <div className={styles.raceStat}>
            <strong>{factor.toFixed(1)}×</strong>
            <span>{ahead ? 'faster than the next fastest' : 'slower than the fastest'}, {closest.label}</span>
            <p>{scenario.description}.</p>
          </div>
          <ol className={`${styles.lanes} ${racing ? '' : styles.lanesReset}`}>
            {lanes.map((lane, index) => (
              <li key={lane.method} className={index === 0 ? styles.laneLead : undefined}
                style={{'--share': `${lane.ms / slowest * 100}%`, '--ms': `${Math.round(lane.ms)}ms`} as CSSProperties}>
                <span className={styles.laneName}>{lane.label} <small>{lane.version}</small></span>
                <span className={styles.laneTrack}><span className={styles.laneBar} /></span>
                <span className={styles.laneValue}>
                  {formatMs(lane.ms)}
                  {index > 0 && <small>{(lane.ms / tunit.ms).toFixed(1)}× TUnit</small>}
                </span>
              </li>
            ))}
          </ol>
        </div>
        <div className={styles.raceFoot}>
          <span>Mean run time, {benchmarkDate}. {latestBenchmarks.environment.sdk} on GitHub Actions.</span>
          <button type="button" onClick={start}>Race again</button>
          <Link to="/docs/benchmarks">All benchmarks and methodology</Link>
        </div>
      </div>
    </section>
  );
}

/* ---------- Claims, written as tests. ---------- */

type Token = {types: string[]; content: string};
type TokenPropsGetter = (input: {token: Token}) => {style?: CSSProperties; className: string; children: string};

/* Underlines a line the way an editor marks a diagnostic, leaving its indentation alone. */
function FlaggedTokens({line, getTokenProps}: {line: Token[]; getTokenProps: TokenPropsGetter}): JSX.Element {
  const indent = line[0]?.content.match(/^\s*/)?.[0] ?? '';
  const tokens = line.map((token, index) => index === 0 ? {...token, content: token.content.slice(indent.length)} : token);
  return (
    <>
      {indent}
      <span className={styles.squiggle}>
        {tokens.map((token, index) => {
          const {style: _style, ...tokenProps} = getTokenProps({token});
          return <span key={index} {...tokenProps} />;
        })}
      </span>
    </>
  );
}

function Code({code, flaggedLine}: {code: string; flaggedLine?: number}): JSX.Element {
  return (
    <Highlight code={code} language="csharp" theme={themes.github}>
      {({tokens, getLineProps, getTokenProps}) => (
        <pre className={styles.code} tabIndex={0}>
          <code>
            {tokens.map((line, lineIndex) => {
              const {style: _lineStyle, ...lineProps} = getLineProps({line});
              return (
                <div key={lineIndex} {...lineProps}>
                  {lineIndex === flaggedLine ? <FlaggedTokens line={line} getTokenProps={getTokenProps} /> : line.map((token, tokenIndex) => {
                    const {style: _tokenStyle, ...tokenProps} = getTokenProps({token});
                    return <span key={tokenIndex} {...tokenProps} />;
                  })}
                </div>
              );
            })}
          </code>
        </pre>
      )}
    </Highlight>
  );
}

type Claim = {
  name: string;
  body: JSX.Element;
};

const claims: Claim[] = [
  {
    name: 'Discovers_tests_at_compile_time',
    body: (
      <>
        <div className={styles.claimText}>
          <p>A source generator writes the code that registers every test while your project builds. Nothing scans assemblies with reflection at startup, so the first test starts straight away, and test projects can publish with Native AOT.</p>
          <Link to="/docs/writing-tests/aot">Native AOT support</Link>
        </div>
        <Code code={`[Test]
public async Task Total_includes_tax()
{
    var cart = new Cart(new Item(100m));

    await Assert.That(cart.Total()).IsEqualTo(108m);
}`} />
      </>
    ),
  },
  {
    name: 'Runs_tests_in_parallel_by_default',
    body: (
      <>
        <div className={styles.claimText}>
          <p>Every test runs concurrently unless you say otherwise. When tests share something, say so with an attribute: keep them apart, limit how many run at once, or make one wait for another.</p>
          <Link to="/docs/execution/parallelism">Control parallel execution</Link>
        </div>
        <Code code={`[Test, NotInParallel("Database")]
public async Task Migrates_schema() { /* ... */ }

[Test, DependsOn(nameof(Migrates_schema))]
public async Task Seeds_reference_data() { /* ... */ }`} />
      </>
    ),
  },
  {
    name: 'Turns_one_method_into_many_cases',
    body: (
      <>
        <div className={styles.claimText}>
          <p>Feed a test from attributes, methods, classes, or a matrix of values. Each case is reported on its own, with its arguments in the name.</p>
          <Link to="/docs/writing-tests/data-driven-overview">Data-driven tests</Link>
        </div>
        <Code code={`[Test]
[Arguments(1, 2, 3)]
[Arguments(5, 8, 13)]
[Arguments(-1, 1, 0)]
public async Task Add(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}`} />
      </>
    ),
  },
  {
    name: 'Flags_assertions_you_forgot_to_await',
    body: (
      <>
        <div className={styles.claimText}>
          <p>Assertions are awaited and read left to right. Analyzers check them while you type, along with hook signatures and data sources, so mistakes show up in the editor instead of in CI.</p>
          <Link to="/docs/assertions/awaiting">Awaiting assertions</Link>
        </div>
        <figure className={styles.diagnostic}>
          <Code flaggedLine={4} code={`[Test]
public async Task Greeting_contains_name()
{
    var greeting = Greet("Ada");
    Assert.That(greeting).Contains("Ada");
}`} />
          <figcaption>
            <span className={styles.diagnosticId}>TUnitAssertions0002</span>
            Assert statements must be awaited - all TUnit assertions return Task
          </figcaption>
        </figure>
      </>
    ),
  },
  {
    name: 'Tests_whole_systems_not_just_units',
    body: (
      <>
        <div className={styles.claimText}>
          <p>The same framework drives integration tests, with shared infrastructure that starts once and is cleaned up after the last test using it finishes.</p>
          <Link to="/docs/benchmarks">See the benchmarks</Link>
        </div>
        <ul className={styles.integrations}>
          <li><Link to="/docs/examples/aspnet">ASP.NET Core</Link><span>Endpoints with isolated clients</span></li>
          <li><Link to="/docs/examples/aspire">Aspire</Link><span>Your whole distributed app</span></li>
          <li><Link to="/docs/examples/playwright">Playwright</Link><span>A clean browser page per test</span></li>
          <li><Link to="/docs/writing-tests/mocking">TUnit.Mocks</Link><span>Source-generated, AOT-safe mocks</span></li>
        </ul>
      </>
    ),
  },
];

function TestName({name}: {name: string}): JSX.Element {
  const parts = name.split('_');
  return <>{parts.map((part, index) => <span key={index}>{part}{index < parts.length - 1 && <>_<wbr /></>}</span>)}</>;
}

function Claims(): JSX.Element {
  return (
    <section className={styles.claims} aria-labelledby="claims-title">
      <div className={styles.container}>
        <h2 id="claims-title" className={styles.sectionTitle}>Our claims, written as tests.</h2>
        <p className={styles.sectionLead}>Open any result to see the code behind it.</p>
        <div className={styles.claimList}>
          {claims.map((claim, index) => (
            <details key={claim.name} className={styles.claim} open={index === 0}>
              <summary>
                <span className={styles.passCell} aria-hidden="true" />
                <span className={styles.claimName}><TestName name={claim.name} /></span>
                <span className={styles.claimStatus}>Passed</span>
              </summary>
              <div className={styles.claimBody}>{claim.body}</div>
            </details>
          ))}
        </div>
      </div>
    </section>
  );
}

/* ---------- Getting started. ---------- */

const installSteps = [
  {label: 'Install the template', command: 'dotnet new install TUnit.Templates'},
  {label: 'Create a project', command: 'dotnet new TUnit -n MyTests'},
  {label: 'Run the tests', command: 'dotnet run --project MyTests'},
];

function Start(): JSX.Element {
  const [copyStatus, setCopyStatus] = useState<{index: number; ok: boolean} | null>(null);

  async function copyCommand(command: string, index: number) {
    try {
      await navigator.clipboard.writeText(command);
      setCopyStatus({index, ok: true});
    } catch {
      setCopyStatus({index, ok: false});
    }
  }

  return (
    <section className={styles.start} aria-labelledby="start-title">
      <div className={`${styles.container} ${styles.startGrid}`}>
        <div>
          <h2 id="start-title" className={styles.sectionTitle}>Turn your first bar green.</h2>
          <ol className={styles.terminal}>
            {installSteps.map((step, index) => (
              <li key={step.label}>
                <span className={styles.prompt} aria-hidden="true">&gt;</span>
                <code aria-label={step.label}>{step.command}</code>
                <button type="button" onClick={() => copyCommand(step.command, index)} aria-label={`Copy command: ${step.label}`}>
                  {copyStatus?.index === index && copyStatus.ok ? 'Copied' : 'Copy'}
                </button>
              </li>
            ))}
          </ol>
          <p className={styles.copyStatus} role="status">
            {copyStatus ? (copyStatus.ok ? `${installSteps[copyStatus.index].label}: command copied.` : 'Couldn’t copy automatically. Select the command and copy it.') : ' '}
          </p>
          <Link className={styles.primaryButton} to="/docs/getting-started/writing-your-first-test">Write your first test</Link>
        </div>
        <div className={styles.migration}>
          <h3>Moving from xUnit, NUnit, or MSTest?</h3>
          <p>The migration guides map each attribute, assertion, and hook you use today to its TUnit equivalent.</p>
          <Link className={styles.textLink} to="/docs/comparison/framework-differences">Compare frameworks</Link>
        </div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  return (
    <Layout title="Keep it green" description="TUnit is a source-generated testing framework for modern .NET. Tests are discovered at compile time, run in parallel by default, and use awaitable, fluent assertions.">
      <main className={styles.page}><Hero /><Race /><Claims /><Start /></main>
    </Layout>
  );
}
