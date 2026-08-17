import Link from '@docusaurus/Link';
import Layout from '@theme/Layout';
import {Highlight, themes} from 'prism-react-renderer';
import type {JSX} from 'react';

import styles from './index.module.css';

const testCode = `[Test]
public async Task Checkout_calculates_total()
{
    var total = await checkout.GetTotal();

    await Assert.That(total)
        .IsEqualTo(42.00m);
}`;

const featureCards = [
  {
    number: '01',
    eyebrow: 'Less waiting',
    title: 'Fast by architecture.',
    description:
      'Compile-time discovery removes runtime reflection. Parallel execution is the default, with precise controls when order matters.',
    tags: ['Source generated', 'Parallel by default'],
  },
  {
    number: '02',
    eyebrow: 'Fewer surprises',
    title: 'Catch mistakes at build time.',
    description:
      'Roslyn analyzers flag broken data sources, invalid hooks, and assertion mistakes before they become a red CI run.',
    tags: ['Compile-time safety', 'Actionable diagnostics'],
  },
  {
    number: '03',
    eyebrow: 'Modern everywhere',
    title: 'Built for today’s .NET.',
    description:
      'Native AOT, trimming, async assertions, Microsoft.Testing.Platform, and first-class integrations—without legacy baggage.',
    tags: ['Native AOT', 'Microsoft.Testing.Platform'],
  },
];

const ecosystemCards = [
  {
    mark: 'A',
    title: 'ASP.NET Core',
    copy: 'Per-test isolation, shared infrastructure, and trace-aware test clients.',
    href: '/docs/examples/aspnet',
  },
  {
    mark: '◈',
    title: 'Aspire',
    copy: 'Start, await, test, and dispose your distributed app as one fixture.',
    href: '/docs/examples/aspire',
  },
  {
    mark: 'P',
    title: 'Playwright',
    copy: 'Browser, context, and page lifecycle handled for clean end-to-end tests.',
    href: '/docs/examples/playwright',
  },
  {
    mark: 'M',
    title: 'TUnit.Mocks',
    copy: 'Source-generated, AOT-compatible mocks with setup and verification built in.',
    href: '/docs/writing-tests/mocking',
  },
];

function Arrow(): JSX.Element {
  return <span aria-hidden="true">↗</span>;
}

function SyntaxPreview(): JSX.Element {
  return (
    <Highlight code={testCode} language="csharp" theme={themes.dracula}>
      {({className, tokens, getLineProps, getTokenProps}) => (
        <pre className={`${className} ${styles.code}`}>
          {tokens.map((line, lineIndex) => (
            <div key={lineIndex} {...getLineProps({line})}>
              {line.map((token, tokenIndex) => (
                <span key={tokenIndex} {...getTokenProps({token})} />
              ))}
            </div>
          ))}
        </pre>
      )}
    </Highlight>
  );
}

function Hero(): JSX.Element {
  return (
    <header className={styles.hero}>
      <div className={styles.heroGlow} />
      <div className={`container ${styles.heroGrid}`}>
        <div className={styles.heroCopy}>
          <div className={styles.eyebrow}>
            <span className={styles.pulse} />
            Modern .NET testing, recompiled
          </div>
          <h1 className={styles.heroTitle}>
            Tests should run.
            <span>Not make you wait.</span>
          </h1>
          <p className={styles.heroLead}>
            TUnit moves discovery to compile time, runs tests in parallel, and catches mistakes
            before execution—so your feedback loop stays fast as your suite grows.
          </p>
          <div className={styles.heroActions}>
            <Link className={styles.primaryButton} to="/docs/getting-started/installation">
              Start testing <span aria-hidden="true">→</span>
            </Link>
            <Link className={styles.textButton} to="/docs/guides/philosophy">
              Why TUnit? <Arrow />
            </Link>
          </div>
          <div className={styles.heroSignals} aria-label="TUnit highlights">
            <span><b>01</b> Source-generated</span>
            <span><b>02</b> Native AOT ready</span>
            <span><b>03</b> Open source</span>
          </div>
        </div>

        <div className={styles.codeStage}>
          <div className={styles.codeHalo} />
          <div className={styles.codeWindow}>
            <div className={styles.windowBar}>
              <div className={styles.windowDots} aria-hidden="true"><i /><i /><i /></div>
              <span>CheckoutTests.cs</span>
              <span className={styles.windowStatus}>TUnit</span>
            </div>
            <SyntaxPreview />
            <div className={styles.resultRow}>
              <div className={styles.resultIcon}>✓</div>
              <div>
                <strong>Checkout_calculates_total</strong>
                <span>Passed</span>
              </div>
              <time>12 ms</time>
            </div>
          </div>
          <div className={styles.floatingBadge}>
            <span>Compile-time</span>
            <strong>discovery</strong>
          </div>
        </div>
      </div>
    </header>
  );
}

