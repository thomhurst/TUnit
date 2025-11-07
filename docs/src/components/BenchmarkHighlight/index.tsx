import React, { useEffect, useState } from 'react';
import Link from '@docusaurus/Link';
import styles from './styles.module.css';

export default function BenchmarkHighlight(): JSX.Element {
  const [speedups, setSpeedups] = useState<any>(null);

  useEffect(() => {
    fetch('/benchmarks/latest.json')
      .then(res => res.json())
      .then(data => {
        if (data.averageSpeedups) {
          setSpeedups(data.averageSpeedups);
        }
      })
      .catch(() => {
        // Use fallback data if benchmarks not available yet
        setSpeedups({
          vsXUnit: '2.6',
          vsNUnit: '4.5',
          vsMSTest: '5.1'
        });
      });
  }, []);

  if (!speedups) {
    return null; // Don't show section until data loads
  }

  return (
    <section className={styles.benchmarkSection}>
      <div className="container">
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>Proven Performance</h2>
          <p className={styles.sectionSubtitle}>
            Real-world benchmarks from daily automated tests
          </p>
        </div>

        <div className={styles.benchmarkGrid}>
          <div className={styles.benchmarkCard}>
            <div className={styles.cardIcon}>⚡</div>
            <div className={styles.cardContent}>
              <span className={styles.benchmarkNumber}>{speedups.vsXUnit}x</span>
              <span className={styles.benchmarkLabel}>Faster than xUnit</span>
            </div>
          </div>

          <div className={styles.benchmarkCard}>
            <div className={styles.cardIcon}>🚀</div>
            <div className={styles.cardContent}>
              <span className={styles.benchmarkNumber}>{speedups.vsNUnit}x</span>
              <span className={styles.benchmarkLabel}>Faster than NUnit</span>
            </div>
          </div>

          <div className={styles.benchmarkCard}>
            <div className={styles.cardIcon}>⚡</div>
            <div className={styles.cardContent}>
              <span className={styles.benchmarkNumber}>{speedups.vsMSTest}x</span>
              <span className={styles.benchmarkLabel}>Faster than MSTest</span>
            </div>
          </div>

          <div className={`${styles.benchmarkCard} ${styles.highlightCard}`}>
            <div className={styles.cardIcon}>🏆</div>
            <div className={styles.cardContent}>
              <span className={styles.benchmarkNumber}>10x</span>
              <span className={styles.benchmarkLabel}>Faster with Native AOT</span>
            </div>
          </div>
        </div>

        <div className={styles.benchmarkFeatures}>
          <div className={styles.feature}>
            <span className={styles.featureIcon}>✓</span>
            <span>Source-generated tests eliminate reflection overhead</span>
          </div>
          <div className={styles.feature}>
            <span className={styles.featureIcon}>✓</span>
            <span>Parallel execution by default</span>
          </div>
          <div className={styles.feature}>
            <span className={styles.featureIcon}>✓</span>
            <span>Native AOT compilation for maximum performance</span>
          </div>
        </div>

        <div className={styles.benchmarkActions}>
          <Link to="/docs/benchmarks" className={styles.primaryLink}>
            View Detailed Benchmarks →
          </Link>
          <Link to="/docs/benchmarks/calculator" className={styles.secondaryLink}>
            Calculate Your Savings
          </Link>
        </div>

        <div className={styles.benchmarkNote}>
          <p>
            Benchmarks automatically updated daily from real-world test scenarios.
            <br />
            <Link to="/docs/benchmarks/methodology">See the methodology</Link>
          </p>
        </div>
      </div>
    </section>
  );
}
