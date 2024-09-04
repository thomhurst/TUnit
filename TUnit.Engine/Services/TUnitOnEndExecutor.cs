using System.Text.Json;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Hooks;
using TUnit.Engine.Json;
using TUnit.Engine.Logging;

namespace TUnit.Engine.Services;

internal class TUnitOnEndExecutor
{
    private readonly ICommandLineOptions _commandLineOptions;
    private readonly TUnitLogger _logger;

    public TUnitOnEndExecutor(ICommandLineOptions commandLineOptions, TUnitLogger logger)
    {
        _commandLineOptions = commandLineOptions;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        await WriteJsonOutputFile();
    }

    private async Task WriteJsonOutputFile()
    {
        if (!_commandLineOptions.IsOptionSet(JsonOutputCommandProvider.OutputJson))
        {
            return;
        }

        try
        {
            var path = Path.Combine(Environment.CurrentDirectory, GetFilename());
        
            await using var file = File.Create(path);

            var jsonOutputs = GetJsonOutputs();
        
            await JsonSerializer.SerializeAsync(file, jsonOutputs, CachedJsonOptions.Instance);

            await _logger.LogInformationAsync($"TUnit JSON output saved to: {path}");
        }
        catch (Exception e)
        {
            await _logger.LogErrorAsync(e);
        }
    }

    private string GetFilename()
    {
        var prefix =
            _commandLineOptions.TryGetOptionArgumentList(JsonOutputCommandProvider.OutputJsonFilenamePrefix,
                out var prefixes)
                ? prefixes.First()
                : "tunit_jsonoutput_";
        
        var filename = _commandLineOptions.TryGetOptionArgumentList(JsonOutputCommandProvider.OutputJsonFilename,
            out var filenames)
            ? filenames.First()
            : Guid.NewGuid().ToString("N");
        
        return $"{prefix}{filename}.json";
    }

    private static IEnumerable<JsonOutput> GetJsonOutputs()
    {
        return AssemblyHookOrchestrator.GetAllAssemblyHookContexts()
            .SelectMany(x => x.AllTests)
            .Where(x => x.Result != null)
            .Select(x => new JsonOutput
            {
                TestId = x.TestDetails.TestId,
                TestName = x.TestDetails.TestName,
                DisplayName = x.TestDetails.DisplayName,
                ClassType = x.TestDetails.ClassType,
                Categories = x.TestDetails.Categories,
                Order = x.TestDetails.Order,
                Timeout = x.TestDetails.Timeout,
                CustomProperties = x.TestDetails.CustomProperties,
                RetryLimit = x.TestDetails.RetryLimit,
                ReturnType = x.TestDetails.ReturnType,
                TestClassArguments = x.TestDetails.TestClassArguments.Select(y => y?.ToString()).ToArray(),
                TestFilePath = x.TestDetails.TestFilePath,
                TestLineNumber = x.TestDetails.TestLineNumber,
                TestMethodArguments = x.TestDetails.TestMethodArguments.Select(y => y?.ToString()).ToArray(),
                TestClassParameterTypes = x.TestDetails.TestClassParameterTypes,
                TestMethodParameterTypes = x.TestDetails.TestMethodParameterTypes,
                NotInParallelConstraintKeys = x.TestDetails.NotInParallelConstraintKeys,
                Result = x.Result,
                ObjectBag = x.ObjectBag,
            });
    }
}