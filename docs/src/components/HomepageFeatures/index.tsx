import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

type FeatureItem = {
  title: string;
  Svg: React.ComponentType<React.ComponentProps<'svg'>>;
  description: JSX.Element;
};

const FeatureList: FeatureItem[] = [
  {
    title: 'Flexible',
    Svg: require('@site/static/img/flexible.svg').default,
    description: (
      <>
        TUnit was designed for flexibility. With various ways to write, inject data into, and control tests.
      </>
    ),
  },
  {
    title: 'Easy',
    Svg: require('@site/static/img/easy.svg').default,
    description: (
      <>
        TUnit uses an attribute-based syntax that is easy to read and write. Simply create methods and add attributes to them to control how your test suite works.
      </>
    ),
  },
  {
    title: 'Fast',
    Svg: require('@site/static/img/fast.svg').default,
    description: (
      <>
        TUnit is built on top of the new Microsoft Testing Platform. This means that it is fast and efficient.
      </>
    ),
  },
  {
    title: 'Source Generated',
    Svg: require('@site/static/img/fast.svg').default,
    description: (
      <>
        TUnit Source-Generates your tests. This makes your tests fast, and it enables you to create your test applications using Native AOT or as Single File applications.
      </>
    ),
  },
];

function Feature({title, Svg, description}: FeatureItem) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <Svg className={styles.featureSvg} role="img" />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures(): JSX.Element {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props} />
          ))}
        </div>
      </div>
    </section>
  );
}
