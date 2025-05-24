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
        'tutorial-basics/installing',
        'tutorial-basics/libraries',
        'tutorial-basics/congratulations',
        'faq',
      ],
    },
    {
      type: 'category',
      label: 'Test Authoring',
      items: [
        'test-authoring/things-to-know',
        'test-authoring/method-data-source',
        'test-authoring/skip',
        'test-authoring/explicit',
        'test-authoring/depends-on',
        'test-authoring/matrix-tests',
        'test-authoring/assertion-groups',
        'test-authoring/and-conditions',
        'test-authoring/or-conditions',
        'test-authoring/class-data-source',
        'test-authoring/repeating',
        'test-authoring/retrying',
        'test-authoring/order',
        'test-authoring/parallel-limiter',
      ],
    },
    {
      type: 'category',
      label: 'Assertions',
      items: [
        'assertions/awaiting',
        'assertions/custom-assertions',
        'assertions/extensibility-chaining-and-converting',
        'assertions/extensibility-returning-items-from-await',
        'tutorial-assertions/scopes',
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
        'tutorial-advanced/test-context',
        'tutorial-advanced/properties',
        'tutorial-advanced/class-constructors',
        'tutorial-advanced/dependency-injection',
        'tutorial-advanced/test-configuration',
        'tutorial-advanced/engine-modes',
        'tutorial-advanced/fsharp-interactive',
      ],
    },
    {
      type: 'category',
      label: 'Parallelism & Execution Control',
      items: [
        'parallelism-execution/not-in-parallel',
        'parallelism-execution/parallel-groups',
        'parallelism-execution/test-filters',
        'tutorial-advanced/command-line-flags',
      ],
    },
    {
      type: 'category',
      label: 'Customization & Extensibility',
      items: [
        'customization-extensibility/extensions',
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
      ],
    },
    {
      type: 'category',
      label: 'Reference',
      items: [
        'reference/attributes',
        'comparison/framework-differences',
        'comparison/attributes',
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
