import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import Heading from '@theme/Heading';
import styles from './index.module.css';

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <iframe
        className={styles.videoBackground}
        src="https://www.youtube.com/embed/4qEdIXLdxMo?autoplay=1&mute=1&loop=1&playlist=4qEdIXLdxMo&controls=0&showinfo=0&rel=0&modestbranding=1&playsinline=1"
        title="Background Video"
        frameBorder="0"
        allow="autoplay; encrypted-media"
        allowFullScreen
      />
      <div className={styles.videoOverlay}></div>
      <div className="container">
        <Heading as="h1" className="hero__title">
          {siteConfig.title}
        </Heading>
        <p className="hero__subtitle">{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/user/intro">
            Get Started
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title} - Admin Plugin for CS2`}
      description="CS2-SimpleAdmin is a comprehensive administration plugin for Counter-Strike 2 servers. Manage bans, mutes, warnings, and permissions with multi-server support and extensible API.">
      <HomepageHeader />
      <main>
        <HomepageFeatures />
      </main>
    </Layout>
  );
}
