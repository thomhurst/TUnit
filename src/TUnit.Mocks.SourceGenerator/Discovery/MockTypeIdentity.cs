using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Discovery;

/// <summary>
/// The mocked target a model describes: its type plus any additional interfaces. Models sharing an
/// identity are the same target reached in different modes (<c>Mock.Of</c> and <c>Mock.Wrap</c> of
/// one type, say), which callers have to tell apart from two distinct targets that merely collide
/// on a generated name.
/// </summary>
internal static class MockTypeIdentity
{
    internal static string Of(MockTypeModel model)
        => model.AdditionalInterfaceNames.Length == 0
            ? model.FullyQualifiedName
            : model.FullyQualifiedName + "|" + string.Join("|", model.AdditionalInterfaceNames);
}
