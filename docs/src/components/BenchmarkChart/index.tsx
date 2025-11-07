import React, { useEffect, useState } from 'react';
import styles from './styles.module.css';

interface BenchmarkData {
  Method: string;
  Version: string;
  Mean: string;
  Median: string;
  StdDev?: string;
}

interface BenchmarkChartProps {
  category?: string;
}

export default function BenchmarkChart({ category = 'AsyncTests' }: BenchmarkChartProps): JSX.Element {
  const [data, setData] = useState<BenchmarkData[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/benchmarks/latest.json')
      .then(res => {
        if (!res.ok) throw new Error('Failed to load benchmark data');
        return res.json();
      })
      .then(jsonData => {
        const categoryData = jsonData.categories?.[category];
        if (categoryData) {
          setData(categoryData);
        } else {
          setError(`No data available for category: ${category}`);
        }
        setLoading(false);
      })
      .catch(err => {
        console.error('Error loading benchmark data:', err);
        setError('Benchmark data not yet available. Run the Speed Comparison workflow to generate data.');
        setLoading(false);
      });
  }, [category]);

  if (loading) {
    return (
      <div className={styles.chartContainer}>
        <div className={styles.loading}>Loading benchmark data...</div>
      </div>
    );
  }

  if (error || !data) {
    return (
      <div className={styles.chartContainer}>
        <div className={styles.error}>
          {error || 'No benchmark data available'}
          <p className={styles.errorHint}>
            Benchmarks are generated daily. Check back soon or run the workflow manually.
          </p>
        </div>
      </div>
    );
  }

  const parseMean = (meanStr: string): number => {
    const match = meanStr.match(/[\d.]+/);
    return match ? parseFloat(match[0]) : 0;
  };

  const getUnit = (meanStr: string): string => {
    return meanStr.includes('ms') ? 'ms' : 's';
  };

  const sortedData = [...data].sort((a, b) => parseMean(a.Mean) - parseMean(b.Mean));
  const maxValue = Math.max(...sortedData.map(d => parseMean(d.Mean)));
  const unit = getUnit(sortedData[0]?.Mean || 'ms');

  const getBarColor = (method: string): string => {
    if (method.includes('TUnit_AOT')) return 'var(--ifm-color-primary)';
    if (method.includes('TUnit')) return 'var(--ifm-color-primary-light)';
    return 'var(--ifm-color-emphasis-400)';
  };

  const getTextColor = (method: string): string => {
    return method.includes('TUnit') ? 'var(--ifm-color-primary)' : 'var(--text-body)';
  };

  return (
    <div className={styles.chartContainer}>
      <div className={styles.chartHeader}>
        <h3>{category} Performance</h3>
        <p>Lower is better • Measured in {unit}</p>
      </div>
      <div className={styles.chart}>
        {sortedData.map((row, index) => {
          const value = parseMean(row.Mean);
          const percentage = (value / maxValue) * 100;
          const isTUnit = row.Method.includes('TUnit');

          return (
            <div key={index} className={styles.barRow}>
              <div className={styles.barLabel}>
                <span className={styles.barMethod} style={{ color: getTextColor(row.Method) }}>
                  {isTUnit && '🏆 '}
                  {row.Method === 'TUnit_AOT' ? 'TUnit (AOT)' : row.Method}
                </span>
                <span className={styles.barVersion}>{row.Version}</span>
              </div>
              <div className={styles.barContainer}>
                <div
                  className={styles.bar}
                  style={{
                    width: `${percentage}%`,
                    backgroundColor: getBarColor(row.Method)
                  }}
                >
                  <span className={styles.barValue}>{row.Mean}</span>
                </div>
              </div>
            </div>
          );
        })}
      </div>
      <div className={styles.chartFooter}>
        <p>
          Data from latest automated benchmark run.
          See <a href="/docs/benchmarks/methodology">methodology</a> for details.
        </p>
      </div>
    </div>
  );
}
