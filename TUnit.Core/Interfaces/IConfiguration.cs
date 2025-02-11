namespace TUnit.Core.Interfaces;

public interface IConfiguration
{
    string? Get(string key);
}