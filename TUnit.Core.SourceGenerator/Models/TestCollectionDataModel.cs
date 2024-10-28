namespace TUnit.Core.SourceGenerator.Models;

public record TestCollectionDataModel(IEnumerable<TestSourceDataModel> TestSourceDataModels)
{
    public virtual bool Equals(TestCollectionDataModel? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return TestSourceDataModels.SequenceEqual(other.TestSourceDataModels);
    }

    public override int GetHashCode()
    {
        return TestSourceDataModels.GetHashCode();
    }
}