using System.Collections.Immutable;

namespace TUnit.Core;

/// <summary>
/// Contains a collection of tests that can be run. Should be disposed to ensure that the
/// temporary <see cref="AppDomain"/> containing the test assemblies is unloaded.
/// </summary>
public sealed class TestCollection
{
    /// <summary>
    /// The test sources (assembly file names).
    /// </summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>
    /// The tests that were discovered.
    /// </summary>
    public IReadOnlyList<Test> Tests { get; private set; }

    public TestCollection(IEnumerable<string> sources, IEnumerable<Test> tests)
    {
        Sources = ImmutableArray.CreateRange(sources);
        Tests = ImmutableArray.CreateRange(tests);
    }

    /// <summary>
    /// Filters the tests in the test collection. This is used for partial test runs.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    public void Filter(Func<Test, bool> filter)
    {
        Tests = ImmutableArray.CreateRange(Tests.Where(filter));
    }
}