module.exports = {
  docs: [
    {
      type: 'category',
      label: 'Introduction',
      items: [
        'introduction/intro',
        'introduction/faq',
      ],
    },
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/writing-your-first-test',
        'getting-started/running-your-tests',
        'getting-started/migration-from-xunit',
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
      ],
    },
    {
      type: 'category',
      label: 'Assertions',
      items: [
        'assertions/awaiting',
        'assertions/custom-assertions', // Create if needed
        'assertions/extensibility-chaining-and-converting',
        'assertions/extensibility-returning-items-from-await',
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
      ],
    },
    {
      type: 'category',
      label: 'Parallelism & Execution Control',
      items: [
        'parallelism-execution/not-in-parallel',
        'parallelism-execution/parallel-groups',
        'parallelism-execution/test-filters',
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
        'reference/framework-differences',
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