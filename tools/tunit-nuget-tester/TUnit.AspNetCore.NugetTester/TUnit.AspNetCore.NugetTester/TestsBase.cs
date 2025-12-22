using TUnit.AspNetCore;

namespace TUnit.AspNetCore.NugetTester;

/// <summary>
/// Base class for ASP.NET Core integration tests using the WebApplicationTest pattern.
/// </summary>
public abstract class TestsBase : WebApplicationTest<TestWebAppFactory, Program>
{
}
