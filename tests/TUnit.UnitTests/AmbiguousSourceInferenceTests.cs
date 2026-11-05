using System.Reflection;
using TUnit.Engine.Discovery;

#pragma warning disable TUnit0019 // Subjects are inspected by reflection, not executed as tests.

namespace TUnit.UnitTests;

public class AmbiguousSourceInferenceTests
{
    [Test]
    public async Task Invalid_Overloads_Preserve_Explicit_Method_Instantiation()
    {
        var method = typeof(MethodSubject).GetMethod(nameof(MethodSubject.Run))!;
        var instantiations = ReflectionTestDataCollector.ResolveMethodInstantiations(typeof(MethodSubject), method).ToArray();

        await Assert.That(instantiations).Count().IsEqualTo(1);
        await Assert.That(instantiations[0].Method.GetGenericArguments()[0]).IsEqualTo(typeof(int));
    }

    [Test]
    public async Task Invalid_Overloads_Do_Not_Abort_Class_Inference()
    {
        // Exercise the class inference boundary directly without executing an invalid source.
        var infer = typeof(ReflectionTestDataCollector).GetMethod(
            "InferClassTypeArgumentsFromOwnMemberRowType", BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments = [typeof(ClassSubject<>), new MethodDataSourceAttribute("Rows"), null];

        var inferred = infer.Invoke(null, arguments);

        await Assert.That(inferred).IsNull();
        await Assert.That(arguments[2]).IsNull();
    }

    public class MethodSubject
    {
        public static IEnumerable<(int, string)> Rows(int value) => [(value, "row")];
        public static IEnumerable<(int, string)> Rows(string value) => [(1, value)];

        [GenerateGenericTest(typeof(int)), MethodDataSource(nameof(Rows))]
        public void Run<T>((T, string) row, int extra = 5) { }
    }

    public class ClassSubject<T>(T value)
    {
        public T Value => value;
        public static IEnumerable<int> Rows(int value) => [value];
        public static IEnumerable<int> Rows(string value) => [value.Length];
    }
}
