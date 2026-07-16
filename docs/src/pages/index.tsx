import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';
import BenchmarkHighlight from '@site/src/components/BenchmarkHighlight';
import ChooseYourJourney from '@site/src/components/ChooseYourJourney';
import Heading from '@theme/Heading';
import CodeBlock from '@theme/CodeBlock';

import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();

  const codeExample = `[Test]
public async Task MyTest()
{
    // Arrange
    var calculator = new Calculator();

    // Act
    var result = calculator.Add(2, 3);

    // Assert
    await Assert.That(result).IsEqualTo(5);
}`;

  return (
    <header className={styles.heroBanner}>
      <div className={styles.heroBackground}>
        <div className={styles.codePattern}></div>
        <div className={styles.gradientOverlay}></div>
      </div>
      <div className="container">
        <div className={styles.heroContent}>
          <div className={styles.heroText}>
            <div className={styles.titleRow}>
              <Heading as="h1" className={styles.heroTitle}>
                <span className={styles.titleMain}>TUNIT</span>
                <span className={styles.titleAccent}>Testing Made Simple</span>
              </Heading>
              <div className={styles.badge}>Modern .NET Testing</div>
            </div>
            <p className={styles.heroSubtitle}>
              A modern .NET testing framework where tests are discovered at compile time, not
              reflected at runtime. Source-generated and Native AOT ready, parallel by default,
              batteries included — assertions, mocking, and first-class ASP.NET Core, Aspire,
              and Playwright integrations.
            </p>
            <div className={styles.heroStats}>
              <div className={styles.stat}>
                <span className={styles.statNumber}>Source</span>
                <span className={styles.statLabel}>Generated</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statNumber}>AOT</span>
                <span className={styles.statLabel}>Compatible</span>
              </div>
              <div className={styles.stat}>
                <span className={styles.statNumber}>Parallel</span>
                <span className={styles.statLabel}>Execution</span>
              </div>
            </div>
            <div className={styles.heroButtons}>
              <Link
                className={styles.primaryButton}
                to="/docs/getting-started/installation"
                aria-label="Get started with TUnit installation guide">
                Get Started
                <span className={styles.buttonIcon} aria-hidden="true">→</span>
              </Link>
              <Link
                className={styles.secondaryButton}
                to="/docs/intro"
                aria-label="View TUnit tutorial and documentation">
                View Tutorial
                <span className={styles.buttonIcon} aria-hidden="true">📚</span>
              </Link>
              <Link
                className={styles.secondaryButton}
                href="https://github.com/sponsors/thomhurst"
                aria-label="Sponsor TUnit development">
                ❤️ Sponsor
              </Link>
            </div>
          </div>
          <div className={styles.heroCode}>
            <div className={styles.codeWindow}>
              <div className={styles.codeHeader}>
                <div className={styles.codeHeaderDots}>
                  <span></span>
                  <span></span>
                  <span></span>
                </div>
                <span className={styles.codeFileName}>CalculatorTests.cs</span>
              </div>
              <CodeBlock language="csharp" className={styles.codeContent}>
                {codeExample}
              </CodeBlock>
            </div>
            <div className={styles.testOutput} role="region" aria-label="Test execution results">
              <div className={styles.testOutputHeader}>
                <span className={styles.testOutputIcon} aria-hidden="true">✓</span>
                Test Results
              </div>
              <div className={styles.testOutputContent}>
                <div className={styles.testSuccess}>
                  <span aria-label="Test passed">✓</span> MyTest <span className={styles.testTime}>2ms</span>
                </div>
                <div className={styles.testSummary}>
                  1 test passed • 0 failed • 2ms total
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </header>
  );
}

function QuickStartSection() {
  return (
    <section className={styles.quickStart}>
      <div className={styles.quickStartContainer}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>Quick Start</h2>
          <p className={styles.sectionSubtitle}>Get up and running in seconds</p>
        </div>
        <div className={styles.quickStartSteps}>
          <div className={styles.step}>
            <div className={styles.stepNumber}>1</div>
            <div className={styles.stepContent}>
              <h3>Install</h3>
              <CodeBlock language="bash">
                dotnet add package TUnit
              </CodeBlock>
            </div>
          </div>
          <div className={styles.step}>
            <div className={styles.stepNumber}>2</div>
            <div className={styles.stepContent}>
              <h3>Write</h3>
              <CodeBlock language="csharp">
                {`[Test]
public async Task TestAddition()
{
    await Assert.That(1 + 1).IsEqualTo(2);
}`}
              </CodeBlock>
            </div>
          </div>
          <div className={styles.step}>
            <div className={styles.stepNumber}>3</div>
            <div className={styles.stepContent}>
              <h3>Run</h3>
              <CodeBlock language="bash">
                dotnet test
              </CodeBlock>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function WhyTUnit() {
  const features = [
    {
      icon: '⚡',
      title: 'Compile-Time Discovery',
      description: 'Tests are wired up by a source generator at build time, not found via reflection at runtime — faster startup, better IDE integration, and full Native AOT / trimming support.',
    },
    {
      icon: '🎯',
      title: 'Compile-Time Safety',
      description: 'A suite of Roslyn analyzers ships in the box, so invalid hook signatures, broken data sources, and misused assertions fail your build — not your CI run.',
    },
    {
      icon: '🔀',
      title: 'Parallel by Default',
      description: 'Tests run concurrently out of the box; [DependsOn], [NotInParallel], and [ParallelLimiter<T>] give you precise ordering and throttling when you need it.',
    },
    {
      icon: '🧩',
      title: 'Batteries Included',
      description: 'Rich async assertions, shared fixtures with dependency injection, and lifecycle hooks at every scope — plus a source-generated mocking library.',
    },
    {
      icon: '🔌',
      title: 'First-Class Integrations',
      description: 'Purpose-built support for ASP.NET Core, Aspire, and Playwright, so integration and end-to-end tests feel native to the framework.',
    },
    {
      icon: '🚀',
      title: 'AOT & Trimming Ready',
      description: 'Native AOT support enables dramatically faster startup and reduced memory usage, with no runtime reflection to trim away.',
    },
  ];

  return (
    <section className={styles.whySection}>
      <div className={styles.whyContainer}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>Why TUnit?</h2>
          <p className={styles.sectionSubtitle}>Built for modern .NET development</p>
        </div>
        <div className={styles.featureGrid}>
          {features.map((feature, idx) => (
            <div key={idx} className={styles.featureCard}>
              <div className={styles.featureIcon}>{feature.icon}</div>
              <h3>{feature.title}</h3>
              <p>{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}

export default function Home(): JSX.Element {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title} - Modern .NET Testing Framework`}
      description="TUnit - A powerful, flexible, and blazing-fast testing framework for modern .NET">
      <HomepageHeader />
      <main>
        <QuickStartSection />
        <ChooseYourJourney />
        <BenchmarkHighlight />
        <HomepageFeatures />
        <WhyTUnit />
      </main>
    </Layout>
  );
}
