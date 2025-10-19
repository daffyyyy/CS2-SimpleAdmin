// @ts-check
// `@type` JSDoc annotations allow editor autocompletion and type checking
// (when paired with `@ts-check`).
// There are various equivalent ways to declare your Docusaurus config.
// See: https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'CS2-SimpleAdmin',
  tagline: 'Comprehensive administration plugin for Counter-Strike 2 servers',
  favicon: 'img/favicon.ico',

  // Future flags, see https://docusaurus.io/docs/api/docusaurus-config#future
  future: {
    v4: true, // Improve compatibility with the upcoming Docusaurus v4
  },

  // Set the production url of your site here
  url: 'https://cs2-simpleadmin.daffyy.dev',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'daffyyyy', // Usually your GitHub org/user name.
  projectName: 'CS2-SimpleAdmin', // Usually your repo name.

  onBrokenLinks: 'throw',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          // editUrl:
          //   '',
        },
        blog: false,  // Disable blog
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      // Replace with your project's social card
      image: 'img/docusaurus-social-card.jpg',
      metadata: [
        {name: 'keywords', content: 'CS2, Counter-Strike 2, admin plugin, server management, bans, mutes, CounterStrikeSharp'},
        {name: 'description', content: 'Comprehensive administration plugin for Counter-Strike 2 servers with ban management, multi-server support, and extensible API'},
        {name: 'author', content: 'daffyyyy'},
        {property: 'og:title', content: 'CS2-SimpleAdmin - Admin Plugin for Counter-Strike 2'},
        {property: 'og:description', content: 'Comprehensive administration plugin for CS2 servers. Manage bans, mutes, warnings, and permissions with multi-server support.'},
        {property: 'og:type', content: 'website'},
        {property: 'og:url', content: 'https://cs2-simpleadmin.daffyy.dev'},
        {property: 'og:image', content: 'https://cs2-simpleadmin.daffyy.dev/img/docusaurus-social-card.jpg'},
        {name: 'twitter:card', content: 'summary_large_image'},
        {name: 'twitter:title', content: 'CS2-SimpleAdmin - Admin Plugin for Counter-Strike 2'},
        {name: 'twitter:description', content: 'Comprehensive administration plugin for CS2 servers with ban management and multi-server support.'},
        {name: 'twitter:image', content: 'https://cs2-simpleadmin.daffyy.dev/img/docusaurus-social-card.jpg'},
      ],
      colorMode: {
        respectPrefersColorScheme: true,
      },
      navbar: {
        title: 'CS2-SimpleAdmin',
        logo: {
          alt: 'CS2-SimpleAdmin Logo',
          src: 'img/logo.svg',
        },
        items: [
          {
            type: 'docSidebar',
            sidebarId: 'userSidebar',
            position: 'left',
            label: 'User Guide',
          },
          {
            type: 'docSidebar',
            sidebarId: 'modulesSidebar',
            position: 'left',
            label: 'Modules',
          },
          {
            type: 'docSidebar',
            sidebarId: 'developerSidebar',
            position: 'left',
            label: 'Developer',
          },
          {
            href: 'https://github.com/daffyyyy/CS2-SimpleAdmin',
            label: 'GitHub',
            position: 'right',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Documentation',
            items: [
              {
                label: 'User Guide',
                to: '/docs/user/intro',
              },
              {
                label: 'Modules',
                to: '/docs/modules/intro',
              },
              {
                label: 'Developer',
                to: '/docs/developer/intro',
              },
            ],
          },
          {
            title: 'Community',
            items: [
              {
                label: 'GitHub Issues',
                href: 'https://github.com/daffyyyy/CS2-SimpleAdmin/issues',
              },
              {
                label: 'GitHub Discussions',
                href: 'https://github.com/daffyyyy/CS2-SimpleAdmin/discussions',
              },
            ],
          },
          {
            title: 'More',
            items: [
              {
                label: 'GitHub',
                href: 'https://github.com/daffyyyy/CS2-SimpleAdmin',
              },
              {
                label: 'Releases',
                href: 'https://github.com/daffyyyy/CS2-SimpleAdmin/releases',
              },
            ],
          },
        ],
        copyright: `Copyright © ${new Date().getFullYear()} CS2-SimpleAdmin. Built with Docusaurus.`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
        additionalLanguages: ['csharp', 'json', 'bash'],
      },
    }),
};

export default config;
