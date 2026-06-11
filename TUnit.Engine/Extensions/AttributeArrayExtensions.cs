namespace TUnit.Engine.Extensions;

internal static class AttributeArrayExtensions
{
    /// <summary>
    /// Returns the first attribute assignable to <typeparamref name="T"/>, or null.
    /// Allocation-free alternative to OfType&lt;T&gt;().FirstOrDefault() for hot paths.
    /// </summary>
    public static T? FirstOfType<T>(this Attribute[] attributes) where T : Attribute
    {
        foreach (var attribute in attributes)
        {
            if (attribute is T typed)
            {
                return typed;
            }
        }

        return null;
    }
}
