using TUnit.Core.Interfaces;

namespace TUnit;

public class DependencyInjectionClassConstructor : IClassConstructor
{
    public T Create<T>(ClassConstructorMetadata classConstructorMetadata) where T : class
    {
        Console.WriteLine("You can also control how your test classes are new'd up, giving you lots of power and the ability to utilise tools such as dependency injection");

        if (typeof(T) == typeof(AndEvenMoreTests))
        {
            return (new AndEvenMoreTests(new DataClass()) as T)!;
        }

        throw new NotImplementedException();
    }
}
