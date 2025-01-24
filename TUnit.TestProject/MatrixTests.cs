﻿using OneOf;

namespace TUnit.TestProject;

public class MatrixTests
{
    [Test]
    [MatrixDataSource]
    public async Task MatrixTest_One(
        [Matrix("A", "B", "C", "D")] string str, 
        [Matrix(1, 2, 3)] int i, 
        [Matrix(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
    
    [Test]
    [MatrixDataSource]
    public async Task MatrixTest_Two(
        [Matrix(1, 2)] int i, 
        [Matrix(1, 2, 3)] int i2, 
        [Matrix(1, 2, 3, 4)] int i3, 
        [Matrix(true, false)] bool boolean)
    {
        await Task.CompletedTask;
    }
    
    [Test]
    [MatrixDataSource]
    public async Task MatrixTest_Enum(
        [Matrix(1, 2)] int i, 
        [Matrix(-1, TestEnum.One)] TestEnum testEnum)
    {
        await Task.CompletedTask;
    }
    
    [Test]
    [MatrixDataSource]
    public async Task AutoGenerateBools(
        [Matrix("A", "B", "C")] string str, 
        bool boolean)
    {
        await Task.CompletedTask;
    }
    
    [Test]
    [MatrixDataSource]
    public async Task AutoGenerateBools2(
        [Matrix("A", "B", "C")] string str, 
        [Matrix] bool boolean)
    {
        await Task.CompletedTask;
    }

    [Test]
    [MatrixDataSource]
    public async Task ImplicitConversion(
        [Matrix(TestEnum.One, TestEnum2.Two)] OneOf<TestEnum, TestEnum2> @enum,
        [Matrix] bool boolean)
    {
        object @enum1 = TestEnum.One;
        
        OneOf<TestEnum, TestEnum2> oneOf = (OneOf<TestEnum, TestEnum2>)(dynamic) @enum1;
        
        await Task.CompletedTask;
    }
}