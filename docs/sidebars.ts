import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

/**
 * Creating a sidebar enables you to:
 - create an ordered group of docs
 - render a sidebar for each doc of that group
 - provide next/previous navigation

 The sidebars can be generated from the filesystem, or explicitly defined here.

 Create as many sidebars as you want.
 */
const sidebars: SidebarsConfig = {
  docs: [
    {
      type: 'category',
      label: '📚 Fundamentals',
      collapsed: false,
      items: [
        {
          type: 'category',
          label: 'Getting Started',
          items: [
            'intro',
            'getting-started/installation',
            'getting-started/writing-your-first-test',
            'getting-started/running-your-tests',
            'getting-started/congratulations',
            'faq',
            'troubleshooting',
          ],
        },
        'guides/philosophy',
        {
          type: 'category',
          label: 'Test Authoring',
          items: [
            'test-authoring/things-to-know',
            {
              type: 'category',
              label: 'Data Driven Testing',
              items: [
                'test-authoring/arguments',
                'test-authoring/method-data-source',
                'test-authoring/class-data-source',
                'test-authoring/matrix-tests',
                'test-authoring/combined-data-source',
                {
                  type: 'link',
                  label: 'Data Source Generators',
                  href: '/docs/customization-extensibility/data-source-generators',
                },
                'test-authoring/nested-data-sources',
              ],
            },
            'test-authoring/skip',
            'test-authoring/explicit',
            'test-authoring/depends-on',
            'test-authoring/order',
            'test-authoring/mocking',
            'test-authoring/culture',
            'test-authoring/aot-compatibility',
            'test-authoring/generic-attributes'
          ],
        },
        {
          type: 'category',
          label: 'Assertions',
          collapsed: false,
          items: [
            'assertions/getting-started',
            'assertions/library',
            {
              type: 'category',
              label: 'Core Assertions',
              collapsed: true,
              items: [
                'assertions/equality-and-comparison',
                'assertions/null-and-default',
                'assertions/awaiting',
                'assertions/boolean',
              ],
            },
            {
              type: 'category',
              label: 'Value Assertions',
              collapsed: true,
              items: [
                'assertions/numeric',
                'assertions/string',
                'assertions/datetime',
              ],
            },
            {
              type: 'category',
              label: 'Collection Assertions',
              collapsed: true,
              items: [
                'assertions/collections',
                'assertions/dictionaries',
              ],
            },
            {
              type: 'category',
              label: 'Advanced Assertions',
              collapsed: true,
              items: [
                'assertions/exceptions',
                'assertions/types',
                'assertions/tasks-and-async',
                'assertions/specialized-types',
                'assertions/member-assertions',
              ],
            },
            {
              type: 'category',
              label: 'Assertion Composition',
              collapsed: true,
              items: [
                'assertions/and-conditions',
                'assertions/or-conditions',
                'assertions/scopes',
                'assertions/assertion-groups',
              ],
            },
            {
              type: 'category',
              label: 'Specialized',
              collapsed: true,
              items: [
                'assertions/fsharp',
              ],
            },
            {
              type: 'category',
              label: 'Extensibility',
              collapsed: true,
              items: [
                'assertions/extensibility/custom-assertions',
                'assertions/extensibility/source-generator-assertions',
                'assertions/extensibility/extensibility-chaining-and-converting',
                'assertions/extensibility/extensibility-returning-items-from-await',
              ],
            },
          ],
        },
        {
          type: 'category',
          label: 'Test Lifecycle',
          items: [
            'test-lifecycle/setup',
            'test-lifecycle/cleanup',
            'test-lifecycle/property-injection',
            'test-lifecycle/event-subscribing',
            'test-lifecycle/test-context',
            'test-lifecycle/properties',
            'test-lifecycle/class-constructors',
            'test-lifecycle/dependency-injection',
          ],
        },
        'guides/best-practices',
      ],
    },
    {
      type: 'category',
      label: '🔄 Migrating to TUnit',
      collapsed: false,
      items: [
        'migration/testcontext-interface-organization',
        'migration/xunit',
        'migration/nunit',
        'migration/mstest',
      ],
    },
    {
      type: 'category',
      label: '⚡ Advanced Capabilities',
      collapsed: false,
      items: [
        {
          type: 'category',
          label: 'Execution Control',
          items: [
            'execution/retrying',
            'execution/repeating',
            'execution/timeouts',
            'execution/test-filters',
            'execution/executors',
            'execution/engine-modes',
            'execution/ci-cd-reporting',
          ],
        },
        {
          type: 'category',
          label: 'Parallelism Control',
          items: [
            'parallelism/not-in-parallel',
            'parallelism/parallel-groups',
            'parallelism/parallel-limiter',
          ],
        },
        {
          type: 'category',
          label: 'Benchmarks',
          items: [
            'benchmarks/index',
            'benchmarks/calculator',
            'benchmarks/methodology',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '🔧 Customization',
      collapsed: false,
      items: [
        {
          type: 'category',
          label: 'Customization & Extensibility',
          items: [
            'extensions/extensions',
            'customization-extensibility/data-source-generators',
            'customization-extensibility/argument-formatters',
            'customization-extensibility/logging',
            'customization-extensibility/display-names',
            'customization-extensibility/libraries',
          ],
        },
        {
          type: 'category',
          label: 'Deep Dive',
          items: [
            'advanced/exception-handling',
            'advanced/extension-points',
            'advanced/test-variants',
            'advanced/performance-best-practices',
          ],
        },
        {
          type: 'category',
          label: 'Experimental Features',
          items: [
            'experimental/dynamic-tests',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '📖 Resources',
      collapsed: false,
      items: [
        {
          type: 'category',
          label: 'Examples & Use Cases',
          items: [
            'examples/intro',
            'examples/aspnet',
            'examples/playwright',
            'examples/complex-test-infrastructure',
            'examples/instrumenting-global-test-ids',
            'examples/tunit-ci-pipeline',
            'examples/fsharp-interactive',
            'examples/filebased-csharp'
          ],
        },
        {
          type: 'category',
          label: 'Reference',
          items: [
            'guides/cookbook',
            'comparison/framework-differences',
            'comparison/attributes',
            'reference/test-configuration',
            'reference/command-line-flags',
          ],
        },
      ],
    },
  ],
};

export default sidebars;
