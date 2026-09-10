using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TUnit.Example.Asp.Net.EfCore;

/// <summary>
/// Custom model cache key factory that includes the schema name in the cache key.
/// This ensures EF Core creates a separate model for each schema, enabling
/// per-test schema isolation without model conflicts.
/// </summary>
public class SchemaModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return context is TodoDbContext todoContext
            ? (context.GetType(), todoContext.SchemaName, designTime)
            : (object)(context.GetType(), designTime);
    }
}
