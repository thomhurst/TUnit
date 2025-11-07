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
      label: '🚀 Getting Started',
      collapsed: false,
      items: [
        'intro',
        'getting-started/installation',
        'getting-started/writing-your-first-test',
        'getting-started/running-your-tests',
        {
          type: 'category',
          label: 'Understanding TUnit',
          collapsed: true,
          items: [
            'guides/philosophy',
            'comparison/framework-differences',
            'benchmarks/index',
            'benchmarks/calculator',
            'benchmarks/methodology',
          ],
        },
        'faq',
        'troubleshooting',
      ],
    },
    {
      type: 'category',
      label: '📖 Core Concepts',
      collapsed: true,
      items: [
        'test-authoring/things-to-know',
        {
          type: 'category',
          label: 'Test Lifecycle & Hooks',
          collapsed: false,
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
      ],
    },
    {
      type: 'category',
      label: '✅ Assertions',
      collapsed: true,
      items: [
        'assertions/getting-started',
        'assertions/library',
        'assertions/equality-and-comparison',
        'assertions/null-and-default',
        'assertions/boolean',
        'assertions/awaiting',
        {
          type: 'category',
          label: 'Type-Specific Assertions',
          collapsed: false,
          items: [
            'assertions/numeric',
            'assertions/string',
            'assertions/datetime',
            'assertions/collections',
            'assertions/dictionaries',
            'assertions/types',
            'assertions/specialized-types',
          ],
        },
        {
          type: 'category',
          label: 'Advanced Assertions',
          collapsed: false,
          items: [
            'assertions/exceptions',
            'assertions/tasks-and-async',
            'assertions/member-assertions',
            'assertions/and-conditions',
            'assertions/or-conditions',
            'assertions/scopes',
            'assertions/assertion-groups',
          ],
        },
        'assertions/fsharp',
        {
          type: 'category',
          label: 'Custom Assertions',
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
      label: '📋 How-To Guides',
      collapsed: true,
      items: [
        'test-authoring/mocking',
        {
          type: 'category',
          label: 'Controlling Test Execution',
          collapsed: false,
          items: [
            'test-authoring/skip',
            'test-authoring/explicit',
            'execution/test-filters',
            'execution/timeouts',
            'test-authoring/order',
          ],
        },
        {
          type: 'category',
          label: 'Reliability & Resilience',
          collapsed: false,
          items: [
            'execution/retrying',
            'execution/repeating',
          ],
        },
        'test-authoring/culture',
        'test-authoring/aot-compatibility',
        'test-authoring/generic-attributes',
        'guides/best-practices',
        'guides/cookbook',
      ],
    },
    {
      type: 'category',
      label: '⚡ Advanced',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'Parallelism & Performance',
          items: [
            'parallelism/not-in-parallel',
            'parallelism/parallel-groups',
            'parallelism/parallel-limiter',
            'advanced/performance-best-practices',
          ],
        },
        {
          type: 'category',
          label: 'CI/CD Integration',
          items: [
            'execution/ci-cd-reporting',
          ],
        },
        {
          type: 'category',
          label: 'Advanced Patterns',
          items: [
            'execution/executors',
            'execution/engine-modes',
            'advanced/exception-handling',
            'advanced/extension-points',
            'advanced/test-variants',
            'test-authoring/depends-on',
          ],
        },
        {
          type: 'category',
          label: 'Experimental',
          items: [
            'experimental/dynamic-tests',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '🛠️ Extending TUnit',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'Built-in Extensions',
          items: [
            'extensions/extensions',
          ],
        },
        {
          type: 'category',
          label: 'Creating Extensions',
          items: [
            'customization-extensibility/data-source-generators',
            'customization-extensibility/argument-formatters',
            'customization-extensibility/display-names',
            'customization-extensibility/logging',
            'customization-extensibility/libraries',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '🔄 Migration Guides',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'From Other Frameworks',
          collapsed: false,
          items: [
            'migration/xunit',
            'migration/nunit',
            'migration/mstest',
          ],
        },
        'migration/testcontext-interface-organization',
      ],
    },
    {
      type: 'category',
      label: '📚 Reference',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'Command Line & Configuration',
          items: [
            'reference/command-line-flags',
            'reference/test-configuration',
          ],
        },
        {
          type: 'category',
          label: 'API Reference',
          items: [
            'comparison/attributes',
          ],
        },
        {
          type: 'category',
          label: 'Examples',
          items: [
            'examples/intro',
            {
              type: 'category',
              label: 'Integration Examples',
              collapsed: true,
              items: [
                'examples/aspnet',
                'examples/playwright',
                'examples/complex-test-infrastructure',
              ],
            },
            {
              type: 'category',
              label: 'Advanced Examples',
              collapsed: true,
              items: [
                'examples/instrumenting-global-test-ids',
                'examples/tunit-ci-pipeline',
                'examples/fsharp-interactive',
                'examples/filebased-csharp',
              ],
            },
          ],
        },
      ],
    },
  ],
};

export default sidebars;
