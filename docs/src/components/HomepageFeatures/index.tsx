import clsx from 'clsx';
import Heading from '@theme/Heading';
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
    codeExample: '[Test]\n[Arguments(1, 2, 3)]\n[Arguments(4, 5, 9)]\npublic void TestAdd(int a, int b, int expected)\n{\n    Assert.That(a + b).IsEqualTo(expected);\n}'
  },
  {
    title: 'Intuitive Syntax',
    icon: '✨',
    description: (
      <>
        Clean attribute-based syntax that's easy to read and write. Fluent assertions make tests expressive and self-documenting.
      </>
    ),
    codeExample: '[Test]\npublic async Task TestAsync()\n{\n    var result = await GetDataAsync();\n    await Assert.That(result)\n        .IsNotNull()\n        .And.HasCount(5);\n}'
  },
  {
    title: 'Performance Optimized',
    icon: '⚡',
    description: (
      <>
        Source generated tests with Native AOT support. Built on Microsoft Testing Platform to reduce overhead and improve efficiency.
      </>
    ),
    codeExample: '// AOT Compatible\n[Test]\npublic void PerformantTest()\n{\n    // Source generated\n    // No reflection overhead\n    Assert.That(true).IsTrue();\n}'
  },
];

function Feature({title, icon, description, codeExample}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className={styles.featureBox}>
        <div className={styles.featureHeader}>
          <span className={styles.featureIcon}>{icon}</span>
          <Heading as="h3" className={styles.featureTitle}>{title}</Heading>
        </div>
        <p className={styles.featureDescription}>{description}</p>
        {codeExample && (
          <div className={styles.featureCode}>
            <pre>
              <code>{codeExample}</code>
            </pre>
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
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
