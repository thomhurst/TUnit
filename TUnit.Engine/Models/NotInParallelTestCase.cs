using Microsoft.Testing.Platform.Extensions.Messages;
using TUnit.Core;

namespace TUnit.Engine.Models;

internal record NotInParallelTestCase
{
    public string Id => Test.TestId;
    public required ConstraintKeysCollection ConstraintKeys { get; init; }
    public required TestInformation Test { get; init; }

    public virtual bool Equals(NotInParallelTestCase? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}