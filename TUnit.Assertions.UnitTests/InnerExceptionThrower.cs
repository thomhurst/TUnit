using System.Data;

namespace TUnit.Assertions.UnitTests;

public class InnerExceptionThrower
{
    public static void Throw()
    {
        throw new DataException(
            "Message 1", new AggregateException(
                "Message 2", new ArithmeticException(
                    "Message 3", new ArgumentException(
                        "Message 4", new TaskCanceledException(
                            "Message 5", new NullReferenceException(
                                "Message 6"
                                )
                        )
                    )
                )
            )
        );
    }
}