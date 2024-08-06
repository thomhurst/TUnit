using TUnit.Core;

namespace TUnit.TestProject;

public class MatrixTests
{
    [Test]
    public async Task MatrixTest_One(
        [Matrix("A", "B", "C", "D")] string str, 
        [Matrix(1, 2, 3)] int i, 
        [Matrix(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
    
    [Test]
    public async Task MatrixTest_Two(
        [Matrix(1, 2)] int i, 
        [Matrix(1, 2, 3)] int i2, 
        [Matrix(1, 2, 3, 4)] int i3, 
        [Matrix(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
    
    [Test]
    public async Task MatrixTest_Enum(
        [Matrix(1, 2)] int i, 
        [Matrix(-1, TestEnum.One)] TestEnum testEnum)
    {
        await Task.CompletedTask;
    }
}