import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import {Highlight, themes} from 'prism-react-renderer';
import {useState, type CSSProperties, type JSX} from 'react';

import styles from './index.module.css';

const examples = [
  {
    label: 'Data-driven tests',
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
    note: 'One test. Three cases. Discovered at compile time.',
    href: '/docs/writing-tests/data-driven-overview',
  },
  {
    label: 'Fluent assertions',
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
    note: 'Awaitable assertions that read like your intent.',
    href: '/docs/assertions/getting-started',
  },
  {
    label: 'Lifecycle hooks',
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
    note: 'Setup and cleanup, right where they belong.',
    href: '/docs/writing-tests/hooks',
  },
];

const installSteps = [
  {label: 'Install the template', command: 'dotnet new install TUnit.Templates'},
  {label: 'Create your project', command: 'dotnet new TUnit -n MyTests'},
  {label: 'Run your tests', command: 'dotnet run --project MyTests'},
];

const integrations = [
  {title: 'ASP.NET Core', kind: 'API tests', copy: 'Test your endpoints with isolated clients and shared infrastructure.', href: '/docs/examples/aspnet'},
  {title: 'Aspire', kind: 'Distributed apps', copy: 'Bring your whole application into the test lifecycle.', href: '/docs/examples/aspire'},
  {title: 'Playwright', kind: 'Browser tests', copy: 'Give every test a clean browser context and page.', href: '/docs/examples/playwright'},
  {title: 'TUnit.Mocks', kind: 'Test doubles', copy: 'Set up and verify source-generated, AOT-compatible mocks.', href: '/docs/writing-tests/mocking'},
];

function Checkmark(): JSX.Element {
  return <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="m5 12 4 4L19 6" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}

function TestWorkbench(): JSX.Element {
  const [selected, setSelected] = useState(0);
  const example = examples[selected];

  return (
    <div className={styles.workbench}>
      <div className={styles.workbenchHeading}>
        <span>A little C#. A lot covered.</span>
        <span className={styles.language}>.NET</span>
      </div>
      <div className={styles.examplePicker} role="group" aria-label="Choose a C# example">
        {examples.map((item, index) => (
          <button key={item.label} type="button" aria-pressed={selected === index}
            aria-controls="test-example" onClick={() => setSelected(index)}>
            {item.label}
          </button>
        ))}
      </div>
      <div id="test-example" role="region" aria-label={example.label}>
        <div className={styles.fileName}><span aria-hidden="true">C#</span>{example.file}<span>Class members</span></div>
        <Highlight code={example.code} language="csharp" theme={themes.github}>
          {({tokens, getLineProps, getTokenProps}) => (
            <pre className={styles.code} tabIndex={0} aria-label={`${example.file} example`}>
              <code>
                {tokens.map((line, lineIndex) => (
                  <div key={lineIndex} {...getLineProps({line})}>
                    <span className={styles.lineNumber} aria-hidden="true">{lineIndex + 1}</span>
                    {line.map((token, tokenIndex) => {
                      const props = getTokenProps({token});
                      return <span key={tokenIndex} {...props} style={{'--preview-token': props.style?.color ?? '#214b3c'} as CSSProperties} />;
                    })}
                  </div>
                ))}
              </code>
            </pre>
          )}
        </Highlight>
        <div className={styles.testResults}>
          <div className={styles.resultsHeading}><span>Example results</span><strong>{example.results.length} passed</strong></div>
          <ul>
            {example.results.map(result => <li key={result}><Checkmark /><span>{result}</span><span>Passed</span></li>)}
          </ul>
        </div>
        <div className={styles.exampleNote}>
          <p>{example.note}</p>
          <Link to={example.href}>Read the guide</Link>
        </div>
      </div>
    </div>
  );
}

function Hero(): JSX.Element {
  return (
    <header className={styles.hero}>
      <div className={styles.container}>
        <div className={styles.heroIntro}><span>The testing framework for modern .NET</span><Link to="https://github.com/thomhurst/TUnit">Open source, from the start</Link></div>
        <div className={styles.heroGrid}>
          <div className={styles.heroCopy}>
            <h1 className={styles.wordmark} aria-label="TUnit: modern .NET testing">TUnit<span className={styles.wordmarkCheck} aria-hidden="true"><Checkmark /></span></h1>
            <p className={styles.heroTitle}>Give your tests<br />a head start.</p>
            <p className={styles.heroLead}>Discover tests while you compile. Run them in parallel.
              Get back to the code you came to write.</p>
            <div className={styles.actions}>
              <Link className={styles.primaryButton} to="/docs/getting-started/installation">Start testing</Link>
              <Link className={styles.quietLink} to="/docs/guides/philosophy">Why TUnit?</Link>
            </div>
            <p className={styles.heroFootnote}>Source generated. Async native. AOT ready.</p>
          </div>
          <TestWorkbench />
        </div>
        <div className={styles.compilePath} aria-label="TUnit discovers tests at compile time and executes them in parallel">
          <span>Your C# tests</span><span className={styles.pathLine} aria-hidden="true" />
          <span>Compile-time discovery</span><span className={styles.pathLine} aria-hidden="true" />
          <span className={styles.parallelMark} aria-hidden="true"><i /><i /><i /></span><span>Parallel execution</span>
        </div>
      </div>
    </header>
  );
}

