using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.TestProject.Dummy;

namespace TUnit.TestProject;

[ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🌲")]
[SuppressMessage("Usage", "TUnit0018:Test methods should not assign instance data")]
public class ClassDataSourceDrivenTestsSharedKeyed2
{
    private readonly SomeAsyncDisposableClass _someAsyncDisposableClass;
    private static readonly List<SomeAsyncDisposableClass> MethodLevels = [];
    private static readonly List<SomeAsyncDisposableClass> ClassLevels = [];

    public ClassDataSourceDrivenTestsSharedKeyed2(SomeAsyncDisposableClass someAsyncDisposableClass)
    {
        _someAsyncDisposableClass = someAsyncDisposableClass;
        ClassLevels.Add(someAsyncDisposableClass);
    }
    
    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public async Task DataSource_Class(SomeAsyncDisposableClass value)
    {
        await Assert.That(_someAsyncDisposableClass.IsDisposed).Is.False();
        await Assert.That(value.IsDisposed).Is.False();
        MethodLevels.Add(value);
    }

    [Test]
    [ClassDataSource<SomeAsyncDisposableClass>(Shared = SharedType.Keyed, Key = "🔑")]
    public async Task DataSource_Class_Generic(SomeAsyncDisposableClass value)
    {
        await Assert.That(_someAsyncDisposableClass.IsDisposed).Is.False();
        await Assert.That(value.IsDisposed).Is.False();
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
}