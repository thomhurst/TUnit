import type { SidebarsConfig } from '@docusaurus/plugin-content-docs';

/**
 * Creating a sidebar enables you to:
 * - create an ordered group of docs
 * - render a sidebar for each doc of that group
 * - provide next/previous navigation
 *
 * The sidebars can be generated from the filesystem, or explicitly defined here.
 *
 * Create as many sidebars as you want.
 */
const sidebars: SidebarsConfig = {
  docs: [
    {
      type: 'category',
      label: '🚀 Getting Started',
      collapsed: false,
      items: [
        'intro',
        'getting-started/installation',
        'getting-started/writing-your-first-test',
        'getting-started/running-your-tests',
      ],
    },
    {
      type: 'category',
      label: '🔄 Migration Guides',
      collapsed: true,
      items: [
        'comparison/framework-differences',
        'migration/xunit',
        'migration/nunit',
        'migration/mstest',
        'migration/testcontext-interface-organization',
      ],
    },
    {
      type: 'category',
      label: '✍️ Test Authoring',
      collapsed: true,
      items: [
        'test-authoring/things-to-know',
        {
          type: 'category',
          label: 'Core Concepts',
          collapsed: false,
          items: [
            'test-lifecycle/setup',
            'test-lifecycle/cleanup',
            'test-lifecycle/test-context',
            'test-lifecycle/artifacts',
            'test-lifecycle/properties',
            'test-lifecycle/property-injection',
            'test-lifecycle/event-subscribing',
            'test-lifecycle/class-constructors',
            'test-lifecycle/dependency-injection',
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
              label: 'Core & Value Assertions',
              collapsed: false,
              items: [
                'assertions/equality-and-comparison',
                'assertions/null-and-default',
                'assertions/boolean',
                'assertions/numeric',
                'assertions/string',
                'assertions/datetime',
                'assertions/types',
                'assertions/specialized-types',
              ],
            },
            {
              type: 'category',
              label: 'Collection Assertions',
              collapsed: false,
              items: [
                'assertions/collections',
                'assertions/dictionaries',
              ],
            },
            {
              type: 'category',
              label: 'Async & Exception Assertions',
              collapsed: false,
              items: [
                'assertions/awaiting',
                'assertions/tasks-and-async',
                'assertions/exceptions',
              ],
            },
            {
              type: 'category',
              label: 'Advanced & Composition',
              collapsed: false,
              items: [
                'assertions/member-assertions',
                'assertions/and-conditions',
                'assertions/or-conditions',
                'assertions/scopes',
                'assertions/assertion-groups',
              ],
            },
            {
              type: 'category',
              label: 'Extending Assertions',
              collapsed: false,
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
          label: 'Data-Driven Testing',
          collapsed: false,
          items: [
            'test-authoring/arguments',
            'test-authoring/method-data-source',
            'test-authoring/class-data-source',
            'test-authoring/matrix-tests',
            'test-authoring/combined-data-source',
            'test-authoring/nested-data-sources',
          ],
        },
        {
          type: 'category',
          label: 'Controlling Execution',
          collapsed: false,
          items: [
            'test-authoring/skip',
            'test-authoring/explicit',
            'test-authoring/order',
            'test-authoring/depends-on',
          ],
        },
        {
          type: 'category',
          label: 'Advanced Techniques',
          collapsed: false,
          items: [
            'test-authoring/mocking',
            'test-authoring/generic-attributes',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '⚙️ Running & Integrating',
      collapsed: true,
      items: [
        'execution/test-filters',
        'execution/timeouts',
        'execution/retrying',
        'execution/repeating',
        {
          type: 'category',
          label: 'Parallelism',
          collapsed: false,
          items: [
            'parallelism/not-in-parallel',
            'parallelism/parallel-groups',
            'parallelism/parallel-limiter',
          ],
        },
        {
          type: 'category',
          label: 'CI/CD & Reporting',
          collapsed: false,
          items: [
            'execution/ci-cd-reporting',
            'examples/tunit-ci-pipeline',
          ],
        },
        {
          type: 'category',
          label: 'Integrations & Tooling',
          collapsed: false,
          items: [
            'examples/aspnet',
            'examples/playwright',
            'examples/complex-test-infrastructure',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '💡 Guides & Best Practices',
      collapsed: true,
      items: [
        'guides/best-practices',
        'advanced/performance-best-practices',
        'guides/cookbook',
        'examples/intro',
        'examples/instrumenting-global-test-ids',
        {
          type: 'category',
          label: 'Platform-Specific Scenarios',
          collapsed: false,
          items: [
            'assertions/fsharp',
            'examples/fsharp-interactive',
            'test-authoring/aot-compatibility',
            'test-authoring/culture',
            'examples/filebased-csharp',
          ],
        },
        'troubleshooting',
      ],
    },
    {
      type: 'category',
      label: '🛠️ Extensibility',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'TUnit Internals',
          collapsed: false,
          items: [
            'execution/executors',
            'execution/engine-modes',
            'advanced/exception-handling',
            'advanced/extension-points',
            'advanced/test-variants',
          ],
        },
        {
          type: 'category',
          label: 'Creating Extensions',
          collapsed: false,
          items: [
            'customization-extensibility/data-source-generators',
            'customization-extensibility/argument-formatters',
            'customization-extensibility/display-names',
            'customization-extensibility/logging',
            'customization-extensibility/libraries',
          ],
        },
        {
          type: 'category',
          label: 'Built-in Extensions',
          collapsed: false,
          items: [
            'extensions/extensions',
          ],
        },
        {
          type: 'category',
          label: 'Experimental Features',
          collapsed: false,
          items: [
            'experimental/dynamic-tests',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '📚 Reference',
      collapsed: true,
      items: [
        'reference/command-line-flags',
        'reference/test-configuration',
        'comparison/attributes',
      ],
    },
    {
      type: 'category',
      label: 'ℹ️ About TUnit',
      collapsed: true,
      items: [
        'guides/philosophy',
        {
          type: 'category',
          label: 'Performance & Benchmarks',
          collapsed: false,
          items: [
            {
              type: 'autogenerated',
              dirName: 'benchmarks',
            },
          ],
        },
      ],
    },
  ],
};

export default sidebars;
