using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Models.Properties;

internal class NotInParallelConstraintKeysProperty(IReadOnlyList<string>? constraintKeys) : IProperty
{
    public ConstraintKeysCollection? ConstraintKeys { get; } = constraintKeys == null ? null : new ConstraintKeysCollection(constraintKeys);
}