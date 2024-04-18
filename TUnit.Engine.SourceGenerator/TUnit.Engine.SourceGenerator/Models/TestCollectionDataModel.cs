using System.Collections.Generic;
using System.Linq;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestCollectionDataModel
{
    public IEnumerable<TestSourceDataModel> TestSourceDataModels { get; }

    public TestCollectionDataModel(IEnumerable<TestSourceDataModel> testSourceDataModels)
    {
        TestSourceDataModels = testSourceDataModels;
    }

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