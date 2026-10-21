using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.CommandLine;
using TUnit.Engine.Capabilities;
using TUnit.Engine.CommandLineProviders;
using TUnit.Engine.Configuration;
using TUnit.Engine.Enums;

namespace TUnit.UnitTests;

// Regression tests for https://github.com/thomhurst/TUnit/issues/7078: the banner must report the
// engine mode the engine actually selects. [NotInParallel] because tests change TUNIT_EXECUTION_MODE.
[NotInParallel]
public class BannerCapabilityTests
{
    private string? _previousExecutionMode;

    [Before(HookType.Test)]
    public void ClearExecutionMode()
    {
        _previousExecutionMode = Environment.GetEnvironmentVariable(EnvironmentConstants.ExecutionMode);
        Environment.SetEnvironmentVariable(EnvironmentConstants.ExecutionMode, null);
    }

    [After(HookType.Test)]
    public void RestoreExecutionMode()
        => Environment.SetEnvironmentVariable(EnvironmentConstants.ExecutionMode, _previousExecutionMode);

    [Test]
    public async Task Reports_Reflection_When_Environment_Variable_Requests_Reflection()
    {
        SkipOnAot();
        Environment.SetEnvironmentVariable(EnvironmentConstants.ExecutionMode, "reflection");

        await Assert.That(CreateBanner(new FakeCommandLineOptions()).GetMode()).IsEqualTo(EngineMode.Reflection);
    }

    [Test]
    public async Task Reports_SourceGenerated_When_Environment_Variable_Requests_SourceGeneration()
    {
        Environment.SetEnvironmentVariable(EnvironmentConstants.ExecutionMode, "sourcegeneration");

        await Assert.That(CreateBanner(new FakeCommandLineOptions()).GetMode()).IsEqualTo(EngineMode.SourceGenerated);
    }

    [Test]
    public async Task Reports_Reflection_When_Reflection_Flag_Is_Set()
    {
        SkipOnAot();
        var options = new FakeCommandLineOptions(ReflectionModeCommandProvider.ReflectionMode);

        await Assert.That(CreateBanner(options).GetMode()).IsEqualTo(EngineMode.Reflection);
    }

    [Test]
    public async Task Falls_Back_To_Source_Registration_Without_Overrides()
    {
        // With no CLI or environment override, the engine falls back to whether generated sources
        // registered themselves (false when EnableTUnitSourceGeneration=false strips the generator).
        var options = new FakeCommandLineOptions();
        var expected = SourceRegistrar.IsSourceGenerationAvailable
            ? EngineMode.SourceGenerated
            : EngineMode.Reflection;

        await Assert.That(CreateBanner(options).GetMode()).IsEqualTo(expected);
    }

    private static BannerCapability CreateBanner(ICommandLineOptions options)
        => new(null!, options, null!);

    private static void SkipOnAot()
    {
#if NET
        if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported)
        {
            Skip.Test("Native AOT always forces source-generated mode.");
        }
#endif
    }

    private sealed class FakeCommandLineOptions(params string[] setOptions) : ICommandLineOptions
    {
        public bool IsOptionSet(string optionName) => setOptions.Contains(optionName);

        public bool TryGetOptionArgumentList(string optionName, [NotNullWhen(true)] out string[]? arguments)
        {
            if (setOptions.Contains(optionName))
            {
                arguments = [];
                return true;
            }

            arguments = null;
            return false;
        }
    }
}
