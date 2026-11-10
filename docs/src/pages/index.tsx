import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import {Highlight, themes} from 'prism-react-renderer';
import {useCallback, useEffect, useRef, useState, type CSSProperties, type JSX, type PointerEvent, type ReactNode} from 'react';

import latestBenchmarks from '@site/static/benchmarks/latest.json';

import styles from './index.module.css';

/* The page follows one test through a working day. Each chapter owns one of the five note colours. */
const chapters = [
  {id: 'type', label: 'As you type'},
  {id: 'build', label: 'As you build'},
  {id: 'run', label: 'As it runs'},
  {id: 'measured', label: 'Measured'},
  {id: 'grow', label: 'As it grows'},
];

function noteColour(index: number): CSSProperties {
  return {'--note': `var(--tunit-note-${index + 1})`} as CSSProperties;
}

function Hero(): JSX.Element {
  return (
    <header className={styles.hero}>
      <div className={`${styles.container} ${styles.heroTop}`}>
        <h1 className={styles.heroTitle}>Know sooner.</h1>
        <div className={styles.heroAside}>
          <p>
            TUnit is a testing framework for modern .NET. It checks your tests as you type, finds them
            as you build, and runs them the way you describe.
          </p>
          <div className={styles.actions}>
            <Link className={styles.primaryButton} to="/docs/getting-started/installation">Get started</Link>
            <Link className={styles.textLink} to="/docs/guides/philosophy">Why TUnit?</Link>
          </div>
        </div>
      </div>
      <nav className={`${styles.container} ${styles.timeline}`} aria-label="On this page">
        {chapters.map((chapter, index) => (
          <a key={chapter.id} href={`#${chapter.id}`} style={noteColour(index)}>
            <span aria-hidden="true" />
            {chapter.label}
          </a>
        ))}
      </nav>
    </header>
  );
}

function ChapterHead({index, title, children}: {index: number; title: string; children: ReactNode}): JSX.Element {
  const chapter = chapters[index];
  return (
    <div className={styles.chapterHead}>
      <p className={styles.chapterLabel} style={noteColour(index)}><span aria-hidden="true" />{chapter.label}</p>
      <h2 id={`${chapter.id}-title`} className={styles.sectionTitle}>{title}</h2>
      {children}
    </div>
  );
}

/* ---------- Code samples. ---------- */

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

/* ---------- As you type: an analyzer diagnostic, and the one-word fix. ---------- */

const greetingTest = (awaited: boolean) => `[Test]
public async Task Greeting_contains_name()
{
    var greeting = Greet("Ada");
    ${awaited ? 'await ' : ''}Assert.That(greeting).Contains("Ada");
}`;

function TypeChapter(): JSX.Element {
  const [awaited, setAwaited] = useState(false);
  return (
    <section id="type" className={styles.chapter} aria-labelledby="type-title">
      <div className={`${styles.container} ${styles.split}`}>
        <ChapterHead index={0} title="Mistakes show up in the editor.">
          <p>
            Analyzers read your tests while you write them. A forgotten await, a hook with the wrong
            signature or a data source that doesn't fit its test is flagged in the editor, before anything runs.
          </p>
          <Link to="/docs/assertions/awaiting">Awaiting assertions</Link>
        </ChapterHead>
        <figure className={styles.editor}>
          <Code flaggedLine={awaited ? undefined : 4} code={greetingTest(awaited)} />
          <figcaption className={awaited ? styles.editorClean : undefined} role="status">
            {awaited ? (
              <span>No issues found.</span>
            ) : (
              <span>
                <b className={styles.diagnosticId}>TUnitAssertions0002</b>{' '}
                Assert statements must be awaited - all TUnit assertions return Task
              </span>
            )}
            <button type="button" onClick={() => setAwaited(!awaited)}>{awaited ? 'Take it out again' : 'Add await'}</button>
          </figcaption>
        </figure>
      </div>
    </section>
  );
}

/* ---------- As you build: where the work of finding tests happens. ---------- */

type WorkStep = {label: string; share: number; early?: boolean; late?: boolean};

const workOrders: {name: string; steps: WorkStep[]}[] = [
  {
    name: 'Found at startup',
    steps: [
      {label: 'Build', share: 30},
      {label: 'Start', share: 11},
      {label: 'Scan assemblies for tests', share: 27, late: true},
      {label: 'Run', share: 32},
    ],
  },
  {
    name: 'TUnit',
    steps: [
      {label: 'Build, and write down every test', share: 38, early: true},
      {label: 'Start', share: 11},
      {label: 'Run', share: 34},
    ],
  },
];

