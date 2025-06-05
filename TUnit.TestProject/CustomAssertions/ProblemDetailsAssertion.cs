#if NET

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using TUnit.Assertions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.CustomAssertions;

public class ProblemDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "about:blank";

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("detail")]
    public required string Detail { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; } = new Dictionary<string, object>();
}

public class HttpResponseDeserializesToAssertCondition<TToType>(JsonTypeInfo<TToType> jsonTypeInfo) : ConvertToAssertCondition<HttpResponseMessage, TToType>
{
    public override async ValueTask<(AssertionResult, TToType?)> ConvertValue(HttpResponseMessage? value)
    {
        if (value is null)
        {
            return (AssertionResult.Fail("HttpResponseMessage is null"), default(TToType?));
        }
        
        var convertedValue = await value.Content.ReadFromJsonAsync(jsonTypeInfo);
        
        return (AssertionResult.Passed, convertedValue);
    }

    protected override string GetExpectation()
    {
        return $"to deserialize to {typeof(TToType).Name}";
    }
}

public static class HttpResponseAssertionExtensions
{
    public static InvokableValueAssertionBuilder<TToType> DeSerializesTo<TToType>(this IValueSource<HttpResponseMessage> valueSource, JsonTypeInfo<TToType> jsonTypeInfo)
    {
        return valueSource.RegisterConversionAssertion(new HttpResponseDeserializesToAssertCondition<TToType>(jsonTypeInfo), [])!;
    }
    
    public static InvokableValueAssertionBuilder<ProblemDetails> IsProblemDetails(this IValueSource<HttpResponseMessage> valueSource)
    {
        return valueSource.DeSerializesTo(ProblemDetailsSourceGenerationContext.Default.ProblemDetails);
    }
}

public static class ProblemDetailsAssertionExtensions
{
    public static InvokableValueAssertionBuilder<ProblemDetails> HasTitle(this IValueSource<ProblemDetails> valueSource,
        string title, [CallerArgumentExpression("title")] string? titleExpression = null)
    {
        return valueSource.RegisterAssertion(new ProblemDetailsHasTitleAssertCondition(title), [titleExpression]);
    }
    
    public static InvokableValueAssertionBuilder<ProblemDetails> HasDetail(this IValueSource<ProblemDetails> valueSource,
        string detail, [CallerArgumentExpression("detail")] string? detailExpression = null)
    {
        return valueSource.RegisterAssertion(new ProblemDetailsHasDetailAssertCondition(detail), [detailExpression]);
    }
}

public class ProblemDetailsHasTitleAssertCondition(string? expected) : ExpectedValueAssertCondition<ProblemDetails, string>(expected)
{
    protected override string GetExpectation()
    {
        return $"to have a title of {expected}";
    }

    protected override ValueTask<AssertionResult> GetResult(ProblemDetails? actualValue, string? expectedValue)
    {
        return AssertionResult.FailIf(actualValue is null, "ProblemDetails is null")
            .OrFailIf(actualValue!.Title != expectedValue, $"ProblemDetails has a title of {actualValue.Title}");
    }
}

public class ProblemDetailsHasDetailAssertCondition(string? expected) : ExpectedValueAssertCondition<ProblemDetails, string>(expected)
{
    protected override string GetExpectation()
    {
        return $"to have a detail of {expected}";
    }

    protected override ValueTask<AssertionResult> GetResult(ProblemDetails? actualValue, string? expectedValue)
    {
        return AssertionResult.FailIf(actualValue is null, "ProblemDetails is null")
            .OrFailIf(actualValue!.Detail != expectedValue, $"ProblemDetails has a detail equal to {actualValue.Title}");
    }
}

#endif