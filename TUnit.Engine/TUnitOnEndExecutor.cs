using System.Text.Json;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
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
        
        await JsonSerializer.SerializeAsync(file, TestDictionary.GetAllTestDetails(), CachedJsonOptions.Instance);
    }
}