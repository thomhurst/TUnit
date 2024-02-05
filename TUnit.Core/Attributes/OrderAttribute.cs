namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method)]
public class OrderAttribute : TUnitAttribute
{
    public int Order { get; }

    public OrderAttribute(int order)
    {
        Order = order;
    }
}