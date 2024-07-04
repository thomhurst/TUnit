using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[ClassDataSource(typeof(SomeClass), Shared = SharedType.Keyed, Key = "🌲")]
[SuppressMessage("Usage", "TUnit0018:Test methods should not assign instance data")]
public class ClassDataSourceDrivenTests_Shared_Keyed2
{
    private static readonly List<SomeClass> MethodLevels = [];
    private static readonly List<SomeClass> ClassLevels = [];

    public ClassDataSourceDrivenTests_Shared_Keyed2(SomeClass someClass)
    {
        ClassLevels.Add(someClass);
    }
    
    [DataSourceDrivenTest]
    [ClassDataSource(typeof(SomeClass), Shared = SharedType.Keyed, Key = "🔑")]
    public void DataSource_Class(SomeClass value)
    {
        MethodLevels.Add(value);
    }

    [DataSourceDrivenTest]
    [ClassDataSource<SomeClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public void DataSource_Class_Generic(SomeClass value)
    {
        MethodLevels.Add(value);
    }

    [AfterAllTestsInClass]
    public static async Task AssertAfter()
    {
        await Assert.That(ClassLevels).Is.Not.Empty();
        await Assert.That(MethodLevels).Is.Not.Empty();

        foreach (var classLevel in ClassLevels)
        {
            await Assert.That(classLevel.IsDisposed).Is.True();
        }
        
        foreach (var methodLevel in MethodLevels)
        {
            await Assert.That(methodLevel.IsDisposed).Is.True();
        }
    }

    public record SomeClass : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        
        public int Value => 1;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}