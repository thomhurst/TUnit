using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace TUnit.Mocks.InternalsAccess.Tests;

// #6836: a publicized reference that came from a ProjectReference stays a live project
// reference for Roslyn-workspace tooling (MSBuildWorkspace, the C# language server,
// OmniSharp). Those hosts reference the referenced project's own compilation, which has no
// publicized internals, so the editor reports a false CS0122 on code the compiler accepts.
// The targets detach that project reference in design-time builds only; a real build must keep
// it, or copy-local and deps.json would lose the assembly.
//
// The scenario is generated outside the repository so the probe is a plain SDK project: no
// repo-wide props, no shared obj directory, nothing to race with a parallel build.

public class DesignTimeProjectReferenceTests
{
    [Test]
    public async Task Design_Time_Build_Detaches_The_Publicized_Project_Reference()
    {
        var scenario = await Scenario.CreateAsync();

        var reference = await scenario.QueryProjectReferenceAsync(designTimeBuild: true);

        await Assert.That(reference).IsEqualTo("false");
    }

    [Test]
    public async Task Real_Build_Keeps_The_Publicized_Project_Reference()
    {
        var scenario = await Scenario.CreateAsync();

        var reference = await scenario.QueryProjectReferenceAsync(designTimeBuild: false);

        // Untouched: no ReferenceOutputAssembly metadata was written at all.
        await Assert.That(reference).IsEqualTo("");
    }

    [Test]
    public async Task Detaching_Can_Be_Opted_Out_Of()
    {
        var scenario = await Scenario.CreateAsync();

        var reference = await scenario.QueryProjectReferenceAsync(
            designTimeBuild: true,
            "-p:TUnitMocksInternalsAccessDetachDesignTimeProjectReferences=false");

        await Assert.That(reference).IsEqualTo("");
    }

    private sealed class Scenario
    {
        private const string LibraryAssemblyName = "DesignTimeSdkLib";

        private Scenario(string probeProject) => ProbeProject = probeProject;

        private string ProbeProject { get; }

        public static async Task<Scenario> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "tunit-mocks-ia-designtime", Guid.NewGuid().ToString("N"));
            var library = Path.Combine(root, "lib");
            var probe = Path.Combine(root, "probe");
            Directory.CreateDirectory(library);
            Directory.CreateDirectory(probe);

            // Stop MSBuild walking out of the temp directory for props/targets it should not find.
            File.WriteAllText(Path.Combine(root, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(root, "Directory.Build.targets"), "<Project />");

            File.WriteAllText(Path.Combine(library, "lib.csproj"),
                $"""
                 <Project Sdk="Microsoft.NET.Sdk">
                   <PropertyGroup>
                     <TargetFramework>{TargetFramework}</TargetFramework>
                     <AssemblyName>{LibraryAssemblyName}</AssemblyName>
                   </PropertyGroup>
                 </Project>
                 """);

            File.WriteAllText(Path.Combine(library, "Api.cs"),
                $$"""
                  namespace {{LibraryAssemblyName}};

                  internal interface IQuotaPolicy
                  {
                      bool Allow(string clientId);
                  }
                  """);

            File.WriteAllText(Path.Combine(probe, "probe.csproj"),
                $"""
                 <Project Sdk="Microsoft.NET.Sdk">
                   <PropertyGroup>
                     <TargetFramework>{TargetFramework}</TargetFramework>
                     <TUnitMocksExperimentalInternalsAccess>true</TUnitMocksExperimentalInternalsAccess>
                     <TUnitMocksInternalsAccessTasksAssembly>{TasksAssembly}</TUnitMocksInternalsAccessTasksAssembly>
                   </PropertyGroup>
                   <ItemGroup>
                     <TUnitMocksInternalsAccess Include="{LibraryAssemblyName}" />
                     <ProjectReference Include="..\lib\lib.csproj" />
                   </ItemGroup>
                   <Import Project="{TargetsFile}" />
                 </Project>
                 """);

            // Naming the internal type is what the publicized reference buys; if the swap stops
            // working this file no longer compiles.
            File.WriteAllText(Path.Combine(probe, "Use.cs"),
                $$"""
                  using {{LibraryAssemblyName}};

                  internal static class Use
                  {
                      internal static bool Allow(IQuotaPolicy policy) => policy.Allow("acme");
                  }
                  """);

            var probeProject = Path.Combine(probe, "probe.csproj");
            await RunAsync("build", probeProject);
            return new Scenario(probeProject);
        }

        /// <summary>
        /// Runs the compile pipeline without invoking the compiler and reports the
        /// ReferenceOutputAssembly metadata the project reference carries afterwards.
        /// </summary>
        public async Task<string> QueryProjectReferenceAsync(bool designTimeBuild, params string[] extraArguments)
        {
            List<string> arguments =
            [
                "-t:Compile",
                "-p:SkipCompilerExecution=true",
                "-p:ProvideCommandLineArgs=true",
            ];

            if (designTimeBuild)
            {
                // What an IDE passes: nothing is compiled or copied, so the reference swap is
                // free to reshape the reference set the workspace reads back.
                arguments.Add("-p:DesignTimeBuild=true");
                arguments.Add("-p:BuildProjectReferences=false");
            }

            arguments.AddRange(extraArguments);
            arguments.Add("-getItem:ProjectReference");

            var output = await RunAsync("msbuild", ProbeProject, [.. arguments]);

            using var document = JsonDocument.Parse(output[output.IndexOf('{')..]);
            var items = document.RootElement.GetProperty("Items").GetProperty("ProjectReference");
            var item = items.EnumerateArray().Single();
            return item.TryGetProperty("ReferenceOutputAssembly", out var metadata) ? metadata.GetString() ?? "" : "";
        }

        private static async Task<string> RunAsync(string verb, string project, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // A reused MSBuild node keeps the task assembly — this test project's own build
                // output — loaded and locked for the next build in this repository.
                ArgumentList = { verb, project, "-nologo", "-nr:false" },
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)!;
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var output = await standardOutput;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    new StringBuilder()
                        .AppendLine($"dotnet {verb} {project} {string.Join(' ', arguments)} exited with {process.ExitCode}.")
                        .AppendLine(output)
                        .AppendLine(await standardError)
                        .ToString());
            }

            return output;
        }

        private static string TargetFramework => "net10.0";

        private static string TasksAssembly =>
            Path.Combine(AppContext.BaseDirectory, "TUnit.Mocks.InternalsAccess.Tasks.dll");

        private static string TargetsFile => FindRepositoryFile(
            Path.Combine("src", "TUnit.Mocks", "TUnit.Mocks.InternalsAccess.targets"));

        private static string FindRepositoryFile(string relativePath)
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException($"'{relativePath}' was not found above '{AppContext.BaseDirectory}'.");
        }
    }
}
