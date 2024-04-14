namespace TUnit.Core;

public record BeforeAllTestsInClassModel(Type Type, List<Func<Task>> Func);