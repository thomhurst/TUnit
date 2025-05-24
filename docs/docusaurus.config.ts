import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'TUnit',
  tagline: 'Welcome to TUnit Testing',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://tunit.dev',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'thomhurst', // Usually your GitHub org/user name.
  projectName: 'TUnit', // Usually your repo name.

  deploymentBranch: 'gh-pages',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

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
      {
        docs: {
          sidebarPath: './sidebars.ts',
        },
        // blog: {
        //   showReadingTime: true,
        //   // Please change this to your repo.
        //   // Remove this to remove the "edit this page" links.
        //   editUrl:
        //     'https://github.com/facebook/docusaurus/tree/main/packages/create-docusaurus/templates/shared/',
        // },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    algolia: {
      // The application ID provided by Algolia
      appId: 'RLNKZO4OTO',

      apiKey: 'a15e3e91bf0e4b10394321db84a1b658',

      indexName: 'tunit',
    },
    image: 'img/docusaurus-social-card.jpg',
    navbar: {
      title: 'TUnit',
      logo: {
        alt: 'TUnit Logo',
        src: 'img/lab.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docs',
          position: 'left',
          label: 'Tutorial',
        },
        {
          href: 'https://github.com/thomhurst/TUnit/issues',
          label: 'Issues',
          left: 'left',
        },
        {
          href: 'https://github.com/thomhurst/TUnit/discussions',
          label: 'Discussions',
          left: 'left',
        },
        // {to: '/blog', label: 'Blog', position: 'left'},
        {
          href: 'https://www.nuget.org/packages/TUnit',
          label: 'NuGet',
          position: 'right',
        },
        {
          href: 'https://github.com/thomhurst/TUnit',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Docs',
          items: [
            {
              label: 'Tutorial',
              to: '/docs/intro',
            },
          ],
        },
        {
          title: 'Community',
          items: [
            {
              label: 'Stack Overflow',
              href: 'https://stackoverflow.com/questions/tagged/tunit',
            },
            {
              label: 'Discussions',
              href: 'https://github.com/thomhurst/TUnit/discussions',
            },
          ],
        },
        {
          title: 'More',
          items: [
            // {
            //   label: 'Blog',
            //   to: '/blog',
            // },
            {
              label: 'GitHub',
              href: 'https://github.com/thomhurst/TUnit',
            },
            {
              label: 'NuGet',
              href: 'https://www.nuget.org/packages/TUnit',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} Tom Longhurst. Built with Docusaurus.`,
    },
    prism: {
      additionalLanguages: ['csharp', 'powershell', 'fsharp'],
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
