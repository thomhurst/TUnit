using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1577;

public partial class Tests
{
    public partial record MyRecordType(IEnumerable<string> SomeEnumerable);
    public partial record MyRecordType2(IReadOnlyCollection<string> SomeCollection);
    public partial record MyRecordType3(IReadOnlyList<string> SomeList);
    public partial record MyRecordType4(IReadOnlyDictionary<string, int> SomeDictionary);

    [Test]
    public async Task MyRecordType_SerializesProperly()
    {
        MyRecordType original = new(["a", "b"]);
        MyRecordType? roundTripped = RoundTripSerialize(original);

        await Assert.That(roundTripped).IsEquivalentTo(original);
    }

    [Test]
    public async Task MyRecordType2_SerializesProperly()
    {
        MyRecordType2 original = new(new List<string> { "a", "b" });
        MyRecordType2? roundTripped = RoundTripSerialize(original);
        await Assert.That(roundTripped).IsEquivalentTo(original);
    }

    [Test]
    public async Task MyRecordType3_SerializesProperly()
    {
        MyRecordType3 original = new(new List<string> { "a", "b" });
        MyRecordType3 roundTripped = RoundTripSerialize(original);
        await Assert.That(roundTripped).IsEquivalentTo(original);
    }

    [Test]
    public async Task MyRecordType4_SerializesProperly()
    {
        MyRecordType4 original = new(new Dictionary<string, int> { { "a", 1 }, { "b", 2 } });
        MyRecordType4? roundTripped = RoundTripSerialize(original);
        await Assert.That(roundTripped).IsEquivalentTo(original);
    }

    private T RoundTripSerialize<T>(T original)
    {
        var jsonTypeInfo = (JsonTypeInfo<T>)SourceGenerationContext.Default.GetTypeInfo(original!.GetType())!;
        return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(original, jsonTypeInfo), jsonTypeInfo)!;
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(MyRecordType))]
    [JsonSerializable(typeof(MyRecordType2))]
    [JsonSerializable(typeof(MyRecordType3))]
    [JsonSerializable(typeof(MyRecordType4))]
    public partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}