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

        await using var file = File.Create(Path.Combine(Environment.CurrentDirectory, $"TUnit_JsonOutput_{Guid.NewGuid():N}.json"));

        var jsonOutputs = GetJsonOutputs();
        
        await JsonSerializer.SerializeAsync(file, jsonOutputs, CachedJsonOptions.Instance);
    }

    private static IEnumerable<JsonOutput> GetJsonOutputs()
    {
        return ClassHookOrchestrator.GetAssemblyHookContext()
            .AllTests
            .Select(x => new JsonOutput
            {
                TestId = x.TestInformation.TestId,
                TestName = x.TestInformation.TestName,
                DisplayName = x.TestInformation.DisplayName,
                // ClassType = x.TestInformation.ClassType,
                // TestClassInstance = x.TestInformation.ClassInstance,
                Categories = x.TestInformation.Categories,
                Order = x.TestInformation.Order,
                Timeout = x.TestInformation.Timeout,
                CustomProperties = x.TestInformation.CustomProperties,
                RetryCount = x.TestInformation.RetryCount,
                // ReturnType = x.TestInformation.ReturnType,
                // TestClassArguments = x.TestInformation.TestClassArguments,
                TestFilePath = x.TestInformation.TestFilePath,
                TestLineNumber = x.TestInformation.TestLineNumber,
                // TestMethodArguments = x.TestInformation.TestMethodArguments,
                // TestClassParameterTypes = x.TestInformation.TestClassParameterTypes,
                // TestMethodParameterTypes = x.TestInformation.TestMethodParameterTypes,
                NotInParallelConstraintKeys = x.TestInformation.NotInParallelConstraintKeys,
                Result = x.Result,
                ObjectBag = x.ObjectBag,
            });
    }
}