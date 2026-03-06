using System.Linq.Expressions;
using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Unit tests for ExpressionHelper.ExtractMethodInfo covering all expression tree shapes
/// produced by CreateTestVariantInternal for different test method return types.
/// Regression tests for https://github.com/thomhurst/TUnit/issues/5093
/// </summary>
public class ExpressionHelperTests
{
    // Dummy test class used as expression target
    private class FakeTestClass
    {
        public Task TaskMethod() => Task.CompletedTask;
        public void VoidMethod() { }
        public ValueTask ValueTaskMethod() => default;
        public Task<int> GenericTaskMethod() => Task.FromResult(42);
        public ValueTask<int> GenericValueTaskMethod() => new(42);
    }

    [Test]
    public async Task ExtractMethodInfo_FromMethodCallExpression_ForTaskReturn()
    {
        // Task-returning: body = Call(instance, TestMethod)
        var param = Expression.Parameter(typeof(FakeTestClass), "instance");
        var methodInfo = typeof(FakeTestClass).GetMethod(nameof(FakeTestClass.TaskMethod))!;
        var body = Expression.Call(param, methodInfo);
        var lambda = Expression.Lambda<Func<FakeTestClass, Task>>(body, param);

        var result = ExpressionHelper.ExtractMethodInfo(lambda);

        await Assert.That(result.Name).IsEqualTo(nameof(FakeTestClass.TaskMethod));
    }

    [Test]
    public async Task ExtractMethodInfo_FromBlockExpression_ForVoidReturn()
    {
        // void-returning: body = Block(Call(instance, TestMethod), Constant(Task.CompletedTask))
        var param = Expression.Parameter(typeof(FakeTestClass), "instance");
        var methodInfo = typeof(FakeTestClass).GetMethod(nameof(FakeTestClass.VoidMethod))!;
        var methodCall = Expression.Call(param, methodInfo);
        var body = Expression.Block(methodCall, Expression.Constant(Task.CompletedTask));
        var lambda = Expression.Lambda<Func<FakeTestClass, Task>>(body, param);

        var result = ExpressionHelper.ExtractMethodInfo(lambda);

        await Assert.That(result.Name).IsEqualTo(nameof(FakeTestClass.VoidMethod));
    }

    [Test]
    public async Task ExtractMethodInfo_FromUnaryExpression_ForGenericTaskReturn()
    {
        // Task<T>-returning: body = Convert(Call(instance, TestMethod), Task)
        var param = Expression.Parameter(typeof(FakeTestClass), "instance");
        var methodInfo = typeof(FakeTestClass).GetMethod(nameof(FakeTestClass.GenericTaskMethod))!;
        var methodCall = Expression.Call(param, methodInfo);
        var body = Expression.Convert(methodCall, typeof(Task));
        var lambda = Expression.Lambda<Func<FakeTestClass, Task>>(body, param);

        var result = ExpressionHelper.ExtractMethodInfo(lambda);

        await Assert.That(result.Name).IsEqualTo(nameof(FakeTestClass.GenericTaskMethod));
    }

    [Test]
    public async Task ExtractMethodInfo_FromAsTaskCall_ForValueTaskReturn()
    {
        // ValueTask-returning: body = Call(Call(instance, TestMethod), AsTask)
        var param = Expression.Parameter(typeof(FakeTestClass), "instance");
        var methodInfo = typeof(FakeTestClass).GetMethod(nameof(FakeTestClass.ValueTaskMethod))!;
        var methodCall = Expression.Call(param, methodInfo);
        var asTaskMethod = typeof(ValueTask).GetMethod(nameof(ValueTask.AsTask))!;
        var body = Expression.Call(methodCall, asTaskMethod);
        var lambda = Expression.Lambda<Func<FakeTestClass, Task>>(body, param);

        var result = ExpressionHelper.ExtractMethodInfo(lambda);

        await Assert.That(result.Name).IsEqualTo(nameof(FakeTestClass.ValueTaskMethod));
    }

    [Test]
    public async Task ExtractMethodInfo_FromConvertedAsTaskCall_ForGenericValueTaskReturn()
    {
        // ValueTask<T>-returning: body = Convert(Call(Call(instance, TestMethod), AsTask), Task)
        var param = Expression.Parameter(typeof(FakeTestClass), "instance");
        var methodInfo = typeof(FakeTestClass).GetMethod(nameof(FakeTestClass.GenericValueTaskMethod))!;
        var methodCall = Expression.Call(param, methodInfo);
        var asTaskMethod = typeof(ValueTask<int>).GetMethod(nameof(ValueTask<int>.AsTask))!;
        var asTaskCall = Expression.Call(methodCall, asTaskMethod);
        var body = Expression.Convert(asTaskCall, typeof(Task));
        var lambda = Expression.Lambda<Func<FakeTestClass, Task>>(body, param);

        var result = ExpressionHelper.ExtractMethodInfo(lambda);

        await Assert.That(result.Name).IsEqualTo(nameof(FakeTestClass.GenericValueTaskMethod));
    }

    [Test]
    public async Task ExtractMethodInfo_ThrowsForNullExpression()
    {
        await Assert.That(() => ExpressionHelper.ExtractMethodInfo(null))
            .ThrowsExactly<InvalidOperationException>();
    }
}
