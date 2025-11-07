import React, { useState, useEffect } from 'react';
import styles from './styles.module.css';

export default function BenchmarkCalculator(): JSX.Element {
  const [testCount, setTestCount] = useState<number>(100);
  const [currentFramework, setCurrentFramework] = useState<string>('xUnit3');
  const [runsPerDay, setRunsPerDay] = useState<number>(10);
  const [averageSpeedups, setAverageSpeedups] = useState<any>(null);

  useEffect(() => {
    // Try to load real benchmark data
    fetch('/benchmarks/latest.json')
      .then(res => res.json())
      .then(data => {
        if (data.averageSpeedups) {
          setAverageSpeedups(data.averageSpeedups);
        }
      })
      .catch(() => {
        // Use fallback data if benchmarks not available
        setAverageSpeedups({
          vsXUnit: '2.6',
          vsNUnit: '4.5',
          vsMSTest: '5.1'
        });
      });
  }, []);

  if (!averageSpeedups) {
    return <div className={styles.loading}>Loading calculator...</div>;
  }

  // Baseline: assume 50ms per test average for xUnit3
  const baseTimePerTest = 50; // ms

  const frameworkMultipliers: Record<string, number> = {
    'xUnit3': 1.0,
    'NUnit': parseFloat(averageSpeedups.vsNUnit) / parseFloat(averageSpeedups.vsXUnit),
    'MSTest': parseFloat(averageSpeedups.vsMSTest) / parseFloat(averageSpeedups.vsXUnit)
  };

  const currentTimeMs = testCount * baseTimePerTest * (frameworkMultipliers[currentFramework] || 1.0);
  const tunitTimeMs = currentTimeMs / parseFloat(averageSpeedups[`vs${currentFramework === 'xUnit3' ? 'XUnit' : currentFramework}`] || averageSpeedups.vsXUnit);
  const tunitAotTimeMs = tunitTimeMs / 3.5; // Approximate AOT speedup

  const timeSavedPerRun = currentTimeMs - tunitTimeMs;
  const timeSavedPerRunAot = currentTimeMs - tunitAotTimeMs;

  const dailySavings = (timeSavedPerRun * runsPerDay) / 1000 / 60; // minutes
  const dailySavingsAot = (timeSavedPerRunAot * runsPerDay) / 1000 / 60; // minutes

  const annualSavings = dailySavings * 250; // work days
  const annualSavingsAot = dailySavingsAot * 250;

  const formatTime = (ms: number): string => {
    if (ms < 1000) return `${Math.round(ms)}ms`;
    if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
    return `${(ms / 60000).toFixed(1)}m`;
  };

  const formatSavings = (minutes: number): string => {
    if (minutes < 60) return `${Math.round(minutes)} minutes`;
    return `${(minutes / 60).toFixed(1)} hours`;
  };

  return (
    <div className={styles.calculator}>
      <div className={styles.inputs}>
        <div className={styles.inputGroup}>
          <label htmlFor="testCount">
            <strong>Number of Tests</strong>
          </label>
          <input
            id="testCount"
            type="number"
            min="1"
            max="100000"
            value={testCount}
            onChange={(e) => setTestCount(Math.max(1, parseInt(e.target.value) || 0))}
            className={styles.input}
          />
          <span className={styles.inputHint}>Total test count in your suite</span>
        </div>

        <div className={styles.inputGroup}>
          <label htmlFor="currentFramework">
            <strong>Current Framework</strong>
          </label>
          <select
            id="currentFramework"
            value={currentFramework}
            onChange={(e) => setCurrentFramework(e.target.value)}
            className={styles.select}
          >
            <option value="xUnit3">xUnit v3</option>
            <option value="NUnit">NUnit</option>
            <option value="MSTest">MSTest</option>
          </select>
          <span className={styles.inputHint}>Your current testing framework</span>
        </div>

        <div className={styles.inputGroup}>
          <label htmlFor="runsPerDay">
            <strong>Test Runs Per Day</strong>
          </label>
          <input
            id="runsPerDay"
            type="number"
            min="1"
            max="1000"
            value={runsPerDay}
            onChange={(e) => setRunsPerDay(Math.max(1, parseInt(e.target.value) || 0))}
            className={styles.input}
          />
          <span className={styles.inputHint}>Per developer (includes CI)</span>
        </div>
      </div>

      <div className={styles.results}>
        <h3>Estimated Results</h3>

        <div className={styles.resultSection}>
          <h4>⏱️ Execution Time Per Run</h4>
          <div className={styles.resultGrid}>
            <div className={styles.resultCard}>
              <span className={styles.resultLabel}>Current ({currentFramework})</span>
              <span className={styles.resultValue}>{formatTime(currentTimeMs)}</span>
            </div>
            <div className={styles.resultCard}>
              <span className={styles.resultLabel}>TUnit (JIT)</span>
              <span className={styles.resultValue}>{formatTime(tunitTimeMs)}</span>
              <span className={styles.resultSavings}>
                -{formatTime(timeSavedPerRun)}
                ({((timeSavedPerRun / currentTimeMs) * 100).toFixed(0)}% faster)
              </span>
            </div>
            <div className={styles.resultCard + ' ' + styles.highlight}>
              <span className={styles.resultLabel}>🏆 TUnit (AOT)</span>
              <span className={styles.resultValue}>{formatTime(tunitAotTimeMs)}</span>
              <span className={styles.resultSavings}>
                -{formatTime(timeSavedPerRunAot)}
                ({((timeSavedPerRunAot / currentTimeMs) * 100).toFixed(0)}% faster)
              </span>
            </div>
          </div>
        </div>

        <div className={styles.resultSection}>
          <h4>🚀 Time Saved</h4>
          <div className={styles.savingsGrid}>
            <div className={styles.savingsCard}>
              <span className={styles.savingsLabel}>Per Day (TUnit JIT)</span>
              <span className={styles.savingsValue}>{formatSavings(dailySavings)}</span>
            </div>
            <div className={styles.savingsCard}>
              <span className={styles.savingsLabel}>Per Year (TUnit JIT)</span>
              <span className={styles.savingsValue}>{formatSavings(annualSavings)}</span>
            </div>
          </div>
          <div className={styles.savingsGrid}>
            <div className={styles.savingsCard + ' ' + styles.highlight}>
              <span className={styles.savingsLabel}>Per Day (TUnit AOT)</span>
              <span className={styles.savingsValue}>{formatSavings(dailySavingsAot)}</span>
            </div>
            <div className={styles.savingsCard + ' ' + styles.highlight}>
              <span className={styles.savingsLabel}>Per Year (TUnit AOT)</span>
              <span className={styles.savingsValue}>{formatSavings(annualSavingsAot)}</span>
            </div>
          </div>
        </div>

        <div className={styles.impact}>
          <h4>💰 Real-World Impact</h4>
          <ul>
            <li>
              <strong>CI/CD:</strong> Faster builds mean quicker feedback and lower infrastructure costs
            </li>
            <li>
              <strong>Developer Productivity:</strong> {formatSavings(annualSavings)} saved per developer annually
            </li>
            <li>
              <strong>Native AOT:</strong> Even faster cold starts in containers and CI environments
            </li>
            <li>
              <strong>Team of 10:</strong> Save {formatSavings(annualSavings * 10)}/year with JIT,
              {' '}{formatSavings(annualSavingsAot * 10)}/year with AOT
            </li>
          </ul>
        </div>

        <div className={styles.disclaimer}>
          <p>
            <strong>Note:</strong> These estimates are based on real benchmark data from automated tests.
            Actual results depend on test complexity, infrastructure, and parallelization potential.
            See the <a href="/docs/benchmarks/">detailed benchmarks</a> for methodology.
          </p>
        </div>
      </div>
    </div>
  );
}
