using System.Text.Json;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.Json;

namespace TUnit.Engine;

internal class TUnitOnEndExecutor
{
    private readonly ICommandLineOptions _commandLineOptions;

    public TUnitOnEndExecutor(ICommandLineOptions commandLineOptions)
    {
        _commandLineOptions = commandLineOptions;
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

        await using var file = File.Create(Path.Combine(Environment.CurrentDirectory, GetFilename()));

        var jsonOutputs = GetJsonOutputs();
        
        await JsonSerializer.SerializeAsync(file, jsonOutputs, CachedJsonOptions.Instance);
    }

    private string GetFilename()
    {
        var prefix =
            _commandLineOptions.TryGetOptionArgumentList(JsonOutputCommandProvider.OutputJsonFilenamePrefix,
                out var prefixes)
                ? prefixes.First()
                : "tunit_jsonoutput_";
        
        var filename = _commandLineOptions.TryGetOptionArgumentList(JsonOutputCommandProvider.OutputJsonFilenamePrefix,
            out var filenames)
            ? filenames.First()
            : Guid.NewGuid().ToString("N");
        
        return $"{prefix}{filename}.json";
    }

    private static IEnumerable<JsonOutput> GetJsonOutputs()
    {
        return ClassHookOrchestrator.GetAssemblyHookContext()
            .AllTests
            .Where(x => x.Result != null)
            .Select(x => new JsonOutput
            {
                TestId = x.TestInformation.TestId,
                TestName = x.TestInformation.TestName,
                DisplayName = x.TestInformation.DisplayName,
                ClassType = x.TestInformation.ClassType,
                Categories = x.TestInformation.Categories,
                Order = x.TestInformation.Order,
                Timeout = x.TestInformation.Timeout,
                CustomProperties = x.TestInformation.CustomProperties,
                RetryCount = x.TestInformation.RetryCount,
                ReturnType = x.TestInformation.ReturnType,
                TestClassArguments = x.TestInformation.TestClassArguments?.Select(y => y?.ToString()).ToArray(),
                TestFilePath = x.TestInformation.TestFilePath,
                TestLineNumber = x.TestInformation.TestLineNumber,
                TestMethodArguments = x.TestInformation.TestMethodArguments?.Select(y => y?.ToString()).ToArray(),
                TestClassParameterTypes = x.TestInformation.TestClassParameterTypes,
                TestMethodParameterTypes = x.TestInformation.TestMethodParameterTypes,
                NotInParallelConstraintKeys = x.TestInformation.NotInParallelConstraintKeys,
                Result = x.Result,
                ObjectBag = x.ObjectBag,
            });
    }
}