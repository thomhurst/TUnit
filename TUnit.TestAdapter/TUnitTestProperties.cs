using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.TestAdapter;

public static class TUnitTestProperties
{
    public static TestProperty GetOrRegisterTestProperty<T>(string name)
    {
        return TestProperty.Find(name) 
               ?? TestProperty.Register(name, name, typeof(T), typeof(TestCase));
    }

    public static TestProperty UniqueId => GetOrRegisterTestProperty<string>(nameof(TestDetails.UniqueId));
    public static TestProperty ManagedType => GetOrRegisterTestProperty<string>("ManagedType");
    public static TestProperty ManagedMethod => GetOrRegisterTestProperty<string>("ManagedMethod");
}