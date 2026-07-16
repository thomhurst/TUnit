using Aspire.Hosting.ApplicationModel;

namespace TUnit.Aspire.Tests.Helpers;

/// <summary>
/// Minimal fake <see cref="IResource"/> implementations for unit-testing resource-filtering
/// logic (<c>ShouldWaitForResource</c>, log selection) without building or starting an app.
/// </summary>
internal sealed class FakeComputeResource(string name) : IComputeResource
{
    public string Name => name;
    public ResourceAnnotationCollection Annotations { get; } = new();
}

/// <summary>A non-compute resource (e.g. ParameterResource, ConnectionStringResource).</summary>
internal sealed class FakeNonComputeResource(string name) : IResource
{
    public string Name => name;
    public ResourceAnnotationCollection Annotations { get; } = new();
}

/// <summary>
/// An <see cref="IComputeResource"/> that also implements <see cref="IResourceWithParent"/>
/// (e.g. Aspire 13.2.0's <c>ProjectRebuilderResource</c>).
/// </summary>
internal sealed class FakeChildComputeResource(string name, IResource parent)
    : IComputeResource, IResourceWithParent
{
    public string Name => name;
    public ResourceAnnotationCollection Annotations { get; } = new();
    public IResource Parent => parent;
}
