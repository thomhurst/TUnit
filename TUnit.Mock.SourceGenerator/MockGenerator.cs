using Microsoft.CodeAnalysis;
using TUnit.Mock.SourceGenerator.Builders;
using TUnit.Mock.SourceGenerator.Discovery;
using TUnit.Mock.SourceGenerator.Models;

namespace TUnit.Mock.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class MockGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all Mock.Of<T>() invocations
        var mockTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: MockTypeDiscovery.IsMockOfInvocation,
                transform: MockTypeDiscovery.TransformToModel)
            .Where(model => model is not null)
            .Select((model, _) => model!);

        // Step 2: Deduplicate by fully qualified name
        var distinctTypes = mockTypes
            .Collect()
            .SelectMany((models, _) => models.Distinct());

        // Step 3: Generate source for each unique type
        context.RegisterSourceOutput(distinctTypes, (spc, model) =>
        {
            // Generate mock implementation
            var implSource = MockImplBuilder.Build(model);
            spc.AddSource($"{GetSafeFileName(model)}_MockImpl.g.cs", implSource);

            // Generate setup surface
            var setupSource = MockSetupBuilder.Build(model);
            spc.AddSource($"{GetSafeFileName(model)}_MockSetup.g.cs", setupSource);

            // Generate verify surface
            var verifySource = MockVerifyBuilder.Build(model);
            spc.AddSource($"{GetSafeFileName(model)}_MockVerify.g.cs", verifySource);

            // Generate raise surface (if type has events)
            if (model.Events.Length > 0)
            {
                var raiseSource = MockRaiseBuilder.Build(model);
                spc.AddSource($"{GetSafeFileName(model)}_MockRaise.g.cs", raiseSource);
            }

            // Generate factory
            var factorySource = MockFactoryBuilder.Build(model);
            spc.AddSource($"{GetSafeFileName(model)}_MockFactory.g.cs", factorySource);
        });
    }

    private static string GetSafeFileName(MockTypeModel model)
    {
        return model.FullyQualifiedName
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "");
    }
}