function InstallStrip(): JSX.Element {
  return (
    <section className={styles.installStrip} aria-label="Quick install">
      <div className={`container ${styles.installGrid}`}>
        <div>
          <span className={styles.stepLabel}>01 / Install</span>
          <code>dotnet new install TUnit.Templates</code>
        </div>
        <div>
          <span className={styles.stepLabel}>02 / Create</span>
          <code>dotnet new TUnit -n MyTests</code>
        </div>
        <div>
          <span className={styles.stepLabel}>03 / Run</span>
          <code>dotnet test</code>
        </div>
      </div>
    </section>
  );
}

function Features(): JSX.Element {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.kicker}>Designed differently</span>
            <h2>Your test suite.<br />Without the drag.</h2>
          </div>
          <p>
            TUnit shifts work out of runtime and into your compiler, pairing speed with strong
            diagnostics and APIs that feel native to modern C#.
          </p>
        </div>
        <div className={styles.featureGrid}>
          {featureCards.map((feature) => (
            <article className={styles.featureCard} key={feature.number}>
              <div className={styles.featureTop}>
                <span>{feature.eyebrow}</span>
                <b>{feature.number}</b>
              </div>
              <h3>{feature.title}</h3>
              <p>{feature.description}</p>
              <div className={styles.tagList}>
                {feature.tags.map((tag) => <span key={tag}>{tag}</span>)}
              </div>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}

function Performance(): JSX.Element {
  return (
    <section className={styles.performance}>
      <div className={`container ${styles.performanceGrid}`}>
        <div className={styles.performanceCopy}>
          <span className={styles.kicker}>Proof, not promises</span>
          <h2>Built to keep<br />feedback fast.</h2>
          <p>
            Daily BenchmarkDotNet runs compare real test scenarios on the latest .NET SDK.
            Inspect every number and the methodology behind it.
          </p>
          <Link className={styles.outlineButton} to="/docs/benchmarks">
            Explore benchmarks <Arrow />
          </Link>
        </div>
        <div className={styles.benchmarkVisual}>
          <div className={styles.benchmarkHeader}>
            <span>Massive parallel tests</span>
            <span>lower is better ↓</span>
          </div>
          <div className={styles.bars}>
            <div className={styles.barRow}>
              <span>TUnit AOT</span><div><i style={{width: '7%'}} /></div><b>218 ms</b>
            </div>
            <div className={styles.barRow}>
              <span>TUnit</span><div><i style={{width: '16%'}} /></div><b>469 ms</b>
            </div>
            <div className={styles.barRowMuted}>
              <span>xUnit 3</span><div><i style={{width: '98%'}} /></div><b>2,927 ms</b>
            </div>
            <div className={styles.barRowMuted}>
              <span>MSTest</span><div><i style={{width: '100%'}} /></div><b>2,982 ms</b>
            </div>
          </div>
          <p className={styles.benchmarkCaption}>Ubuntu 24.04 · .NET SDK 10.0.302 · Updated daily</p>
        </div>
      </div>
    </section>
  );
}

function Ecosystem(): JSX.Element {
  return (
    <section className={styles.ecosystem}>
      <div className="container">
        <div className={styles.sectionHeading}>
          <div>
            <span className={styles.kicker}>Batteries included</span>
            <h2>From unit test<br />to whole system.</h2>
          </div>
          <p>
            Start small. Scale into APIs, distributed apps, browser tests, and compile-time
            mocks without stitching together a fragile toolchain.
          </p>
        </div>
        <div className={styles.ecosystemGrid}>
          {ecosystemCards.map((item) => (
            <Link className={styles.ecosystemCard} to={item.href} key={item.title}>
              <span className={styles.ecosystemMark}>{item.mark}</span>
              <div><h3>{item.title}</h3><p>{item.copy}</p></div>
              <Arrow />
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}

function MigrationCta(): JSX.Element {
  return (
    <section className={styles.ctaSection}>
      <div className={`container ${styles.cta}`}>
        <span className={styles.kicker}>Ready when you are</span>
        <h2>Bring your tests.<br /><em>Leave the baggage.</em></h2>
        <p>Starting fresh or migrating from xUnit, NUnit, or MSTest—there’s a clear path in.</p>
        <div className={styles.ctaActions}>
          <Link className={styles.darkButton} to="/docs/getting-started/installation">Get started <span>→</span></Link>
          <Link className={styles.lightButton} to="/docs/comparison/framework-differences">Compare frameworks <Arrow /></Link>
        </div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  return (
    <Layout
      title="Fast, modern .NET testing"
      description="TUnit is a source-generated, Native AOT-ready testing framework for modern .NET. Fast feedback, compile-time safety, and batteries included.">
      <main className={styles.page}>
        <Hero />
        <InstallStrip />
        <Features />
        <Performance />
        <Ecosystem />
        <MigrationCta />
      </main>
    </Layout>
  );
}
