using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.Engine.Models;

internal record NotInParallelTestCase
{
    public string Id => TestCase.Id.ToString();
    public required ConstraintKeysCollection ConstraintKeys { get; init; }
    public required TestCase TestCase { get; init; }

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