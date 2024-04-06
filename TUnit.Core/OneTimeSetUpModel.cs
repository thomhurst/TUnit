namespace TUnit.Core;

public record OneTimeSetUpModel(Type Type, List<Func<Task>> Func);