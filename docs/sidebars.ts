import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

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
        'getting-started/libraries',
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
        'test-authoring/method-data-source',
        'test-authoring/data-driven-tests',
        'test-authoring/skip',
        'test-authoring/explicit',
        'test-authoring/depends-on',
        'test-authoring/matrix-tests',
        'test-authoring/order',
      ],
    },
    {
      type: 'category',
      label: 'Assertions',
      items: [
        'assertions/awaiting',
        'assertions/extensibility-chaining-and-converting',
        'assertions/extensibility-returning-items-from-await',
        'assertions/scopes',
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
      label: 'Parallelism & Execution Control',
      items: [
        'parallelism-execution/not-in-parallel',
        'parallelism-execution/parallel-groups',
        'parallelism-execution/test-filters',
        'reference/command-line-flags',
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
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      items: [
        'comparison/framework-differences',
        'comparison/attributes',
        'reference/engine-modes',
        'reference/test-configuration',
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
