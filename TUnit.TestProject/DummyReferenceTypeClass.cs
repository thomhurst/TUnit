using System.Diagnostics;

namespace TUnit.TestProject;

[DebuggerDisplay("{Id}")]
public class DummyReferenceTypeClass
{
    public string Id { get; } = Guid.NewGuid().ToString();
}