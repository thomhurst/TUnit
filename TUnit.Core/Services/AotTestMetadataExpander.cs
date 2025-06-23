using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Services;

/// <summary>
/// AOT-safe implementation of test metadata expander.
/// This implementation works only with source-generated metadata and avoids runtime type resolution.
/// </summary>
[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
public class AotTestMetadataExpander : ITestMetadataExpander
{
    private readonly ITestNameFormatter _testNameFormatter;

    public AotTestMetadataExpander(ITestNameFormatter testNameFormatter)
    {
        _testNameFormatter = testNameFormatter;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TestDefinition>> ExpandTestsAsync(
        ITestDescriptor metadata,
        CancellationToken cancellationToken = default)
    {
        return metadata switch
        {
            StaticTestDefinition staticDef => await ExpandStaticTestAsync(staticDef, cancellationToken),
            DynamicTestMetadata => throw new NotSupportedException(
                "Dynamic test metadata is not supported in AOT mode. Use source generation instead."),
            _ => throw new NotSupportedException($"Unsupported test descriptor type: {metadata.GetType().Name}")
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ExpandedTest> ExpandTestAsync(
        ITestDescriptor testDescriptor,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // In AOT mode, test expansion should be handled by source-generated code
        // This method should not be called in AOT scenarios
        await Task.Yield(); // Make method truly async to avoid warning
        yield break;
    }

    private async Task<IEnumerable<TestDefinition>> ExpandStaticTestAsync(
        StaticTestDefinition staticDef,
        CancellationToken cancellationToken)
    {
        // StaticTestDefinition already provides all the data we need
        // Just create TestDefinition(s) based on it
        
        // Create a single TestDefinition that will be expanded by TestBuilder
        var definition = new TestDefinition
        {
            TestId = staticDef.TestId,
            MethodMetadata = staticDef.TestMethodMetadata,
            TestFilePath = staticDef.TestFilePath,
            TestLineNumber = staticDef.TestLineNumber,
            TestClassFactory = () => staticDef.ClassFactory(Array.Empty<object?>()),
            TestMethodInvoker = async (instance, ct) => await staticDef.MethodInvoker(instance, Array.Empty<object?>(), ct),
            PropertiesProvider = () => staticDef.PropertyValuesProvider().FirstOrDefault() ?? new Dictionary<string, object?>(),
            ClassDataProvider = staticDef.ClassDataProvider,
            MethodDataProvider = staticDef.MethodDataProvider,
            OriginalClassFactory = staticDef.ClassFactory,
            OriginalMethodInvoker = staticDef.MethodInvoker
        };

        return await Task.FromResult(new[] { definition });
    }
}