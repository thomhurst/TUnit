#nullable enable

// Promote the nullability warnings that #5424 / #5425 produced into errors so any
// regression in the source generator (where the generated mock loses nullability
// information from the source event) breaks this project's build instead of
// silently emitting warnings.
//
//   CS8604 — Possible null reference argument          (#5425)
//   CS8612 — Nullability mismatch in implemented type  (#5424)
//   CS8613 — Nullability mismatch in return type
//   CS8614 — Nullability mismatch in parameter type
//   CS8615 — Nullability mismatch in implemented member
#pragma warning error CS8604
#pragma warning error CS8612
#pragma warning error CS8613
#pragma warning error CS8614
#pragma warning error CS8615

namespace TUnit.Mocks.Tests;

// Compile-time regression coverage for:
//   - https://github.com/thomhurst/TUnit/issues/5424 — nullable delegate type
//   - https://github.com/thomhurst/TUnit/issues/5425 — nullable generic type argument
//
// If the source generator stops preserving nullability on event handler types,
// the generated mock will mismatch these source declarations and this file will
// fail to compile.
public class Issue5425Tests
{
    public interface IWithNullableTypeArg
    {
        event EventHandler<string?> Something;
    }

    public interface IWithNullableEvent
    {
        event EventHandler<string>? Something;
    }

    public interface IWithBothNullable
    {
        event EventHandler<string?>? Something;
    }

    [Test]
    public async Task Can_Mock_Event_With_Nullable_Type_Argument()
    {
        var mock = Mock.Of<IWithNullableTypeArg>();
        await Assert.That(mock).IsNotNull();
    }

    [Test]
    public async Task Can_Mock_Nullable_Event()
    {
        var mock = Mock.Of<IWithNullableEvent>();
        await Assert.That(mock).IsNotNull();
    }

    [Test]
    public async Task Can_Mock_Nullable_Event_With_Nullable_Type_Argument()
    {
        var mock = Mock.Of<IWithBothNullable>();
        await Assert.That(mock).IsNotNull();
    }
}
