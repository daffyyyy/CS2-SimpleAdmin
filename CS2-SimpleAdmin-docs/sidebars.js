// @ts-check

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.

 @type {import('@docusaurus/plugin-content-docs').SidebarsConfig}
 */
const sidebars = {
  userSidebar: [
    'user/intro',
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'user/installation',
        'user/configuration',
      ],
    },
    {
      type: 'category',
      label: 'Commands',
      items: [
        'user/commands/basebans',
        'user/commands/basecomms',
        'user/commands/basecommands',
        'user/commands/basechat',
        'user/commands/playercommands',
        'user/commands/basevotes',
      ],
    },
  ],

  modulesSidebar: [
    'modules/intro',
    {
      type: 'category',
      label: 'Official Modules',
      items: [
        'modules/funcommands',
      ],
    },
    'modules/development',
  ],

  developerSidebar: [
    'developer/intro',
    {
      type: 'category',
      label: 'API Reference',
      items: [
        'developer/api/overview',
        'developer/api/commands',
        'developer/api/menus',
        'developer/api/penalties',
        'developer/api/events',
        'developer/api/utilities',
      ],
    },
    {
      type: 'category',
      label: 'Module Development',
      items: [
        'developer/module/getting-started',
        'developer/module/best-practices',
        'developer/module/examples',
      ],
    },
    'developer/architecture',
  ],
};

export default sidebars;
