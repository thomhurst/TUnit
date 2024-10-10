using System.Diagnostics;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Exceptions;

namespace TUnit.Engine.Services;

internal class TUnitInitializer(ICommandLineOptions commandLineOptions)
{
    public void Initialize()
    {
        ParseParameters();
        SetUpExceptionListeners();

        if (TestContext.OutputDirectory != null)
        {
            TestContext.WorkingDirectory = TestContext.OutputDirectory;
        }
    }

    private void SetUpExceptionListeners()
    {
        Trace.Listeners.Insert(0, new ThrowListener());
    }

    private void ParseParameters()
    {
        if (!commandLineOptions.TryGetOptionArgumentList(ParametersCommandProvider.TestParameter, out var parameters))
        {
            return;
        }
        
        foreach (var parameter in parameters)
        {
            var split = parameter.Split('=');
            TestContext.InternalParametersDictionary.Add(split[0], split[1]);   
        }
    }
}