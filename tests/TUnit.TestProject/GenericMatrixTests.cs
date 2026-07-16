using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class GenericMatrixTests
{
    // Test with typed Matrix attribute for generic type inference
    [Test]
    [MatrixDataSource]
    public async Task GenericMethodWithTypedMatrix<T>([Matrix<int>(1, 2, 3)] T value)
    {
        // T should be inferred as int from Matrix<int>
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        // Value assertions work for specific types, not generic types
        await Task.CompletedTask;
    }
    
    // Test with multiple typed Matrix parameters
    [Test]
    [MatrixDataSource]
    public async Task GenericMethodWithMultipleMatrixParams<T1, T2>(
        [Matrix<int>(1, 2)] T1 num,
        [Matrix<string>("a", "b")] T2 text)
    {
        // T1 should be int, T2 should be string
        await Assert.That(typeof(T1)).IsEqualTo(typeof(int));
        await Assert.That(typeof(T2)).IsEqualTo(typeof(string));
        // Value assertions work for specific types, not generic types
        await Task.CompletedTask;
    }
    
    // Test Matrix with generic class
    [Test]
    [MatrixDataSource]
    public async Task TestMatrixInGenericClass<T>([Matrix<int>(5, 10, 15)] T value) where T : struct
    {
        // Should work with struct constraint
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        // Value assertions work for specific types, not generic types
        await Task.CompletedTask;
    }
    
    // Test combining Matrix with Arguments (simplified for now)
    [Test]
    [MatrixDataSource]
    public async Task GenericMethodWithMatrixOnly<T>([Matrix<int>(1, 2)] T num)
    {
        // T from Matrix
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        // Value assertions work for specific types, not generic types
        await Task.CompletedTask;
    }
}

// Generic class with Matrix tests
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(int))]
public class GenericClassMatrixTests<T> where T : struct
{
    [Test]
    [Arguments(42)]
    public async Task TestWithArguments(T value)
    {
        // T should be inferred from Arguments
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        // Value assertions work for specific types, not generic types
        await Task.CompletedTask;
    }
    
    [Test]
    [MatrixDataSource]
    public async Task TestWithMatrix([Matrix<int>(10, 20)] T value)
    {
        // T should be inferred from Matrix<int>
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        // Value assertions work for specific types, not generic types
        await Task.CompletedTask;
    }
}