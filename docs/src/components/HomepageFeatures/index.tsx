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
        Multiple ways to write, inject data, and control tests. Support for data-driven testing, matrix tests, and custom data sources.
      </>
    ),
    codeExample: `[Test]
[Arguments(1, 2, 3)]
[Arguments(4, 5, 9)]
public void TestAdd(int a, int b, int expected)
{
    Assert.That(a + b).IsEqualTo(expected);
}`
  },
  {
    title: 'Intuitive Syntax',
    icon: '✨',
    description: (
      <>
        Clean attribute-based syntax that's easy to read and write. Fluent assertions make tests expressive and self-documenting.
      </>
    ),
    codeExample: `[Test]
public async Task TestAsync()
{
    var result = await GetDataAsync();
    await Assert.That(result)
        .IsNotNull()
        .And.HasCount(5);
}`
  },
  {
    title: 'Performance Optimized',
    icon: '⚡',
    description: (
      <>
        Source generated tests with Native AOT support. Built on Microsoft Testing Platform to reduce overhead and improve efficiency.
      </>
    ),
    codeExample: `// AOT Compatible
[Test]
public void PerformantTest()
{
    // Source generated
    // No reflection overhead
    Assert.That(true).IsTrue();
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
            <CodeBlock language="csharp">
              {codeExample}
            </CodeBlock>
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
