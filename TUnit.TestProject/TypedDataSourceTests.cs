namespace TUnit.TestProject;

public class TypedDataSourceTests
{
    [Test]
    [Arguments<int>(42)]
    public async Task SingleValueTypedArguments(int value)
    {
        await Assert.That(value).IsEqualTo(42);
    }
    
    [Test]
    [Arguments<string>("hello")]
    public async Task SingleReferenceTypedArguments(string value)
    {
        await Assert.That(value).IsEqualTo("hello");
    }
    
    [Test]
    [Arguments<int>(1)]
    [Arguments<int>(2)]
    [Arguments<int>(3)]
    public async Task MultipleTypedArguments(int value)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(1);
        await Assert.That(value).IsLessThanOrEqualTo(3);
    }
    
    // Custom typed data source for testing
    public class IntRangeDataSource : TypedDataSourceAttribute<int>
    {
        private readonly int _start;
        private readonly int _end;
        
        public IntRangeDataSource(int start, int end)
        {
            _start = start;
            _end = end;
        }
        
        public override async IAsyncEnumerable<Func<Task<int>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            for (var i = _start; i <= _end; i++)
            {
                var value = i;
                yield return () => Task.FromResult(value);
            }
            await Task.CompletedTask;
        }
    }
    
    [Test]
    [IntRangeDataSource(5, 7)]
    public async Task CustomTypedDataSource(int value)
    {
        await Assert.That(value).IsGreaterThanOrEqualTo(5);
        await Assert.That(value).IsLessThanOrEqualTo(7);
    }
    
    // Typed data source for tuples
    public class TupleDataSource : TypedDataSourceAttribute<(int, string)>
    {
        public override async IAsyncEnumerable<Func<Task<(int, string)>>> GetTypedDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            yield return () => Task.FromResult((1, "one"));
            yield return () => Task.FromResult((2, "two"));
            yield return () => Task.FromResult((3, "three"));
            await Task.CompletedTask;
        }
    }
    
    [Test]
    [TupleDataSource]
    public async Task TupleTypedDataSource(int number, string word)
    {
        await Assert.That(number).IsGreaterThan(0);
        await Assert.That(word).IsNotNull();
        await Assert.That(word.Length).IsGreaterThan(0);
    }
    
    // Async typed data source
    public class AsyncIntDataSource : AsyncDataSourceGeneratorAttribute<int>
    {
        private readonly int _count;
        
        public AsyncIntDataSource(int count)
        {
            _count = count;
        }
        
        protected override async IAsyncEnumerable<Func<Task<int>>> GenerateDataSourcesAsync(DataGeneratorMetadata dataGeneratorMetadata)
        {
            for (var i = 0; i < _count; i++)
            {
                await Task.Delay(1); // Simulate async work
                var value = i * 10;
                yield return () => Task.FromResult(value);
            }
        }
    }
    
    [Test]
    [AsyncIntDataSource(3)]
    public async Task AsyncTypedDataSource(int value)
    {
        await Task.Delay(1);
        await Assert.That(value % 10).IsEqualTo(0);
    }
}