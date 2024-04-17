using System.Collections.Generic;
using System.Linq;

namespace TUnit.Engine.SourceGenerator.Models;

internal record TestSourceCollection
{
    public IEnumerable<TestSourceDataModel> TestSourceDataModels { get; }

    public TestSourceCollection(IEnumerable<TestSourceDataModel> testSourceDataModels)
    {
        TestSourceDataModels = testSourceDataModels;
    }

    public virtual bool Equals(TestSourceCollection? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return TestSourceDataModels.SequenceEqual(other.TestSourceDataModels);
    }

    public override int GetHashCode()
    {
        return TestSourceDataModels.GetHashCode();
    }
}