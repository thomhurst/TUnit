import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import {Highlight, themes} from 'prism-react-renderer';
import {useCallback, useEffect, useRef, useState, type CSSProperties, type JSX, type PointerEvent} from 'react';

import styles from './index.module.css';

/* ---------- The test wall: every square is one test in a simulated parallel run. ---------- */

const cellSize = 8;
const cellPitch = 10;
const workerCount = 12;
const animationMs = 3200;
/* Durations below these thresholds get the lightest and middle shades of green. */
const quickTestMs = 4;
const steadyTestMs = 12;

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

type RunPlan = {
  width: number;
  cols: number;
  rows: number;
  offsetX: number;
  start: Float32Array;
  end: Float32Array;
  durationMs: Float32Array;
  testClass: Uint16Array;
  method: Uint16Array;
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
function planRun(width: number, rows: number): RunPlan {
  const cols = Math.max(1, Math.floor((width + cellPitch - cellSize) / cellPitch));
  const total = cols * rows;
  const offsetX = Math.floor((width - (cols * cellPitch - (cellPitch - cellSize))) / 2);
  const next = random(1729);

  const groups: number[][] = [];
  const testClass = new Uint16Array(total);
  const method = new Uint16Array(total);
  for (let i = 0; i < total;) {
    const size = Math.min(total - i, 3 + Math.floor(next() * 12));
    const group: number[] = [];
    for (let j = 0; j < size; j++) {
      testClass[i + j] = groups.length;
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
  return {width, cols, rows, offsetX, start, end, durationMs, testClass, method, makespanMs, sortedEnd};
}

function testName(run: RunPlan, index: number): string {
  return `${classNames[run.testClass[index] % classNames.length]}.${methodNames[run.method[index]]}`;
}

type Hover = {x: number; y: number; flip: boolean; name: string; status: string};
type Palette = {pending: string; running: string; quick: string; steady: string; slow: string};

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

function TestWall(): JSX.Element {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const passedRef = useRef<HTMLElement>(null);
  const timeRef = useRef<HTMLSpanElement>(null);
  const fillRef = useRef<HTMLDivElement>(null);
  const planRef = useRef<RunPlan | null>(null);
  const paletteRef = useRef<Palette | null>(null);
  const progressRef = useRef(1);
  const frame = useRef(0);
  const replayed = useRef(false);
  const [done, setDone] = useState(true);
  const [announcement, setAnnouncement] = useState('');
  const [hover, setHover] = useState<Hover | null>(null);

  const readPalette = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const css = getComputedStyle(canvas);
    const colour = (name: string) => css.getPropertyValue(name).trim();
    paletteRef.current = {
      pending: colour('--tunit-cell'),
      running: colour('--tunit-muted'),
      quick: colour('--tunit-pass-light'),
      steady: colour('--tunit-pass-mid'),
      slow: colour('--tunit-pass-fill'),
    };
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
    const ratio = window.devicePixelRatio || 1;
    context.setTransform(ratio, 0, 0, ratio, 0, 0);
    context.clearRect(0, 0, run.width, canvas.height);

    // Passed tests are shaded by duration, so slow tests stand out as darker squares.
    const buckets: number[][] = [[], [], [], [], []];
    for (let i = 0; i < run.start.length; i++) {
      const bucket = t < run.start[i] ? 0
        : t < run.end[i] ? 1
        : run.durationMs[i] < quickTestMs ? 2
        : run.durationMs[i] < steadyTestMs ? 3 : 4;
      buckets[bucket].push(i);
    }
    const colours = [palette.pending, palette.running, palette.quick, palette.steady, palette.slow];
    buckets.forEach((cells, bucket) => {
      context.fillStyle = colours[bucket];
      for (const i of cells) {
        context.fillRect(run.offsetX + (i % run.cols) * cellPitch, Math.floor(i / run.cols) * cellPitch, cellSize, cellSize);
      }
    });

    const passed = countPassed(run, t);
    if (passedRef.current) passedRef.current.textContent = passed.toLocaleString('en-US');
    if (timeRef.current) timeRef.current.textContent = `${(run.makespanMs * t / 1000).toFixed(2)}s`;
    if (fillRef.current) fillRef.current.style.transform = `scaleX(${passed / run.start.length})`;
  }, []);

  const layout = useCallback(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const width = canvas.getBoundingClientRect().width;
    const rows = width < 640 ? 16 : 14;
    const run = planRun(width, rows);
    planRef.current = run;
    const height = rows * cellPitch - (cellPitch - cellSize);
    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.round(width * ratio);
    canvas.height = Math.round(height * ratio);
    canvas.style.height = `${height}px`;
    draw();
  }, [draw]);

  const play = useCallback(() => {
    cancelAnimationFrame(frame.current);
    setHover(null);
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
  }, [draw]);

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

  const inspect = (event: PointerEvent<HTMLCanvasElement>) => {
    const run = planRef.current;
    if (!run) return;
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - bounds.left;
    const y = event.clientY - bounds.top;
    // Map from the displayed size back to the plan's coordinates in case the two differ.
    const scale = bounds.width > 0 ? run.width / bounds.width : 1;
    const col = Math.floor((x * scale - run.offsetX) / cellPitch);
    const row = Math.floor(y / cellPitch);
    if (col < 0 || col >= run.cols || row < 0 || row >= run.rows) {
      setHover(null);
      return;
    }
    const index = row * run.cols + col;
    const t = progressRef.current;
    const status = t >= run.end[index]
      ? `Passed in ${Math.max(1, Math.round(run.durationMs[index]))} ms`
      : t >= run.start[index] ? 'Running' : 'Waiting for a worker';
    setHover({x, y, flip: x > bounds.width / 2, name: testName(run, index), status});
  };

  return (
    <figure className={styles.wall}>
      <div className={styles.wallCanvas}>
        <canvas ref={canvasRef} onPointerMove={inspect} onPointerLeave={() => setHover(null)} aria-hidden="true" />
        {hover && (
          <div className={hover.flip ? `${styles.tooltip} ${styles.tooltipFlip}` : styles.tooltip}
            style={{left: hover.x, top: hover.y} as CSSProperties} aria-hidden="true">
            <span>{hover.name}</span>
            <span>{hover.status}</span>
          </div>
        )}
      </div>
      <div className={styles.stats} aria-hidden="true">
        <span><strong ref={passedRef}>0</strong> passed</span>
        <span>0 failed</span>
        <span ref={timeRef} className={styles.barTime}>0.00s</span>
      </div>
      <div className={styles.bar} aria-hidden="true">
        <div ref={fillRef} className={styles.barFill} />
      </div>
      <p className={styles.visuallyHidden} role="status">{announcement}</p>
      <figcaption className={styles.wallCaption}>
        <span>
          Each square is one test, run by one of {workerCount} workers. Darker squares took longer.
          Point at one to see its name.
        </span>
        <button type="button" onClick={replay} disabled={!done}>Run again</button>
      </figcaption>
    </figure>
  );
}

function Hero(): JSX.Element {
  return (
    <header className={styles.hero}>
      <div className={`${styles.container} ${styles.heroTop}`}>
        <h1 className={styles.heroTitle}>Keep it green.</h1>
        <div className={styles.heroAside}>
          <p>
            TUnit is a testing framework for modern .NET. Tests are discovered while you build
            and run in parallel by default, so you get to green sooner.
          </p>
          <div className={styles.actions}>
            <Link className={styles.primaryButton} to="/docs/getting-started/installation">Get started</Link>
            <Link className={styles.textLink} to="/docs/guides/philosophy">Why TUnit?</Link>
          </div>
        </div>
      </div>
      <div className={styles.container}><TestWall /></div>
    </header>
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
      <main className={styles.page}><Hero /><Claims /><Start /></main>
    </Layout>
  );
}
