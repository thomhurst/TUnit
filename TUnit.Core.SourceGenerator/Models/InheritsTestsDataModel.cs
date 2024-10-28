namespace TUnit.Core.SourceGenerator.Models;

public record InheritsTestsDataModel(string MinimalTypeName, IEnumerable<TestSourceDataModel> TestSourceDataModels)
{
    public virtual bool Equals(InheritsTestsDataModel? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return MinimalTypeName == other.MinimalTypeName && TestSourceDataModels.SequenceEqual(other.TestSourceDataModels);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (MinimalTypeName.GetHashCode() * 397) ^ TestSourceDataModels.GetHashCode();
        }
    }
}