using TUnit.Example.Asp.Net.Models;

namespace TUnit.Example.Asp.Net.Repositories;

public interface ITodoRepository
{
    Task<IEnumerable<Todo>> GetAllAsync();
    Task<Todo?> GetByIdAsync(int id);
    Task<Todo> CreateAsync(Todo todo);
    Task<Todo?> UpdateAsync(int id, Todo todo);
    Task<bool> DeleteAsync(int id);
}
