using TUnit.TestProject.Bugs._7205;

namespace TUnit.TestProject.Bugs._7205.FilterExcluded;

// A separate namespace keeps these out of Issue7205Tests' namespace run. Its property filter builds both
// tests and then excludes Excluded, which never runs, so discovery must dispose the scope created for it
// before Observer runs (#7205). Verified by Issue7205Tests in TUnit.Engine.Tests.
public class FilterExcludedScopeTests
{
    [Test]
    [Property("Issue7205", "Excluded")]
    [FilterExcludedDependencyInjection]
    public void Excluded(ProbeService service)
    {
    }

    [Test]
    [Property("Issue7205", "Observer")]
    public async Task Observer() =>
        await DependencyInjectionScopeDisposalObserverTests.AssertScopes(nameof(FilterExcludedScopeTests), 1);
}
