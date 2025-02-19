namespace TUnit.Core;

public record TestBuilderContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public Dictionary<string, object?> ObjectBag { get; } = [];
    public TestContextEvents Events { get; } = new();

    public List<IDataAttribute> DataAttributes { get; } = [];
}

// A reference for us to access the context which might change within loops by calling the setter
public class TestBuilderContextAccessor(TestBuilderContext context)
{
    public TestBuilderContext Current { get; set; } = context;
}