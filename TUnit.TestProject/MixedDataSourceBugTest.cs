using System.Collections;
using TUnit.Core;

namespace TUnit.TestProject;

[ClassDataSource(typeof(ClassData1))]
[ClassDataSource(typeof(ClassData2))]
public class MixedDataSourceBugTest(int classValue)
{
    [Test]
    [MethodDataSource(nameof(GetData))]
    [MethodDataSource(typeof(ExternalData), nameof(ExternalData.GetMoreData))]
    public async Task TestMethod(string value)
    {
        await Task.CompletedTask;
    }
    
    public static IEnumerable<string> GetData()
    {
        yield return "A";
        yield return "B";
    }
    
    public class ClassData1
    {
        public static implicit operator int(ClassData1 _) => 100;
    }
    
    public class ClassData2
    {
        public static implicit operator int(ClassData2 _) => 200;
    }
}

public static class ExternalData
{
    public static IEnumerable<string> GetMoreData()
    {
        yield return "X";
        yield return "Y";
    }
}