using TUnit.TestProject.Attributes;
using Vogen;

namespace TUnit.TestProject.Bugs._1304;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Arguments("\"2c48c152-7cb7-4f51-8f01-704454f36e60\"")]
    [Arguments("invalidNotAGui")]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments(null)]
    public async Task TryParse_InvalidString_ReturnsFailure(string? input, CancellationToken cancellationToken)
    {
        // Act
        var success = AccountId.TryParse(input!, out var id);

        // Assert
        await Assert.That(success).IsFalse();
        await Assert.That(id.HasValue).IsFalse();
        await Assert.That(id.ToString()).IsEqualTo("[UNINITIALIZED]");
    }

    [Test]
    [Arguments("\"2c48c152-7cb7-4f51-8f01-704454f36e60\"")]
    [Arguments("invalidNotAGui")]
    [Arguments("")]
    [Arguments(" ")]
    [Arguments(null)]
    public async Task Parse_InvalidString_ThrowsException(string? input)
    {
        await Assert.That(() => AccountId.Parse(input!)).ThrowsException();
    }

    // This test works fine
    [Test]
    [Arguments("2c48c152-7cb7-4f51-8f01-704454f36e60")]
    [Arguments("00000000-0000-0000-0000-000000000000")]
    public async Task TryParse_ValidString_ReturnsAccountId(string input)
    {
        // Act
        var success = AccountId.TryParse(input, out var id);

        // Assert
        //using var _ = Assert.Multiple();
        await Assert.That(success).IsTrue();
        await Assert.That(id.ToString()).IsEqualTo(input);
    }
}

// this is the account id's code
[ValueObject<Guid>]
public readonly partial record struct AccountId : IIdentifiable<AccountId>
{
    public bool HasValue => _value != Guid.Empty;

    public static AccountId Empty => From(Guid.Empty);

    /// <summary>
    /// Builds a new ID from a new <see cref="Guid"/> version 4
    /// </summary>
    public static AccountId New => From(Guid.NewGuid());
}

public interface IIdentifiable<out TId>
{
    bool HasValue { get; }
}
