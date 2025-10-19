import clsx from 'clsx';
import Heading from '@theme/Heading';
import styles from './styles.module.css';

const FeatureList = [
  {
    title: 'Comprehensive Admin Tools',
    img: require('@site/static/img/index_1.png').default,
    description: (
      <>
        Full suite of admin commands for managing players, bans, mutes, warnings,
        and server settings. Everything you need to moderate your CS2 server.
      </>
    ),
  },
  {
    title: 'Multi-Server Support',
    img: require('@site/static/img/index_2.png').default,
    description: (
      <>
        Manage multiple servers with synchronized admin permissions and penalties.
        Share bans, mutes, and admin groups across your entire server network.
      </>
    ),
  },
  {
    title: 'Extensible API',
    img: require('@site/static/img/index_3.png').default,
    description: (
      <>
        Build custom modules using the public API. Create your own commands,
        menus, and integrate with CS2-SimpleAdmin's permission and penalty systems.
      </>
    ),
  },
];

function Feature({img, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <img src={img} className={styles.featureSvg} alt={title} />
      </div>
      <div className="text--center padding-horiz--md">
        <Heading as="h3">{title}</Heading>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
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
