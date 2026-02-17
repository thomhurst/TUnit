using Microsoft.EntityFrameworkCore;
using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.EfCore;

public class TodoDbContext : DbContext
{
    public string SchemaName { get; set; } = "public";

    public DbSet<Todo> Todos => Set<Todo>();

    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200).HasColumnName("title");
            entity.Property(t => t.IsComplete).HasDefaultValue(false).HasColumnName("is_complete");
            entity.Property(t => t.CreatedAt).HasDefaultValueSql("NOW()").HasColumnName("created_at");
            entity.ToTable("todos");
        });
    }
}
