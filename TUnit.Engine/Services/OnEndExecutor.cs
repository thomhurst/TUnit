using System.Text.Json;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Core.Logging;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Extensions;
using TUnit.Engine.Json;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class OnEndExecutor(
    ICommandLineOptions commandLineOptions,
    TUnitFrameworkLogger logger)
{
    public async Task ExecuteAsync(TestSessionContext? testSessionContext)
    {
        try
        {
            await WriteJsonOutputFile(testSessionContext);
        }
        catch (Exception e)
        {
            await logger.LogErrorAsync(e);
        }
    }

    private async Task WriteJsonOutputFile(TestSessionContext? testSessionContext)
    {
        if (!commandLineOptions.IsOptionSet(JsonOutputCommandProvider.OutputJson))
        {
            return;
        }

        try
        {
            var path = Path.Combine(Environment.CurrentDirectory, GetFilename());
        
            await using var file = File.Create(path);

            var jsonOutput = GetJsonOutput(testSessionContext);
        
            await JsonSerializer.SerializeAsync(file, jsonOutput, JsonContext.Default.TestSessionJson);

            await logger.LogInformationAsync($"TUnit JSON output saved to: {path}");
        }
        catch (Exception e)
        {
            await logger.LogErrorAsync(e);
        }
    }

    private string GetFilename()
    {
        var prefix =
            commandLineOptions.TryGetOptionArgumentList(JsonOutputCommandProvider.OutputJsonFilenamePrefix,
                out var prefixes)
                ? prefixes.First()
                : "tunit_jsonoutput_";
        
        var filename = commandLineOptions.TryGetOptionArgumentList(JsonOutputCommandProvider.OutputJsonFilename,
            out var filenames)
            ? filenames.First()
            : Guid.NewGuid().ToString("N");
        
        return $"{prefix}{filename}.json";
    }

    private static TestSessionJson GetJsonOutput(TestSessionContext? testSessionContext)
    {
        if (testSessionContext is null)
        {
            return new TestSessionJson
            {
                Assemblies = []
            };
        }
        
        return testSessionContext.ToJsonModel();
    }
}