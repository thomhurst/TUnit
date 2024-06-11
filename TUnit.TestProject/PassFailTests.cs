using TUnit.Core;

namespace TUnit.TestProject;

public class PassFailTests
{
    [Test]
    [Category("Pass")]
    public void Pass1()
    {
    }
    
    [DataDrivenTest]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Category("Pass")]
    public void Pass2(int value)
    {
    }

    [DataSourceDrivenTest]
    [MethodDataSource(nameof(Data1))]
    [MethodDataSource(nameof(Data2))]
    [MethodDataSource(nameof(Data3))]
    [MethodDataSource(nameof(Data4))]
    [MethodDataSource(nameof(Data5))]
    [EnumerableMethodDataSource(nameof(EnumerableData1))]
    [EnumerableMethodDataSource(nameof(EnumerableData2))]
    [EnumerableMethodDataSource(nameof(EnumerableData3))]
    public void Pass3(int value)
    {
    }
    
    public static int Data1() => 1;
    public static int Data2() => 1;
    public static int Data3() => 1;
    public static int Data4() => 1;
    public static int Data5() => 1;
    
    public static IEnumerable<int> EnumerableData1()
    {
        yield return 1;
        yield return 2;
        yield return 3;
        yield return 4;
        yield return 5; 
    }
    
    public static IEnumerable<int> EnumerableData2()
    {
        yield return 6;
        yield return 7;
        yield return 8;
        yield return 9;
        yield return 10; 
    }
    
    public static IEnumerable<int> EnumerableData3()
    {
        yield return 11;
        yield return 12;
        yield return 13;
        yield return 14;
        yield return 15; 
    }
}