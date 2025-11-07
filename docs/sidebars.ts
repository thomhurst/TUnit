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
        'getting-started/congratulations',
        'faq',
        'troubleshooting',
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
      ],
    },
    {
      type: 'category',
      label: '📝 Writing Tests',
      collapsed: false,
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
          label: 'Assertions',
          collapsed: false,
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
              collapsed: true,
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
              collapsed: true,
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
          label: 'Test Control',
          collapsed: true,
          items: [
            'test-authoring/skip',
            'test-authoring/explicit',
            'test-authoring/depends-on',
            'test-authoring/order',
            'test-authoring/culture',
          ],
        },
        'test-authoring/mocking',
        'test-authoring/aot-compatibility',
        'test-authoring/generic-attributes',
        'guides/best-practices',
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
      label: '⚡ Advanced Features',
      collapsed: true,
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
          ],
        },
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
            'advanced/exception-handling',
            'advanced/extension-points',
            'advanced/test-variants',
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
      label: '📚 Reference & Resources',
      collapsed: true,
      items: [
        {
          type: 'category',
          label: 'Practical Examples',
          items: [
            'examples/intro',
            'guides/cookbook',
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
        {
          type: 'category',
          label: 'Reference Documentation',
          items: [
            'reference/command-line-flags',
            'reference/test-configuration',
            'comparison/attributes',
          ],
        },
      ],
    },
  ],
};

export default sidebars;