function BuildChapter(): JSX.Element {
  return (
    <section id="build" className={`${styles.chapter} ${styles.chapterTint}`} aria-labelledby="build-title">
      <div className={`${styles.container} ${styles.split}`}>
        <ChapterHead index={1} title="Your tests are found by the compiler.">
          <p>
            A source generator writes the code that registers every test while your project builds. Nothing scans
            assemblies with reflection at startup, so the first test starts straight away, and test projects can
            publish with Native AOT.
          </p>
          <Link to="/docs/writing-tests/aot">Native AOT support</Link>
        </ChapterHead>
        <figure className={styles.orders}>
          {workOrders.map(order => (
            <div key={order.name} className={styles.order}>
              <p>{order.name}</p>
              <ol>
                {order.steps.map(step => (
                  <li key={step.label} style={{'--share': step.share} as CSSProperties}
                    className={step.early ? styles.stepEarly : step.late ? styles.stepLate : undefined}>
                    {step.label}
                  </li>
                ))}
              </ol>
            </div>
          ))}
          <figcaption>The order of work, not drawn to scale.</figcaption>
        </figure>
      </div>
    </section>
  );
}

/* ---------- As it runs: a simulated schedule that three attributes reshape. ---------- */

const workerCount = 8;

const classNames = [
  'OrderTests', 'CartTests', 'InvoiceTests', 'LoginTests', 'SearchTests', 'QueueTests', 'PaymentTests',
  'CalculatorTests', 'SettingsTests', 'EmailTests', 'ReportTests', 'PricingTests',
];
const methodNames = [
  'Applies_discount', 'Rejects_empty_input', 'Returns_201_when_created', 'Retries_twice', 'Parses_iso_dates',
  'Rounds_to_two_places', 'Sends_confirmation', 'Expires_after_one_hour', 'Ranks_exact_matches_first',
  'Total_includes_tax', 'Handles_concurrent_writes', 'Survives_restart', 'Maps_to_dto', 'Logs_failed_attempts',
];

type Group = 'plain' | 'database' | 'chain' | 'api';
type SimTest = {name: string; ms: number; group: Group; dependsOn?: number};
type Rules = {exclusive: boolean; ordered: boolean; limited: boolean};
type Schedule = {lane: Uint8Array; start: Float32Array; totalMs: number};

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

/* Seeded, so the server and the browser draw the same run. */
function buildTests(): SimTest[] {
  const next = random(1729);
  const pick = (names: string[]) => names[Math.floor(next() * names.length)];
  const tests: SimTest[] = [
    {name: 'SchemaTests.Verifies_row_counts', ms: 190, group: 'chain', dependsOn: 2},
    {name: 'SchemaTests.Migrates_schema', ms: 320, group: 'chain'},
    {name: 'SchemaTests.Seeds_reference_data', ms: 240, group: 'chain', dependsOn: 1},
  ];
  // Tests of one class are queued together, so without a rule each group below would start all at once.
  for (let i = 0; i < 57; i++) {
    const ms = Math.round(40 + Math.pow(next(), 2) * 260);
    if (i < 10) tests.push({name: `CheckoutApiTests.${pick(methodNames)}`, ms, group: 'api'});
    else if (i >= 20 && i < 28) tests.push({name: `OrderRepositoryTests.${pick(methodNames)}`, ms, group: 'database'});
    else tests.push({name: `${pick(classNames)}.${pick(methodNames)}`, ms, group: 'plain'});
  }
  return tests;
}

const simTests = buildTests();

