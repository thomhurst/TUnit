import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import {Highlight, themes} from 'prism-react-renderer';
import {useCallback, useEffect, useRef, useState, type CSSProperties, type JSX} from 'react';

import styles from './index.module.css';

/* A representative run: start and end are fractions of the run's duration. */
const runDurationSeconds = 1.42;
const lanes = [
  [
    {name: 'Add(1, 2, 3)', start: 0, end: 0.1},
    {name: 'Add(5, 8, 13)', start: 0.11, end: 0.21},
    {name: 'Checkout_applies_discount', start: 0.22, end: 0.55},
    {name: 'Orders_api_returns_201', start: 0.56, end: 0.93},
  ],
  [
    {name: 'Greeting_contains_name', start: 0, end: 0.18},
    {name: 'Login_page_shows_errors', start: 0.19, end: 0.71},
    {name: 'Token_expires', start: 0.72, end: 0.86},
  ],
  [
    {name: 'Parses_iso_dates', start: 0, end: 0.15},
    {name: 'Cart_total_includes_tax', start: 0.16, end: 0.36},
    {name: 'Search_ranks_exact_matches', start: 0.37, end: 0.68},
    {name: 'Invoice_pdf_renders', start: 0.69, end: 1},
  ],
  [
    {name: 'Mock_repository_is_called', start: 0, end: 0.24},
    {name: 'Queue_retries_twice', start: 0.25, end: 0.58},
    {name: 'Settings_round_trip', start: 0.59, end: 0.79},
  ],
];
const testCount = lanes.reduce((total, lane) => total + lane.length, 0);

function Tick(): JSX.Element {
  return <svg viewBox="0 0 16 16" aria-hidden="true"><path d="m4 8.4 2.6 2.6L12 5.4" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}

function RunPanel(): JSX.Element {
  // Server render shows the finished run; the browser replays it once on load.
  const [progress, setProgress] = useState(1);
  const frame = useRef(0);

  const play = useCallback(() => {
    cancelAnimationFrame(frame.current);
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      setProgress(1);
      return;
    }
    const duration = 2600;
    const begin = performance.now();
    const step = (now: number) => {
      const t = Math.min((now - begin) / duration, 1);
      setProgress(t);
      if (t < 1) frame.current = requestAnimationFrame(step);
    };
    setProgress(0);
    frame.current = requestAnimationFrame(step);
  }, []);

  useEffect(() => {
    play();
    return () => cancelAnimationFrame(frame.current);
  }, [play]);

  const passed = lanes.flat().filter(test => test.end <= progress).length;
  const done = progress >= 1;

  return (
    <figure className={styles.run} aria-label={`Example test run: ${testCount} tests found at build time and run across four workers. All passed in ${runDurationSeconds} seconds.`}>
      <div className={styles.runCommand} aria-hidden="true">
        <span className={styles.prompt}>&gt;</span> dotnet run --project MyTests
      </div>
      <p className={styles.runDiscovery} aria-hidden="true">{testCount} tests found at build time, no reflection scan needed.</p>
      <div className={styles.lanes} aria-hidden="true" style={{'--playhead': progress} as CSSProperties}>
        {lanes.map((lane, laneIndex) => (
          <div className={styles.lane} key={laneIndex}>
            <span className={styles.laneName}>Worker {laneIndex + 1}</span>
            <div className={styles.track}>
              {lane.map(test => {
                const visible = Math.max(0, Math.min(progress, test.end) - test.start);
                if (visible <= 0) return null;
                const finished = test.end <= progress;
                return (
                  <span
                    key={test.name}
                    className={finished ? `${styles.bar} ${styles.barDone}` : styles.bar}
                    style={{left: `${test.start * 100}%`, width: `calc(${visible * 100}% - 3px)`}}>
                    <span className={styles.barLabel}>{test.name}</span>
                    {finished && <Tick />}
                  </span>
                );
              })}
            </div>
          </div>
        ))}
        <span className={styles.playhead} />
      </div>
      <figcaption className={styles.runSummary}>
        <span className={done ? `${styles.verdict} ${styles.verdictPassed}` : styles.verdict} aria-hidden="true">
          {done ? <><Tick />Passed</> : 'Running'}
        </span>
        <span aria-hidden="true"><strong>{passed}</strong> of {testCount} passed, 0 failed</span>
        <span aria-hidden="true">{(progress * runDurationSeconds).toFixed(2)}s</span>
        <button type="button" className={styles.replay} onClick={play} disabled={!done}>Run again</button>
      </figcaption>
    </figure>
  );
}

