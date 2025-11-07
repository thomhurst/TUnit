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
        {
          type: 'category',
          label: 'Parallelism & Performance',
          collapsed: false,
          items: [
            'parallelism/not-in-parallel',
            'parallelism/parallel-groups',
            'parallelism/parallel-limiter',
            'advanced/performance-best-practices',
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
      label: '📋 Guides & Recipes',
      collapsed: true,
      items: [
        'examples/intro',
        {
          type: 'category',
          label: 'Common Testing Techniques',
          collapsed: false,
          items: [
            'test-authoring/mocking',
            'test-authoring/generic-attributes',
          ],
        },
        {
          type: 'category',
          label: 'Controlling Test Execution',
          collapsed: false,
          items: [
            'test-authoring/skip',
            'test-authoring/explicit',
            'test-authoring/order',
            'test-authoring/depends-on',
            'execution/test-filters',
            'execution/timeouts',
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
        {
          type: 'category',
          label: 'Integrations',
          collapsed: false,
          items: [
            'examples/aspnet',
            'examples/playwright',
            'examples/complex-test-infrastructure',
            'execution/ci-cd-reporting',
            'examples/tunit-ci-pipeline',
          ],
        },
        {
          type: 'category',
          label: 'Platform-Specific Scenarios',
          collapsed: false,
          items: [
            'test-authoring/aot-compatibility',
            'test-authoring/culture',
            'examples/fsharp-interactive',
            'examples/filebased-csharp',
          ],
        },
        {
          type: 'category',
          label: 'Best Practices & Patterns',
          collapsed: false,
          items: [
            'guides/best-practices',
            'guides/cookbook',
            'examples/instrumenting-global-test-ids',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '🛠️ Advanced & Extensibility',
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
          collapsed: false,
          items: [
            'reference/command-line-flags',
            'reference/test-configuration',
          ],
        },
        {
          type: 'category',
          label: 'API Reference',
          collapsed: false,
          items: [
            'comparison/attributes',
          ],
        },
      ],
    },
    {
      type: 'category',
      label: '💡 About TUnit',
      collapsed: true,
      items: [
        'guides/philosophy',
        'comparison/framework-differences',
        {
          type: 'category',
          label: 'Benchmarks & Performance',
          collapsed: false,
          items: [
            'benchmarks/index',
            'benchmarks/calculator',
            'benchmarks/methodology',
          ],
        },
      ],
    },
  ],
};

export default sidebars;
