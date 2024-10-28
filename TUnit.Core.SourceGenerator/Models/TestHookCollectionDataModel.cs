namespace TUnit.Core.SourceGenerator.Models;

public record TestHookCollectionDataModel(IEnumerable<HooksDataModel> HooksDataModels)
{
    public virtual bool Equals(TestHookCollectionDataModel? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return HooksDataModels.SequenceEqual(other.HooksDataModels);
    }

    public override int GetHashCode()
    {
        return HooksDataModels.GetHashCode();
    }
}