function Hero(): JSX.Element {
  return (
    <header className={styles.hero}>
      <div className={styles.container}>
        <div className={styles.heroCopy}>
          <h1 className={styles.heroTitle}>
            <span>Found while you build.</span>
            <span>Run all at once.</span>
          </h1>
          <div className={styles.heroAside}>
            <p className={styles.heroLead}>
              TUnit is a testing framework for modern .NET. A source generator finds your tests
              at compile time, so they start immediately and run in parallel by default.
            </p>
            <div className={styles.actions}>
              <Link className={styles.primaryButton} to="/docs/getting-started/installation">Get started</Link>
              <Link className={styles.quietLink} to="/docs/guides/philosophy">Why TUnit?</Link>
            </div>
          </div>
        </div>
        <RunPanel />
      </div>
    </header>
  );
}

const examples = [
  {
    label: 'Data-driven',
    file: 'CalculatorTests.cs',
    code: `[Test]
[Arguments(1, 2, 3)]
[Arguments(5, 8, 13)]
[Arguments(-1, 1, 0)]
public async Task Add(int a, int b, int expected)
{
    await Assert.That(a + b)
        .IsEqualTo(expected);
}`,
    results: ['Add(1, 2, 3)', 'Add(5, 8, 13)', 'Add(-1, 1, 0)'],
    note: 'One method, three test cases, each reported on its own.',
    href: '/docs/writing-tests/data-driven-overview',
  },
  {
    label: 'Assertions',
    file: 'GreetingTests.cs',
    code: `[Test]
public async Task Greeting_contains_name()
{
    var greeting = "Hello, Ada!";

    await Assert.That(greeting)
        .StartsWith("Hello")
        .And.Contains("Ada");
}`,
    results: ['Greeting_contains_name'],
    note: 'Assertions are awaited and chain with And and Or.',
    href: '/docs/assertions/getting-started',
  },
  {
    label: 'Hooks',
    file: 'LifecycleTests.cs',
    code: `[Before(Test)]
public void Setup() => _value = 42;

[Test]
public async Task Setup_runs_before_test()
{
    await Assert.That(_value).IsEqualTo(42);
}

private int _value;`,
    results: ['Setup_runs_before_test'],
    note: 'Setup and cleanup at test, class, assembly, or session level.',
    href: '/docs/writing-tests/hooks',
  },
];

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

function CodeLines({code, flaggedLine}: {code: string; flaggedLine?: number}): JSX.Element {
  return (
    <Highlight code={code} language="csharp" theme={themes.github}>
      {({tokens, getLineProps, getTokenProps}) => (
        <code>
          {tokens.map((line, lineIndex) => {
            const {style: _lineStyle, ...lineProps} = getLineProps({line});
            return (
              <div key={lineIndex} {...lineProps}>
                <span className={styles.lineNumber} aria-hidden="true">{lineIndex + 1}</span>
                {lineIndex === flaggedLine ? <FlaggedTokens line={line} getTokenProps={getTokenProps} /> : line.map((token, tokenIndex) => {
                  const {style: _tokenStyle, ...tokenProps} = getTokenProps({token});
                  return <span key={tokenIndex} {...tokenProps} />;
                })}
              </div>
            );
          })}
        </code>
      )}
    </Highlight>
  );
}

