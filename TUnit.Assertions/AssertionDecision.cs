namespace TUnit.Assertions;

public record AssertionDecision
{
    private AssertionDecision()
    {
    }
    
    public static AssertionDecision Pass => new PassDecision();
    public static AssertionDecision Fail(string message) => new FailDecision(message);
    public static AssertionDecision Continue => new ContinueEvaluationDecision();

    internal sealed record PassDecision : AssertionDecision;
    internal sealed record FailDecision(string Message): AssertionDecision;
    internal sealed record ContinueEvaluationDecision: AssertionDecision;
}