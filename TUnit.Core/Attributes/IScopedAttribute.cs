namespace TUnit.Core;

public interface IScopedAttribute;

public interface IScopedAttribute<T> : IScopedAttribute where T : Attribute;