/* Each test goes on the worker that can start it soonest, once the rules that apply to it allow. */
function planSchedule(rules: Rules): Schedule {
  const lane = new Uint8Array(simTests.length);
  const start = new Float32Array(simTests.length);
  const end = new Float32Array(simTests.length);
  const placed = new Uint8Array(simTests.length);
  const workers = new Array<number>(workerCount).fill(0);
  const apiSlots = [0, 0];
  let databaseFree = 0;

  const place = (index: number) => {
    if (placed[index]) return;
    const test = simTests[index];
    let ready = 0;
    if (rules.ordered && test.dependsOn !== undefined) {
      place(test.dependsOn);
      ready = end[test.dependsOn];
    }
    if (rules.exclusive && test.group === 'database') ready = Math.max(ready, databaseFree);
    const slot = apiSlots[0] <= apiSlots[1] ? 0 : 1;
    if (rules.limited && test.group === 'api') ready = Math.max(ready, apiSlots[slot]);

    // Among the workers that give the earliest start, take the one that was busy latest, to leave fewer gaps.
    let worker = 0;
    for (let w = 1; w < workerCount; w++) {
      const candidate = Math.max(workers[w], ready);
      const best = Math.max(workers[worker], ready);
      if (candidate < best || (candidate === best && workers[w] > workers[worker])) worker = w;
    }
    lane[index] = worker;
    start[index] = Math.max(workers[worker], ready);
    end[index] = start[index] + test.ms;
    workers[worker] = end[index];
    if (test.group === 'database') databaseFree = end[index];
    if (test.group === 'api') apiSlots[slot] = end[index];
    placed[index] = 1;
  };
  simTests.forEach((_, index) => place(index));
  return {lane, start, totalMs: Math.max(...workers)};
}

/* The time axis never rescales: it fits the longest of the eight possible schedules. */
const axisMs = Math.max(...[0, 1, 2, 3, 4, 5, 6, 7].map(bits =>
  planSchedule({exclusive: !!(bits & 1), ordered: !!(bits & 2), limited: !!(bits & 4)}).totalMs));

const ruleSwitches: {key: keyof Rules; group: Group; attribute: string; effect: string}[] = [
  {key: 'exclusive', group: 'database', attribute: '[NotInParallel("Database")]', effect: 'Database tests take turns'},
  {key: 'ordered', group: 'chain', attribute: '[DependsOn(nameof(Migrates_schema))]', effect: 'Migrate, then seed, then verify'},
  {key: 'limited', group: 'api', attribute: '[ParallelLimiter<TwoAtATime>]', effect: 'API tests run two at a time'},
];

type Hover = {x: number; y: number; flip: boolean; index: number};

