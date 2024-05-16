using System.Collections.Generic;
using System.Linq;

namespace TUnit.Engine.SourceGenerator.Models;

internal record InheritsTestsDataModel : TestCollectionDataModel
{
    public string MinimalTypeName { get; }

    public InheritsTestsDataModel(string minimalTypeName, IEnumerable<TestSourceDataModel> testSourceDataModels) : base(testSourceDataModels)
    {
        MinimalTypeName = minimalTypeName;
    }
}