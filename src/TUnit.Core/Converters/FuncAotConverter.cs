namespace TUnit.Core.Converters;

public class FuncAotConverter<TSource, TTarget>(Func<TSource, TTarget> converter) : IAotConverter
{
    public Type SourceType { get; } = typeof(TSource);
    public Type TargetType { get; } =  typeof(TTarget);
    public object? Convert(object? value) => converter((TSource)value!);
}
