namespace TUnit.Engine.SourceGenerator.Models;

internal record InheritsTestsDataModel
{
    public string MinimalTypeName { get; }
    public IEnumerable<TestSourceDataModel> TestSourceDataModels { get; }

    public InheritsTestsDataModel(string minimalTypeName, IEnumerable<TestSourceDataModel> testSourceDataModels)
    {
        MinimalTypeName = minimalTypeName;
        TestSourceDataModels = testSourceDataModels;
    }

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