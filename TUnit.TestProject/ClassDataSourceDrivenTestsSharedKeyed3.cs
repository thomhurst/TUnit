using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

[ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🌲")]
[SuppressMessage("Usage", "TUnit0018:Test methods should not assign instance data")]
public class ClassDataSourceDrivenTestsSharedKeyed3
{
    private readonly SomeAsyncDisposableClass _someAsyncDisposableClass;
    private static readonly List<SomeAsyncDisposableClass> MethodLevels = [];
    private static readonly List<SomeAsyncDisposableClass> ClassLevels = [];

    public ClassDataSourceDrivenTestsSharedKeyed3(SomeAsyncDisposableClass someAsyncDisposableClass)
    {
        _someAsyncDisposableClass = someAsyncDisposableClass;
        ClassLevels.Add(someAsyncDisposableClass);
    }
    
    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public async Task DataSource_Class(SomeAsyncDisposableClass value)
    {
        await Assert.That(_someAsyncDisposableClass.IsDisposed).IsFalse();
        await Assert.That(value.IsDisposed).IsFalse();
        MethodLevels.Add(value);
    }

    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public async Task DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        await Assert.That(_someAsyncDisposableClass.IsDisposed).IsFalse();
        await Assert.That(value.IsDisposed).IsFalse();
        MethodLevels.Add(value);
    }

    [After(Class)]
    public static async Task AssertAfter()
    {
        await Assert.That(ClassLevels).IsNotEmpty();
        await Assert.That(MethodLevels).IsNotEmpty();

        foreach (var classLevel in ClassLevels)
        {
            await Assert.That(classLevel.IsDisposed).IsTrue();
        }
        
        foreach (var methodLevel in MethodLevels)
        {
            await Assert.That(methodLevel.IsDisposed).IsTrue();
        }
    }
}