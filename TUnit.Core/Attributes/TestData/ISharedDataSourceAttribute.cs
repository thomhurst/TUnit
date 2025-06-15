namespace TUnit.Core;

public interface ISharedDataSourceAttribute : IDataAttribute
{
    IEnumerable<SharedType> GetSharedTypes();
    IEnumerable<string> GetKeys();
}