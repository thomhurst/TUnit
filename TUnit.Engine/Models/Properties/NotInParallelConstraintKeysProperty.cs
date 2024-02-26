using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

public class NotInParallelConstraintKeysProperty(IReadOnlyList<string>? constraintKeys) : IProperty
{
    public IReadOnlyList<string>? ConstraintKeys { get; } = constraintKeys;
}