function QuickStart(): JSX.Element {
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
    <section className={`${styles.quickStart} ${styles.container}`} aria-labelledby="quick-start-title">
      <div className={styles.quickStartIntro}>
        <h2 id="quick-start-title">Meet your next<br /> test project.</h2>
        <p>Have the .NET SDK?<br /> You’re three commands away.</p>
        <Link to="/docs/getting-started/installation">Installation guide</Link>
      </div>
      <div className={styles.installCommands}>
        <ol>
          {installSteps.map((step, index) => (
            <li key={step.label}>
              <span className={styles.stepNumber} aria-hidden="true">{index + 1}</span>
              <div><span className={styles.stepLabel}>{step.label}</span><code>{step.command}</code></div>
              <button type="button" onClick={() => copyCommand(step.command, index)} aria-label={`Copy command: ${step.label}`}>
                {copyStatus?.index === index && copyStatus.ok ? 'Copied' : 'Copy'}
              </button>
            </li>
          ))}
        </ol>
        <p className={styles.copyStatus} role="status">{copyStatus ? (copyStatus.ok ? `${installSteps[copyStatus.index].label}: command copied.` : 'Couldn’t copy automatically. Select and copy the command above.') : '\u00a0'}</p>
      </div>
    </section>
  );
}

function Architecture(): JSX.Element {
  return (
    <section className={styles.architecture} aria-labelledby="architecture-title">
      <div className={`${styles.container} ${styles.architectureGrid}`}>
        <div className={styles.architectureIntro}>
          <h2 id="architecture-title">Good tests.<br />Less machinery.</h2>
          <p>The compiler already knows your code. TUnit puts that knowledge to work before your tests even start.</p>
          <Link className={styles.quietLink} to="/docs/guides/philosophy">Explore the design</Link>
          <div className={styles.discoveryDiagram} aria-hidden="true">
            <span>[Test]</span><span>[Test]</span><span>[Test]</span>
            <div className={styles.discoveryBus} />
            <strong>TUnit source generator</strong>
            <div className={styles.discoveryOutput}><Checkmark />Ready to run</div>
          </div>
        </div>
        <div className={styles.featureList}>
          <article><h3>Discovered before you run.</h3><p>Source generators turn your tests into executable code at build time, removing the need for runtime reflection to discover them.</p><Link to="/docs/writing-tests/aot">Native AOT support</Link></article>
          <article><h3>Parallel comes as standard.</h3><p>Tests run concurrently by default. Add dependencies, limits, or shared resources when your suite needs more control.</p><Link to="/docs/execution/parallelism">Control parallel execution</Link></article>
          <article><h3>Mistakes meet the compiler.</h3><p>Roslyn analyzers catch invalid hooks, data-source mistakes, and missing assertion awaits while you’re writing your tests.</p><Link to="/docs/assertions/awaiting">See assertion diagnostics</Link></article>
        </div>
      </div>
    </section>
  );
}

function Ecosystem(): JSX.Element {
  return (
    <section className={`${styles.container} ${styles.ecosystem}`} aria-labelledby="ecosystem-title">
      <div className={styles.sectionHeading}><h2 id="ecosystem-title">Small units.<br />Whole systems.</h2><p>The same testing framework, wherever your code takes you.</p></div>
      <div className={styles.integrationList}>
        {integrations.map(item => <Link className={styles.integration} key={item.title} to={item.href}><span className={styles.integrationKind}>{item.kind}</span><h3>{item.title}</h3><p>{item.copy}</p><span className={styles.integrationAction}>View example <span aria-hidden="true">↗</span></span></Link>)}
      </div>
      <aside className={styles.benchmarkNote} aria-labelledby="benchmark-title"><div><h3 id="benchmark-title">Put the fast part to the test.</h3><p>Explore measured results, framework comparisons, and the methodology behind them.</p></div><Link className={styles.secondaryButton} to="/docs/benchmarks">Explore benchmarks</Link></aside>
    </section>
  );
}

function NextSteps(): JSX.Element {
  return (
    <section className={styles.nextSteps} aria-labelledby="next-steps-title">
      <div className={`${styles.container} ${styles.nextStepsGrid}`}>
        <div><h2 id="next-steps-title">Your next test<br />starts here.</h2><Link className={styles.primaryButton} to="/docs/getting-started/writing-your-first-test">Write your first test</Link></div>
        <div className={styles.migration}><h3>Already have a test suite?</h3><p>See how TUnit compares with xUnit, NUnit, and MSTest, and find a familiar place to begin.</p><Link className={styles.quietLink} to="/docs/comparison/framework-differences">Compare frameworks</Link></div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  return (
    <Layout title="Give your tests a head start" description="TUnit is a source-generated testing framework for modern .NET. Discover tests at compile time, run in parallel, and write expressive async assertions.">
      <main className={styles.page}><Hero /><QuickStart /><Architecture /><Ecosystem /><NextSteps /></main>
    </Layout>
  );
}
