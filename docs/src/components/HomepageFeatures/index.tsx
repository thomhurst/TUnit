import clsx from 'clsx';
import Heading from '@theme/Heading';
import CodeBlock from '@theme/CodeBlock';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  icon: string;
  description: JSX.Element;
  codeExample?: string;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Flexible Test Design',
    icon: '🔧',
    description: (
      <>
        Data-driven arguments, matrix tests, and custom data sources — plus injectable, shared fixtures with dependency injection and reference-counted disposal.
      </>
    ),
    codeExample: `[Test]
[Arguments(1, 2, 3)]
[Arguments(4, 5, 9)]
public async Task TestAdd(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}`
  },
  {
    title: 'Intuitive Syntax',
    icon: '✨',
    description: (
      <>
        Clean, attribute-based tests with fluent async assertions that read like sentences — and pinpoint the exact difference when they fail, instead of dumping object graphs at you.
      </>
    ),
    codeExample: `[Test]
public async Task TestAsync()
{
    var result = await GetDataAsync();
    await Assert.That(result)
        .IsNotNull()
        .And.Count().IsEqualTo(5);
}`
  },
  {
    title: 'Performance Optimized',
    icon: '⚡',
    description: (
      <>
        Source-generated tests with Native AOT support, built on the Microsoft Testing Platform. Work shifts from run time to build time, so every run after starts faster.
      </>
    ),
    codeExample: `// AOT Compatible
[Test]
public async Task PerformantTest()
{
    // Source generated
    // No reflection overhead
    await Assert.That(true).IsTrue();
}`
  },
];

function Feature({title, icon, description, codeExample}: FeatureItem) {
  return (
    <div className={styles.featureColumn}>
      <div className={styles.featureBox}>
        <div className={styles.featureHeader}>
          <span className={styles.featureIcon}>{icon}</span>
          <Heading as="h3" className={styles.featureTitle}>{title}</Heading>
        </div>
        <p className={styles.featureDescription}>{description}</p>
        {codeExample && (
          <div className={styles.featureCode}>
            <div className={styles.codeScrollWrapper}>
              <CodeBlock language="csharp" className={styles.codeBlock}>
                {codeExample}
              </CodeBlock>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export default function HomepageFeatures(): JSX.Element {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className={styles.featuresHeader}>
          <h2 className={styles.featuresTitle}>Core Features</h2>
          <p className={styles.featuresSubtitle}>Everything you need for modern test development</p>
        </div>
        <div className={styles.featureRow}>
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
