﻿using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T1, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T2, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T3, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T4, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T5> : DataSourceGeneratorAttribute<T1, T2, T3, T4, T5>, ITestRegisteredEvents, ITestStartEvent, ITestEndEvent, ILastTestInClassEvent, ILastTestInAssemblyEvent 
    where T1 : new()
    where T2 : new()
    where T3 : new()
    where T4 : new()
    where T5 : new()
{
    private T1? _item1;
    private T2? _item2;
    private T3? _item3;
    private T4? _item4;
    private T5? _item5;
    private DataGeneratorMetadata? _dataGeneratorMetadata;
    
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public override IEnumerable<(T1, T2, T3, T4, T5)> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        _dataGeneratorMetadata = dataGeneratorMetadata;

        var t = Shared switch
        {
            SharedType.None => (
                new T1(),
                new T2(),
                new T3(),
                new T4(),
                new T5()
            ),
            SharedType.Globally => (
                TestDataContainer.GetGlobalInstance(() => new T1()),
                TestDataContainer.GetGlobalInstance(() => new T2()),
                TestDataContainer.GetGlobalInstance(() => new T3()),
                TestDataContainer.GetGlobalInstance(() => new T4()),
                TestDataContainer.GetGlobalInstance(() => new T5())
            ),
            SharedType.ForClass => (
                TestDataContainer.GetInstanceForType<T1>(dataGeneratorMetadata.TestClassType, () => new T1()),
                TestDataContainer.GetInstanceForType<T2>(dataGeneratorMetadata.TestClassType, () => new T2()),
                TestDataContainer.GetInstanceForType<T3>(dataGeneratorMetadata.TestClassType, () => new T3()),
                TestDataContainer.GetInstanceForType<T4>(dataGeneratorMetadata.TestClassType, () => new T4()),
                TestDataContainer.GetInstanceForType<T5>(dataGeneratorMetadata.TestClassType, () => new T5())
            ),
            SharedType.Keyed => (
                TestDataContainer.GetInstanceForKey(Key, () => new T1()),
                TestDataContainer.GetInstanceForKey(Key, () => new T2()),
                TestDataContainer.GetInstanceForKey(Key, () => new T3()),
                TestDataContainer.GetInstanceForKey(Key, () => new T4()),
                TestDataContainer.GetInstanceForKey(Key, () => new T5())
            ),
            SharedType.ForAssembly => (
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T1()),
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T2()),
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T3()),
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T4()),
                TestDataContainer.GetInstanceForAssembly(dataGeneratorMetadata.TestClassType.Assembly, () => new T5())
            ),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        _item1 = t.Item1;
        _item2 = t.Item2;
        _item3 = t.Item3;
        _item4 = t.Item4;
        _item5 = t.Item5;
        
        yield return t;
    }

    public async ValueTask OnTestRegistered(TestContext testContext)
    {
        await ClassDataSources.OnTestRegistered<T1>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item1);
        
        await ClassDataSources.OnTestRegistered<T2>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item2);
        
        await ClassDataSources.OnTestRegistered<T3>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item3);
        
        await ClassDataSources.OnTestRegistered<T4>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item4);
        
        await ClassDataSources.OnTestRegistered<T5>(
            testContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item5);
    }

    public async ValueTask OnTestStart(BeforeTestContext beforeTestContext)
    {
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item1);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item2);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item3);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item4);
        
        await ClassDataSources.OnTestStart(
            beforeTestContext,
            _dataGeneratorMetadata?.PropertyInfo?.GetAccessors()[0].IsStatic == true,
            Shared,
            Key,
            _item5);
    }

    public async ValueTask OnTestEnd(TestContext testContext)
    {
        await ClassDataSources.OnTestEnd(Shared, Key, _item1);
        await ClassDataSources.OnTestEnd(Shared, Key, _item2);
        await ClassDataSources.OnTestEnd(Shared, Key, _item3);
        await ClassDataSources.OnTestEnd(Shared, Key, _item4);
        await ClassDataSources.OnTestEnd(Shared, Key, _item5);
    }

    public async ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInClass<T1>(Shared);
        await ClassDataSources.IfLastTestInClass<T2>(Shared);
        await ClassDataSources.IfLastTestInClass<T3>(Shared);
        await ClassDataSources.IfLastTestInClass<T4>(Shared);
        await ClassDataSources.IfLastTestInClass<T5>(Shared);
    }

    public async ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        await ClassDataSources.IfLastTestInAssembly<T1>(Shared);
        await ClassDataSources.IfLastTestInClass<T2>(Shared);
        await ClassDataSources.IfLastTestInClass<T3>(Shared);
        await ClassDataSources.IfLastTestInClass<T4>(Shared);
        await ClassDataSources.IfLastTestInClass<T5>(Shared);
    }
}