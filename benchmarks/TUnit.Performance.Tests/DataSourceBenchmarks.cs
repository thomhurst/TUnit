using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TUnit.Performance.Tests;

[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.NativeAot80)]
[MemoryDiagnoser]
[JsonExporterAttribute.Full]
public class DataSourceBenchmarks
{
    private PropertyInfo? _dataProperty;
    private MethodInfo? _dataMethod;
    private MethodInfo? _asyncDataMethod;
    private Func<IEnumerable<object?[]>>? _propertyDelegate;
    private Func<IEnumerable<object?[]>>? _methodDelegate;
    private Func<Task<IEnumerable<object?[]>>>? _asyncMethodDelegate;

    [GlobalSetup]
    public void Setup()
    {
        var type = typeof(TestDataProvider);
        
        // Setup reflection info
        _dataProperty = type.GetProperty(nameof(TestDataProvider.PropertyData))!;
        _dataMethod = type.GetMethod(nameof(TestDataProvider.MethodData))!;
        _asyncDataMethod = type.GetMethod(nameof(TestDataProvider.AsyncMethodData))!;
        
        // Setup pre-compiled delegates
        _propertyDelegate = () => TestDataProvider.PropertyData;
        _methodDelegate = TestDataProvider.MethodData;
        _asyncMethodDelegate = TestDataProvider.AsyncMethodData;
    }

    [Benchmark(Baseline = true)]
    public void GetPropertyData_Reflection()
    {
        var data = (IEnumerable<object?[]>)_dataProperty!.GetValue(null)!;
        _ = data.Count();
    }

    [Benchmark]
    public void GetPropertyData_Delegate()
    {
        var data = _propertyDelegate!();
        _ = data.Count();
    }

    [Benchmark]
    public void GetMethodData_Reflection()
    {
        var data = (IEnumerable<object?[]>)_dataMethod!.Invoke(null, null)!;
        _ = data.Count();
    }

    [Benchmark]
    public void GetMethodData_Delegate()
    {
        var data = _methodDelegate!();
        _ = data.Count();
    }

    [Benchmark]
    public async Task GetAsyncMethodData_Reflection()
    {
        var result = _asyncDataMethod!.Invoke(null, null);
        var task = (Task<IEnumerable<object?[]>>)result!;
        var data = await task;
        _ = data.Count();
    }

    [Benchmark]
    public async Task GetAsyncMethodData_Delegate()
    {
        var data = await _asyncMethodDelegate!();
        _ = data.Count();
    }

    [Benchmark]
    public void ExpandComplexData_Reflection()
    {
        var data = GetComplexDataViaReflection();
        var expanded = ExpandDataSource(data);
        _ = expanded.Count();
    }

    [Benchmark]
    public void ExpandComplexData_Delegate()
    {
        var data = GetComplexDataViaDelegate();
        var expanded = ExpandDataSource(data);
        _ = expanded.Count();
    }

    [Benchmark]
    public void ConvertTupleData_Reflection()
    {
        var tupleData = TestDataProvider.TupleData();
        var converted = ConvertTuplesViaReflection(tupleData);
        _ = converted.Count();
    }

    [Benchmark]
    public void ConvertTupleData_Delegate()
    {
        var tupleData = TestDataProvider.TupleData();
        var converted = ConvertTuplesDirectly(tupleData);
        _ = converted.Count();
    }

    private IEnumerable<object?[]> GetComplexDataViaReflection()
    {
        var method = typeof(TestDataProvider).GetMethod(nameof(TestDataProvider.ComplexData));
        var result = method!.Invoke(null, null);
        return (IEnumerable<object?[]>)result!;
    }

    private IEnumerable<object?[]> GetComplexDataViaDelegate()
    {
        return TestDataProvider.ComplexData();
    }

    private List<object?[]> ExpandDataSource(IEnumerable<object?[]> data)
    {
        return data.SelectMany(arr => 
        {
            // Simulate expansion logic
            if (arr.Length > 0 && arr[0] is int[] intArray)
            {
                return intArray.Select(i => new object?[] { i });
            }
            return new[] { arr };
        }).ToList();
    }

    private List<object?[]> ConvertTuplesViaReflection(IEnumerable<(int, string, bool)> tuples)
    {
        var result = new List<object?[]>();
        foreach (var tuple in tuples)
        {
            var tupleType = tuple.GetType();
            var fields = tupleType.GetFields();
            var values = fields.Select(f => f.GetValue(tuple)).ToArray();
            result.Add(values);
        }
        return result;
    }

    private List<object?[]> ConvertTuplesDirectly(IEnumerable<(int, string, bool)> tuples)
    {
        var result = new List<object?[]>();
        foreach (var (item1, item2, item3) in tuples)
        {
            result.Add(new object?[] { item1, item2, item3 });
        }
        return result;
    }

    public static class TestDataProvider
    {
        public static IEnumerable<object?[]> PropertyData { get; } = GenerateData(100);

        public static IEnumerable<object?[]> MethodData()
        {
            return GenerateData(100);
        }

        public static async Task<IEnumerable<object?[]>> AsyncMethodData()
        {
            await Task.Yield();
            return GenerateData(100);
        }

        public static IEnumerable<object?[]> ComplexData()
        {
            yield return new object?[] { new int[] { 1, 2, 3 }, "test" };
            yield return new object?[] { new int[] { 4, 5, 6 }, "data" };
            yield return new object?[] { new int[] { 7, 8, 9 }, "source" };
        }

        public static IEnumerable<(int, string, bool)> TupleData()
        {
            for (int i = 0; i < 50; i++)
            {
                yield return (i, $"item_{i}", i % 2 == 0);
            }
        }

        private static IEnumerable<object?[]> GenerateData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new object?[] { i, $"test_{i}", i * 2 };
            }
        }
    }
}