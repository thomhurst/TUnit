using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;

namespace TUnit.Example.Asp.Net.TestProject;

/// <summary>
/// Base class for ASP.NET Core integration tests using the WebApplicationTest pattern.
/// Provides shared container injection and configuration for all test classes.
/// </summary>
public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
}
