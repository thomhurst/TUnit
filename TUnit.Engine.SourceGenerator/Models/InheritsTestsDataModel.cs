using System.Collections.Generic;

namespace TUnit.Engine.SourceGenerator.Models;

internal record InheritsTestsDataModel : TestCollectionDataModel
{
    public string MinimalTypeName { get; }

    public InheritsTestsDataModel(string minimalTypeName, IEnumerable<TestSourceDataModel> testSourceDataModels) : base(testSourceDataModels)
    {
        MinimalTypeName = minimalTypeName;
    }
}