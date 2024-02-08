using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TUnit.Engine;

internal static class TUnitTestProperties
{
    public static TestProperty GetOrRegisterTestProperty<T>(string id, string label)
    {
        return TestProperty.Find(id)
               ?? TestProperty.Register(id, label, typeof(T), typeof(TestCase));
    }

    public static TestProperty UniqueId => GetOrRegisterTestProperty<string>(nameof(UniqueId), nameof(UniqueId));
    public static TestProperty Hierarchy => GetOrRegisterTestProperty<string[]>("TestCase.Hierarchy", "Hierarchy");
    public static TestProperty ManagedType => GetOrRegisterTestProperty<string>("TestCase.ManagedType", "ManagedType");
    public static TestProperty ManagedMethod => GetOrRegisterTestProperty<string>("TestCase.ManagedMethod", "ManagedMethod");

    public static TestProperty AssemblyQualifiedClassName =>
        GetOrRegisterTestProperty<string>(nameof(AssemblyQualifiedClassName), nameof(AssemblyQualifiedClassName));

    public static TestProperty IsSkipped => GetOrRegisterTestProperty<bool>(nameof(IsSkipped), nameof(IsSkipped));
    public static TestProperty IsStatic => GetOrRegisterTestProperty<bool>(nameof(IsStatic), nameof(IsStatic));
    public static TestProperty Order => GetOrRegisterTestProperty<int>(nameof(Order), nameof(Order));
    public static TestProperty RepeatCount => GetOrRegisterTestProperty<int>(nameof(RepeatCount), nameof(RepeatCount));
    public static TestProperty RetryCount => GetOrRegisterTestProperty<int>(nameof(RetryCount), nameof(RetryCount));

    public static TestProperty Timeout => GetOrRegisterTestProperty<double?>(nameof(Timeout), nameof(Timeout));
    
    public static TestProperty NotInParallelConstraintKeys =>
        GetOrRegisterTestProperty<string[]?>(nameof(NotInParallelConstraintKeys), nameof(NotInParallelConstraintKeys));

    public static TestProperty MethodParameterTypeNames => GetOrRegisterTestProperty<string[]?>(nameof(MethodParameterTypeNames), nameof(MethodParameterTypeNames));
    public static TestProperty MethodArguments => GetOrRegisterTestProperty<string?>(nameof(MethodArguments), nameof(MethodArguments));
    public static TestProperty ClassArguments => GetOrRegisterTestProperty<string?>(nameof(ClassArguments), nameof(ClassArguments));
    public static TestProperty ClassParameterTypeNames => GetOrRegisterTestProperty<string[]?>(nameof(ClassParameterTypeNames), nameof(ClassParameterTypeNames));

    public static TestProperty TestClass => ManagedType;
    public static TestProperty TestName => GetOrRegisterTestProperty<string>(nameof(TestName), nameof(TestName));
    public static TestProperty Category => GetOrRegisterTestProperty<string[]>(nameof(Category), nameof(Category));
    public static TestProperty NotCategory => GetOrRegisterTestProperty<string>(nameof(NotCategory), nameof(NotCategory));
}