function Write(): JSX.Element {
  const [selected, setSelected] = useState(0);
  const example = examples[selected];

  return (
    <section className={styles.stage} aria-labelledby="write-title">
      <div className={`${styles.container} ${styles.stageGrid}`}>
        <div className={styles.stageIntro}>
          <h2 id="write-title">Write.</h2>
          <p>Tests are plain C# methods with an attribute. Data, hooks, and assertions all use the same small set of ideas.</p>
          <Link className={styles.quietLink} to="/docs/getting-started/writing-your-first-test">Write your first test</Link>
        </div>
        <div className={styles.editor}>
          <div className={styles.editorTabs} role="group" aria-label="Choose an example">
            {examples.map((item, index) => (
              <button key={item.label} type="button" aria-pressed={selected === index}
                aria-controls="write-example" onClick={() => setSelected(index)}>
                {item.label}
              </button>
            ))}
            <span className={styles.editorFile}>{example.file}</span>
          </div>
          <div id="write-example" role="region" aria-label={`${example.label} example`}>
            <pre className={styles.code} tabIndex={0}><CodeLines code={example.code} /></pre>
            <div className={styles.results}>
              <ul aria-label="Results">
                {example.results.map(result => <li key={result}><Tick /><span>{result}</span></li>)}
              </ul>
              <p>{example.note} <Link to={example.href}>Read the guide</Link></p>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

const analyzerCode = `[Test]
public async Task Cart_total_includes_tax()
{
    var total = _cart.Total();

    Assert.That(total).IsEqualTo(108m);
}`;

function Build(): JSX.Element {
  return (
    <section className={`${styles.stage} ${styles.stageTinted}`} aria-labelledby="build-title">
      <div className={`${styles.container} ${styles.stageGrid}`}>
        <div className={styles.stageIntro}>
          <h2 id="build-title">Build.</h2>
          <p>The compiler already knows your code. TUnit puts that knowledge to work before a single test runs.</p>
        </div>
        <div className={styles.buildBody}>
          <figure className={styles.diagnostic}>
            <pre className={styles.code} tabIndex={0}><CodeLines code={analyzerCode} flaggedLine={5} /></pre>
            <figcaption>
              <span className={styles.diagnosticId}>TUnitAssertions0002</span>
              Assert statements must be awaited. All TUnit assertions return Task.
            </figcaption>
          </figure>
          <div className={styles.points}>
            <div>
              <h3>Tests are found at compile time.</h3>
              <p>A source generator writes the code that registers each test. There is no reflection scan at startup, and test projects can publish with Native AOT.</p>
              <Link to="/docs/writing-tests/aot">Native AOT support</Link>
            </div>
            <div>
              <h3>Mistakes show up in your editor.</h3>
              <p>Analyzers flag an assertion you forgot to await, a hook with the wrong signature, or a data source that does not match its parameters.</p>
              <Link to="/docs/assertions/awaiting">Assertion diagnostics</Link>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

const integrations = [
  {title: 'ASP.NET Core', copy: 'Test your endpoints with isolated clients and shared infrastructure.', href: '/docs/examples/aspnet'},
  {title: 'Aspire', copy: 'Bring your whole distributed application into the test lifecycle.', href: '/docs/examples/aspire'},
  {title: 'Playwright', copy: 'Give every test a clean browser context and page.', href: '/docs/examples/playwright'},
  {title: 'TUnit.Mocks', copy: 'Source-generated, AOT-compatible mocks you set up and verify.', href: '/docs/writing-tests/mocking'},
];

function Run(): JSX.Element {
  return (
    <section className={styles.stage} aria-labelledby="run-title">
      <div className={`${styles.container} ${styles.stageGrid}`}>
        <div className={styles.stageIntro}>
          <h2 id="run-title">Run.</h2>
          <p>Tests run in parallel unless you say otherwise. Add dependencies, limits, or shared resources when a suite needs more control.</p>
          <Link className={styles.quietLink} to="/docs/execution/parallelism">Control parallel execution</Link>
        </div>
        <div>
          <ul className={styles.integrations}>
            {integrations.map(item => (
              <li key={item.title}>
                <Link to={item.href}>
                  <h3>{item.title}</h3>
                  <p>{item.copy}</p>
                </Link>
              </li>
            ))}
          </ul>
          <p className={styles.benchmarks}>
            How fast is fast? <Link to="/docs/benchmarks">See the benchmarks</Link> and the method behind them.
          </p>
        </div>
      </div>
    </section>
  );
}

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
          <h2 id="start-title">Three commands to your first passing run.</h2>
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
          <Link className={styles.primaryButton} to="/docs/getting-started/installation">Read the installation guide</Link>
        </div>
        <div className={styles.migration}>
          <h3>Moving from xUnit, NUnit, or MSTest?</h3>
          <p>Migration guides map each attribute, assertion, and lifecycle hook you use today to its TUnit equivalent.</p>
          <Link className={styles.quietLink} to="/docs/comparison/framework-differences">Compare frameworks</Link>
        </div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  return (
    <Layout title="Found while you build. Run all at once." description="TUnit is a source-generated testing framework for modern .NET. Tests are found at compile time, run in parallel by default, and use awaitable, fluent assertions.">
      <main className={styles.page}><Hero /><Write /><Build /><Run /><Start /></main>
    </Layout>
  );
}
