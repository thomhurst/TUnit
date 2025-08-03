using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Reflection;
using System.Threading.Tasks;
using TUnit.Core;

namespace TUnit.Performance.Tests;

[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.NativeAot80)]
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class TestExecutionBenchmarks
{
    private TestClass? _testInstance;
    private MethodInfo? _syncMethod;
    private MethodInfo? _asyncMethod;
    private MethodInfo? _paramMethod;
    private Func<object, object?[], Task>? _syncDelegate;
    private Func<object, object?[], Task>? _asyncDelegate;
    private Func<object, object?[], Task>? _paramDelegate;
    private object?[] _emptyArgs = Array.Empty<object?>();
    private object?[] _paramArgs = new object?[] { 10, 20 };

    [GlobalSetup]
    public void Setup()
    {
        _testInstance = new TestClass();
        var type = typeof(TestClass);
        
        // Setup reflection info
        _syncMethod = type.GetMethod(nameof(TestClass.SyncTest))!;
        _asyncMethod = type.GetMethod(nameof(TestClass.AsyncTest))!;
        _paramMethod = type.GetMethod(nameof(TestClass.ParameterizedTest))!;
        
        // Setup pre-compiled delegates
        _syncDelegate = async (instance, args) =>
        {
            ((TestClass)instance).SyncTest();
            await Task.CompletedTask;
        };
        
        _asyncDelegate = async (instance, args) =>
        {
            await ((TestClass)instance).AsyncTest();
        };
        
        _paramDelegate = async (instance, args) =>
        {
            ((TestClass)instance).ParameterizedTest((int)args[0]!, (int)args[1]!);
            await Task.CompletedTask;
        };
    }

    [Benchmark(Baseline = true)]
    public async Task ExecuteSyncTest_Reflection()
    {
        var result = _syncMethod!.Invoke(_testInstance, _emptyArgs);
        if (result is Task task)
            await task;
    }

    [Benchmark]
    public async Task ExecuteSyncTest_Delegate()
    {
        await _syncDelegate!(_testInstance!, _emptyArgs);
    }

    [Benchmark]
    public async Task ExecuteAsyncTest_Reflection()
    {
        var result = _asyncMethod!.Invoke(_testInstance, _emptyArgs);
        if (result is Task task)
            await task;
    }

    [Benchmark]
    public async Task ExecuteAsyncTest_Delegate()
    {
        await _asyncDelegate!(_testInstance!, _emptyArgs);
    }

    [Benchmark]
    public async Task ExecuteParameterizedTest_Reflection()
    {
        var result = _paramMethod!.Invoke(_testInstance, _paramArgs);
        if (result is Task task)
            await task;
    }

    [Benchmark]
    public async Task ExecuteParameterizedTest_Delegate()
    {
        await _paramDelegate!(_testInstance!, _paramArgs);
    }

    [Benchmark]
    public void CreateTestInstance_Reflection()
    {
        var instance = Activator.CreateInstance(typeof(TestClass));
    }

    [Benchmark]
    public void CreateTestInstance_Delegate()
    {
        var instance = TestInstanceFactory();
    }

    [Benchmark]
    public async Task ExecuteTestWithHooks_Reflection()
    {
        var instance = Activator.CreateInstance(typeof(TestClass));
        
        // Before hook
        var beforeMethod = typeof(TestClass).GetMethod("BeforeTest");
        beforeMethod?.Invoke(instance, _emptyArgs);
        
        // Test execution
        var testMethod = typeof(TestClass).GetMethod("SyncTest");
        var result = testMethod?.Invoke(instance, _emptyArgs);
        if (result is Task task)
            await task;
        
        // After hook
        var afterMethod = typeof(TestClass).GetMethod("AfterTest");
        afterMethod?.Invoke(instance, _emptyArgs);
    }

    [Benchmark]
    public async Task ExecuteTestWithHooks_Delegate()
    {
        var instance = TestInstanceFactory();
        var testClass = (TestClass)instance;
        
        // Before hook
        testClass.BeforeTest();
        
        // Test execution
        await _syncDelegate!(instance, _emptyArgs);
        
        // After hook
        testClass.AfterTest();
    }

    private static object TestInstanceFactory() => new TestClass();

    public class TestClass
    {
        private int _counter;

        public void BeforeTest()
        {
            _counter = 0;
        }

        public void AfterTest()
        {
            _counter = 0;
        }

        public void SyncTest()
        {
            _counter++;
            if (_counter > 1000)
                throw new InvalidOperationException("Should not reach here");
        }

        public async Task AsyncTest()
        {
            await Task.Yield();
            _counter++;
        }

        public void ParameterizedTest(int a, int b)
        {
            var sum = a + b;
            if (sum < 0)
                throw new InvalidOperationException("Negative sum");
        }
    }
}