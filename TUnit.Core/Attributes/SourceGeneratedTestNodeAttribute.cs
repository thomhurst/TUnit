using System.ComponentModel;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class SourceGeneratedTestNodeAttribute : Attribute;