function RunChapter(): JSX.Element {
  const [rules, setRules] = useState<Rules>({exclusive: false, ordered: false, limited: false});
  const [hover, setHover] = useState<Hover | null>(null);
  const schedule = planSchedule(rules);
  const active = ruleSwitches.filter(rule => rules[rule.key]).length;

  const inspect = (event: PointerEvent<HTMLDivElement>) => {
    const index = (event.target as HTMLElement).dataset.note;
    if (index === undefined) {
      setHover(null);
      return;
    }
    const bounds = event.currentTarget.getBoundingClientRect();
    const x = event.clientX - bounds.left;
    setHover({x, y: event.clientY - bounds.top, flip: x > bounds.width / 2, index: Number(index)});
  };

  return (
    <section id="run" className={styles.chapter} aria-labelledby="run-title">
      <div className={styles.container}>
        <ChapterHead index={2} title="Say how your tests relate. TUnit schedules the rest.">
          <p>
            Tests run in parallel unless you say otherwise. When some of them share a database, need an order
            or would swamp a service, one attribute says so. Switch them on and watch the same run rearrange.
          </p>
          <Link to="/docs/execution/parallelism">Control parallel execution</Link>
        </ChapterHead>
        <figure className={styles.stage}>
          <div className={styles.rules}>
            {ruleSwitches.map(rule => (
              <button key={rule.key} type="button" aria-pressed={rules[rule.key]} className={styles[rule.group]}
                onClick={() => setRules({...rules, [rule.key]: !rules[rule.key]})}>
                <span className={styles.ruleSwitch} aria-hidden="true" />
                <code>{rule.attribute}</code>
                <span>{rule.effect}</span>
              </button>
            ))}
          </div>
          <div className={styles.stageBody}>
            <div className={styles.roll} onPointerMove={inspect} onPointerLeave={() => setHover(null)} aria-hidden="true">
              <div className={styles.laneLines} />
              {simTests.map((test, index) => (
                <span key={index} data-note={index} className={`${styles.note} ${styles[test.group]}`} style={{
                  '--lane': schedule.lane[index],
                  '--x': `${schedule.start[index] / axisMs * 100}%`,
                  '--w': `${test.ms / axisMs * 100}%`,
                } as CSSProperties} />
              ))}
              {hover && (
                <div className={hover.flip ? `${styles.tooltip} ${styles.tooltipFlip}` : styles.tooltip} style={{left: hover.x, top: hover.y}}>
                  <span>{simTests[hover.index].name}</span>
                  <span>Passed in {simTests[hover.index].ms} ms</span>
                </div>
              )}
            </div>
            <div className={styles.stageStat} role="status">
              <strong>{(schedule.totalMs / 1000).toFixed(2)} s</strong>
              <span>{active === 0 ? 'Nothing held back' : `${active} of 3 rules applied`}</span>
            </div>
          </div>
          <figcaption className={styles.stageCaption}>
            A simulated run of {simTests.length} tests on {workerCount} workers. Each bar is one test, as long as it took.
            Rules cost a little time and buy tests that don't trip over each other.
          </figcaption>
        </figure>
      </div>
    </section>
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

/* The data file is rewritten by the weekly Speed Comparison workflow and read here when the site builds. */
const benchmarkSdk = (latestBenchmarks.environment as {sdk?: string}).sdk;
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
    // A benchmark that failed to run is published without a usable mean; leave its lane out.
    const ms = row ? parseMs(row.Mean) : NaN;
    return row && Number.isFinite(ms) && ms > 0 ? [{method, label, version: row.Version, ms}] : [];
  });
  const tunit = lanes[0];
  const rivals = lanes.slice(1);
  if (!tunit || !tunit.method.startsWith('TUnit') || rivals.length === 0) return null;
  const slowest = Math.max(...lanes.map(lane => lane.ms));
  const closest = rivals.reduce((best, lane) => lane.ms < best.ms ? lane : best);
  const ahead = closest.ms >= tunit.ms;
  const factor = ahead ? closest.ms / tunit.ms : tunit.ms / closest.ms;

  const choose = (setIndex: (index: number) => void, index: number) => {
    setIndex(index);
    start();
  };

  return (
    <section ref={sectionRef} id="measured" className={`${styles.chapter} ${styles.chapterTint}`} aria-labelledby="measured-title">
      <div className={styles.container}>
        <ChapterHead index={3} title="Raced in real time.">
          <p>
            Every bar grows for exactly as long as that framework took to run the same tests in the latest benchmark run.
          </p>
        </ChapterHead>
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
          <span>Mean run time, {benchmarkDate}. {benchmarkSdk ? `${benchmarkSdk} on GitHub Actions.` : 'Run on GitHub Actions.'}</span>
          <button type="button" onClick={start}>Race again</button>
          <Link to="/docs/benchmarks">All benchmarks and methodology</Link>
        </div>
      </div>
    </section>
  );
}

/* ---------- As it grows: the same framework for bigger tests. ---------- */

function GrowChapter(): JSX.Element {
  return (
    <section id="grow" className={styles.chapter} aria-labelledby="grow-title">
      <div className={`${styles.container} ${styles.split}`}>
        <ChapterHead index={4} title="From one method to the whole system.">
          <p>
            The same framework drives integration tests, with shared infrastructure that starts once and is cleaned
            up after the last test using it finishes. One test method can also become many cases, fed from
            attributes, methods, classes or a matrix of values.
          </p>
          <Link to="/docs/writing-tests/data-driven-overview">Data-driven tests</Link>
        </ChapterHead>
        <ul className={styles.integrations}>
          <li><Link to="/docs/examples/aspnet">ASP.NET Core</Link><span>Endpoints with isolated clients</span></li>
          <li><Link to="/docs/examples/aspire">Aspire</Link><span>Your whole distributed app</span></li>
          <li><Link to="/docs/examples/playwright">Playwright</Link><span>A clean browser page per test</span></li>
          <li><Link to="/docs/writing-tests/mocking">TUnit.Mocks</Link><span>Source-generated, AOT-safe mocks</span></li>
        </ul>
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
          <h2 id="start-title" className={styles.sectionTitle}>Three commands to your first test.</h2>
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
    <Layout title="Know sooner" description="TUnit is a source-generated testing framework for modern .NET. Tests are checked as you type, discovered at compile time, and run in parallel the way you describe.">
      <main className={styles.page}><Hero /><TypeChapter /><BuildChapter /><RunChapter /><Race /><GrowChapter /><Start /></main>
    </Layout>
  );
}
