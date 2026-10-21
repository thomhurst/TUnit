using CliWrap;
using CliWrap.Buffered;
using Shouldly;
using TUnit.Tests.Shared;

namespace TUnit.Engine.Tests;

/// <summary>
/// Checks that <see cref="DotNetCli.Path"/>, which every test here starts child builds with, resolves an SDK for this
/// repository.
/// </summary>
public class DotNetCliTests
{
    [Test]
    public async Task Path_Resolves_An_Sdk_For_This_Repository()
    {
        var result = await Cli.Wrap(DotNetCli.Path)
            .WithArguments("--version")
            .WithWorkingDirectory(AppContext.BaseDirectory)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        result.ExitCode.ShouldBe(0, $"'{DotNetCli.Path} --version' failed:{Environment.NewLine}{result.StandardOutput}{result.StandardError}");
    }
}
