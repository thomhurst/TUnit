using Microsoft.CodeAnalysis;
using TUnit.Mocks.SourceGenerator.Builders;
using TUnit.Mocks.SourceGenerator.Discovery;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public class MockGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all Mock.Of<T>() invocations and transform to model arrays
        var mockTypes = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: MockTypeDiscovery.IsMockOfInvocation,
                transform: MockTypeDiscovery.TransformToModels)
            .SelectMany((models, _) => models);

        // Step 2: Deduplicate
        var distinctTypes = mockTypes
            .Collect()
            .SelectMany((models, _) => models.Distinct());

        // Step 3: Generate source for each unique type
        context.RegisterSourceOutput(distinctTypes, (spc, model) =>
        {
            if (model.IsDelegateType)
            {
                // Delegate mock: generate members and delegate factory (no impl class)
                GenerateDelegateMock(spc, model);
            }
            else if (model.IsWrapMock)
            {
                // Wrap mock: generate wrap impl, wrap factory, plus members
                GenerateWrapMock(spc, model);
            }
            else if (model.AdditionalInterfaceNames.Length > 0)
            {
                // Multi-interface mock: generate ONLY impl + factory
                // Members/raise come from the single-type model (also emitted)
                GenerateMultiInterfaceMock(spc, model);
            }
            else
            {
                // Single-type mock: generate everything
                GenerateSingleTypeMock(spc, model);
            }
        });
    }

    private static void GenerateSingleTypeMock(SourceProductionContext spc, MockTypeModel model)
    {
        var fileName = GetSafeFileName(model);

        // Generate mock implementation
        var implSource = MockImplBuilder.Build(model);
        spc.AddSource($"{fileName}_MockImpl.g.cs", implSource);

        // Generate unified members surface (setup + verify)
        var membersSource = MockMembersBuilder.Build(model);
        spc.AddSource($"{fileName}_MockMembers.g.cs", membersSource);

        // Generate events surface (if type has events)
        if (model.Events.Length > 0)
        {
            var eventsSource = MockEventsBuilder.Build(model);
            spc.AddSource($"{fileName}_MockEvents.g.cs", eventsSource);
        }

        // Generate factory
        var factorySource = MockFactoryBuilder.Build(model);
        spc.AddSource($"{fileName}_MockFactory.g.cs", factorySource);
    }

    private static void GenerateDelegateMock(SourceProductionContext spc, MockTypeModel model)
    {
        var fileName = GetSafeFileName(model);

        // Generate unified members surface (setup + verify)
        var membersSource = MockMembersBuilder.Build(model);
        spc.AddSource($"{fileName}_MockMembers.g.cs", membersSource);

        // Generate delegate factory (creates the delegate lambda + wraps in Mock<T>)
        var factorySource = MockDelegateFactoryBuilder.Build(model);
        spc.AddSource($"{fileName}_MockDelegateFactory.g.cs", factorySource);
    }

    private static void GenerateWrapMock(SourceProductionContext spc, MockTypeModel model)
    {
        var fileName = GetSafeFileName(model);

        // Generate wrap mock implementation (delegates to wrapped instance for unconfigured calls)
        var implSource = MockImplBuilder.Build(model);
        spc.AddSource($"{fileName}_WrapMockImpl.g.cs", implSource);

        // Generate unified members surface (setup + verify)
        var membersSource = MockMembersBuilder.Build(model);
        spc.AddSource($"{fileName}_MockMembers.g.cs", membersSource);

        // Generate events surface (if type has events)
        if (model.Events.Length > 0)
        {
            var eventsSource = MockEventsBuilder.Build(model);
            spc.AddSource($"{fileName}_MockEvents.g.cs", eventsSource);
        }

        // Generate wrap factory
        var factorySource = MockFactoryBuilder.Build(model);
        spc.AddSource($"{fileName}_WrapMockFactory.g.cs", factorySource);
    }

    private static void GenerateMultiInterfaceMock(SourceProductionContext spc, MockTypeModel model)
    {
        var fileName = GetSafeFileName(model);

        // Generate combined impl (implements all interfaces)
        var implSource = MockImplBuilder.Build(model);
        spc.AddSource($"{fileName}_MockImpl.g.cs", implSource);

        // Generate multi-interface factory
        var factorySource = MockFactoryBuilder.Build(model);
        spc.AddSource($"{fileName}_MockFactory.g.cs", factorySource);
    }

    private static string GetSafeFileName(MockTypeModel model)
        => MockImplBuilder.GetCompositeSafeName(model);
}
