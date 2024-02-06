using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.TestAdapter;

public static class TUnitTestProperties
{
    public static TestProperty GetOrRegisterTestProperty<T>(string name)
    {
        return TestProperty.Find(name)
               ?? TestProperty.Register(name, name, typeof(T), typeof(TestCase));
    }

    public static TestProperty UniqueId => GetOrRegisterTestProperty<string>(nameof(UniqueId));
    public static TestProperty ManagedType => GetOrRegisterTestProperty<string>("ManagedType");
    public static TestProperty ManagedMethod => GetOrRegisterTestProperty<string>("ManagedMethod");

    public static TestProperty FullyQualifiedClassName =>
        GetOrRegisterTestProperty<string>(nameof(FullyQualifiedClassName));

    public static TestProperty IsSkipped => GetOrRegisterTestProperty<bool>(nameof(IsSkipped));
    public static TestProperty IsStatic => GetOrRegisterTestProperty<bool>(nameof(IsStatic));
    public static TestProperty Order => GetOrRegisterTestProperty<int>(nameof(Order));
    public static TestProperty RepeatCount => GetOrRegisterTestProperty<int>(nameof(RepeatCount));
    public static TestProperty RetryCount => GetOrRegisterTestProperty<int>(nameof(RetryCount));

    public static TestProperty Timeout => GetOrRegisterTestProperty<int>(nameof(Timeout));
    
    public static TestProperty NotInParallelConstraintKey =>
        GetOrRegisterTestProperty<string?>(nameof(NotInParallelConstraintKey));

    public static TestProperty MethodParameterTypeNames => GetOrRegisterTestProperty<string[]?>(nameof(MethodParameterTypeNames));
    public static TestProperty MethodArguments => GetOrRegisterTestProperty<string?>(nameof(MethodArguments));
    public static TestProperty ClassArguments => GetOrRegisterTestProperty<string?>(nameof(ClassArguments));

    public static TestProperty TestClass => GetOrRegisterTestProperty<string>(nameof(TestClass));
    public static TestProperty TestName => GetOrRegisterTestProperty<string>(nameof(TestName));
    public static TestProperty Category => GetOrRegisterTestProperty<string[]>(nameof(Category));
    public static TestProperty NotCategory => GetOrRegisterTestProperty<string>(nameof(NotCategory));
}