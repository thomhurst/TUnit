using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Engine.Building;

namespace TUnit.Engine.Services;

/// <summary>
/// Expands a deferred-enumeration placeholder (see <see cref="DeferredEnumerationExecutableTest"/>) into
/// its real test cases at the start of execution. The data source is only deferred for <em>discovery</em>
/// (so the IDE shows one node instead of thousands); at run time the rows are enumerated normally and the
/// resulting tests are scheduled through the standard pipeline, so they get correct hooks and lifecycle
/// counting. The children are reported nested under the placeholder via their <c>ParentTestId</c>.
/// </summary>
internal sealed class DeferredTestExpander
{
    private readonly TestBuilderPipeline _testBuilderPipeline;
    private readonly TestFilterService _testFilterService;

    public DeferredTestExpander(TestBuilderPipeline testBuilderPipeline, TestFilterService testFilterService)
    {
        _testBuilderPipeline = testBuilderPipeline;
        _testFilterService = testFilterService;
    }

    /// <summary>
    /// Builds the real test cases for a deferred placeholder, registers them for execution (event
    /// receivers + argument/reference tracking), and returns them. Each child is parented to the
    /// placeholder so reporters can nest them underneath it.
    /// </summary>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Building tests in reflection mode uses generic type resolution which requires unreferenced code")]
#endif
    public async Task<IReadOnlyList<AbstractExecutableTest>> ExpandAsync(
        AbstractExecutableTest placeholder,
        CancellationToken cancellationToken)
    {
        // IgnoreDeferral re-runs the normal class x method x repeat expansion that discovery skipped.
        // Filter is null: these cases were selected by the placeholder matching the run filter, so the
        // children must not be re-filtered (they have different ids than the placeholder, like dynamic tests).
        var buildingContext = new TestBuildingContext(IsForExecution: false, Filter: null, IgnoreDeferral: true);

        var built = await _testBuilderPipeline
            .BuildTestsFromMetadataAsync([placeholder.Metadata], buildingContext, cancellationToken)
            .ConfigureAwait(false);

        var children = built as IReadOnlyList<AbstractExecutableTest> ?? built.ToList();

        foreach (var child in children)
        {
            child.Context.ParentTestId = placeholder.TestId;
            child.Context.Relationship = TestRelationship.Generated;
        }

        // Children bypassed the discovery pipeline's post-filter registration (they didn't exist then),
        // so register them here before they are scheduled - mirrors TestRegistry's dynamic-test path.
        await _testFilterService.RegisterTestsAsync(children, isForExecution: true).ConfigureAwait(false);

        return children;
    }
}
