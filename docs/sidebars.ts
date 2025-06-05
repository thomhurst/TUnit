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
      label: 'Getting Started',
      items: [
        'intro',
        'getting-started/installation',
        'getting-started/writing-your-first-test',
        'getting-started/running-your-tests',
        'getting-started/congratulations',
        'faq',
      ],
    },
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
            {
              type: 'link',
              label: 'Data Source Generators',
              href: '/docs/customization-extensibility/data-source-generators',
            },
          ],
        },
        'test-authoring/skip',
        'test-authoring/explicit',
        'test-authoring/depends-on',
        'test-authoring/order',
      ],
    },
    {
      type: 'category',
      label: 'Assertions',
      items: [
        'assertions/awaiting',
        'assertions/and-conditions',
        'assertions/or-conditions',
        'assertions/scopes',
        'assertions/assertion-groups',
        'assertions/delegates',
        {
          type: 'category',
          label: 'Extensibility',
          items: [
            'assertions/extensibility/custom-assertions',
            'assertions/extensibility/extensibility-chaining-and-converting',
            'assertions/extensibility/extensibility-returning-items-from-await',
          ],
        },
        'assertions/fsharp',
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
      label: 'Parallelism Control',
      items: [
        'parallelism/not-in-parallel',
        'parallelism/parallel-groups',
        'parallelism/parallel-limiter',
      ],
    },
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
      label: 'Examples & Use Cases',
      items: [
        'examples/intro',
        'examples/aspnet',
        'examples/playwright',
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
        'comparison/framework-differences',
        'comparison/attributes',
        'reference/test-configuration',
      ],
    },
    {
      type: 'category',
      label: 'Migration Guides',
      items: [
        'migration/xunit',
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
};

export default sidebars;
