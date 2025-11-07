import React from 'react';
import Link from '@docusaurus/Link';
import styles from './styles.module.css';

interface JourneyCard {
  title: string;
  description: string;
  icon: string;
  links: Array<{
    label: string;
    href: string;
  }>;
  color: string;
}

const journeys: JourneyCard[] = [
  {
    title: 'New to TUnit',
    description: 'Start from scratch and learn the fundamentals',
    icon: '🚀',
    color: '#14b8a6',
    links: [
      { label: 'Installation', href: '/docs/getting-started/installation' },
      { label: 'Write Your First Test', href: '/docs/getting-started/writing-your-first-test' },
      { label: 'Philosophy', href: '/docs/guides/philosophy' },
      { label: 'Core Concepts', href: '/docs/test-authoring/things-to-know' },
    ],
  },
  {
    title: 'Migrating to TUnit',
    description: 'Switch to TUnit from xUnit, NUnit, or MSTest',
    icon: '🔄',
    color: '#3b82f6',
    links: [
      { label: 'Migration Overview', href: '/docs/migration/testcontext-interface-organization' },
      { label: 'From xUnit', href: '/docs/migration/xunit' },
      { label: 'From NUnit', href: '/docs/migration/nunit' },
      { label: 'From MSTest', href: '/docs/migration/mstest' },
    ],
  },
  {
    title: 'Essential Features',
    description: 'Master the core TUnit capabilities',
    icon: '⚡',
    color: '#a855f7',
    links: [
      { label: 'Assertions Library', href: '/docs/assertions/library' },
      { label: 'Assertion Basics', href: '/docs/assertions/getting-started' },
      { label: 'Data Driven Testing', href: '/docs/test-authoring/arguments' },
      { label: 'Test Lifecycle', href: '/docs/test-lifecycle/setup' },
    ],
  },
  {
    title: 'Advanced & Optimization',
    description: 'Optimize performance and extend TUnit',
    icon: '🎯',
    color: '#f97316',
    links: [
      { label: 'Best Practices', href: '/docs/guides/best-practices' },
      { label: 'Performance Benchmarks', href: '/docs/benchmarks' },
      { label: 'Parallel Execution', href: '/docs/parallelism/not-in-parallel' },
      { label: 'Customization', href: '/docs/customization-extensibility/data-source-generators' },
    ],
  },
];

export default function ChooseYourJourney(): JSX.Element {
  return (
    <section className={styles.journeySection}>
      <div className="container">
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>Choose Your Journey</h2>
          <p className={styles.sectionSubtitle}>
            Where would you like to start?
          </p>
        </div>

        <div className={styles.journeyGrid}>
          {journeys.map((journey, idx) => (
            <div
              key={idx}
              className={styles.journeyCard}
              style={{ '--card-color': journey.color } as React.CSSProperties}
            >
              <div className={styles.cardIcon}>{journey.icon}</div>
              <h3 className={styles.cardTitle}>{journey.title}</h3>
              <p className={styles.cardDescription}>{journey.description}</p>
              <div className={styles.cardLinks}>
                {journey.links.map((link, linkIdx) => (
                  <Link
                    key={linkIdx}
                    to={link.href}
                    className={styles.cardLink}
                  >
                    {link.label} →
                  </Link>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
