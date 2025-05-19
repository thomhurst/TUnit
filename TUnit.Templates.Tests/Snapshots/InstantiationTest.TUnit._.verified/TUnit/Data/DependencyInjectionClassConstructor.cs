using TUnit.Core.Interfaces;

namespace TUnit;

public class DependencyInjectionClassConstructor : IClassConstructor
{
    public object Create(Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        Console.WriteLine(@"You can also control how your test classes are new'd up, giving you lots of power and the ability to utilise tools such as dependency injection");

        if (type == typeof(AndEvenMoreTests))
        {
            return new AndEvenMoreTests(new DataClass());
        }

        throw new NotImplementedException();
    }
}