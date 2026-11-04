using TUnit.Core.Enums;

namespace TUnit.UnitTests;

public class GenericClassSourceRuntimeTests
{
    // Mirrors generated metadata for C<T,U>(T[] first, U second = default!), including
    // the erased TypeInfo and the shared lazy constructor resolver.
    public sealed class ArrayConstructor<T, U>(T[] first, U second = default!);
    public sealed class MatrixConstructor<T, U>(T[,] first, U second = default!);
    public sealed class NestedArrayConstructor<T, U>(T[][] first, U second = default!);
    public sealed class NestedGenericConstructor<T, U>(IEnumerable<T[]> first, U second = default!);

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Optional_Partition_Retains_Full_Null_And_Convertible_Rows(bool generated)
    {
        foreach (var numeric in new[] { false, true })
        {
            Type[] arguments = numeric ? [typeof(long), typeof(int)] : [typeof(int), typeof(string)];
            IMemberMetadata[] parameters =
            [
                new ParameterMetadata(arguments[0]) { Name = "first", TypeInfo = generated ? new GenericParameter(0, false) : new ConcreteType(arguments[0]), CachedIsOptional = false },
                new ParameterMetadata(arguments[1]) { Name = "second", TypeInfo = generated ? new GenericParameter(1, false) : new ConcreteType(arguments[1]), CachedIsOptional = true }
            ];
            object?[][] original = numeric ? [[1L], [1], [2, 3]] : [[1], [2, null]];
            var source = new MethodDataSourceAttribute("Rows") { Factory = _ => Rows() };
            var partition = GenericClassRowPartition.Create(arguments, GenericClassRowFallback.AllRows, [source], 0);
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = null!, MembersToGenerate = parameters, TestInformation = null,
                Type = DataGeneratorType.ClassParameters, TestSessionId = string.Empty,
                TestClassInstance = null, ClassInstanceArguments = null
            };
            var rows = new List<object?[]?>();
            await foreach (var factory in partition[0].GetDataRowsAsync(metadata))
            {
                rows.Add(await factory());
            }
            await Assert.That(rows.Count).IsEqualTo(original.Length);
            for (var i = 0; i < rows.Count; i++)
            {
                await Assert.That(ReferenceEquals(rows[i], original[i])).IsTrue();
            }

            async IAsyncEnumerable<Func<Task<object?[]?>>> Rows()
            {
                foreach (var row in original)
                {
                    yield return () => Task.FromResult<object?[]?>(row);
                }
                await Task.CompletedTask;
            }
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Partition_Preserves_Supplied_And_Omitted_Optional_Values(bool supplied)
    {
        object?[] original = supplied ? [7, "supplied"] : [7];
        var source = new MethodDataSourceAttribute("Rows") { Factory = _ => OneRow(() => Task.FromResult<object?[]?>(original)) };
        var partition = GenericClassRowPartition.Create([typeof(int)], GenericClassRowFallback.None, [source], 0);
        var factories = await ReadFactories(partition[0]);
        await Assert.That(factories.Count).IsEqualTo(1);
        var row = await factories.Single()();
        await Assert.That(ReferenceEquals(row, original)).IsTrue();
        await Assert.That(row!.Length).IsEqualTo(supplied ? 2 : 1);
        if (supplied)
        {
            await Assert.That(row[1]).IsEqualTo("supplied");
        }
    }

    [Test]
    public async Task Nested_Generic_Constructor_Arguments_Preserve_Covariance()
    {
        IMemberMetadata[] parameters =
        [
            new ParameterMetadata(typeof(object))
            {
                Name = "first", CachedIsOptional = false,
                TypeInfo = new ConstructedGeneric(typeof(IEnumerable<>),
                    [new ConstructedGeneric(typeof(IEnumerable<>), [new ConcreteType(typeof(object))])])
            },
            new ParameterMetadata(typeof(int)) { Name = "second", TypeInfo = new ConcreteType(typeof(int)), CachedIsOptional = true }
        ];
        await Assert.That(GenericClassRowPartition.MatchesConstructorRow([new List<List<string>>()], parameters, [])).IsTrue();
        await Assert.That(GenericClassRowPartition.MatchesConstructorRow([new List<List<int>>()], parameters, [])).IsFalse();
    }

    [Test]
    [Arguments(typeof(ArrayConstructor<,>), typeof(ArrayConstructor<int, int>), 0)]
    [Arguments(typeof(MatrixConstructor<,>), typeof(MatrixConstructor<int, int>), 1)]
    [Arguments(typeof(NestedArrayConstructor<,>), typeof(NestedArrayConstructor<int, int>), 2)]
    [Arguments(typeof(NestedGenericConstructor<,>), typeof(NestedGenericConstructor<int, int>), 3)]
    public async Task Constructor_Array_Filter_Preserves_Element_Type_And_Rank(Type open, Type closed, int shape)
    {
        var generated = ParameterMetadataFactory.ForConstructor(open, true,
            new ParameterMetadata(typeof(object)) { Name = "first", TypeInfo = shape == 3 ? new ConstructedGeneric(typeof(IEnumerable<>), [new ConcreteType(typeof(object))]) : new ConcreteType(typeof(object)), CachedIsOptional = false },
            new ParameterMetadata(typeof(object)) { Name = "second", TypeInfo = new GenericParameter(1, false), CachedIsOptional = true });
        var reflected = closed.GetConstructors()[0].GetParameters().Select(p => (IMemberMetadata)new ParameterMetadata(p.ParameterType)
        {
            Name = p.Name!, TypeInfo = new ConcreteType(p.ParameterType), ReflectionInfo = p
        }).ToArray();
        object valid = shape switch { 1 => new int[1, 1], 2 => new[] { new[] { 1 } }, 3 => new List<int[]> { new[] { 1 } }, _ => new[] { 1 } };
        object invalid = shape switch { 1 => new string[1, 1], 2 => new[] { new[] { "bad" } }, 3 => new List<string[]> { new[] { "bad" } }, _ => new[] { "bad" } };
        foreach (var parameters in new IMemberMetadata[][] { generated, reflected })
        {
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([valid], parameters, [typeof(int), typeof(int)])).IsTrue();
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([invalid], parameters, [typeof(int), typeof(int)])).IsFalse();
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([new int[1, 1, 1]], parameters, [typeof(int), typeof(int)])).IsFalse();
            var source = new MethodDataSourceAttribute("Rows")
            {
                Factory = _ => OneRow(() => Task.FromResult<object?[]?>([invalid]))
            };
            var coordinator = new GenericClassRowPartition.Coordinator([typeof(int), typeof(int)], GenericClassRowFallback.None, [source], [0]);
            var evaluation = await coordinator.EvaluateAsync(new DataGeneratorMetadata
            {
                TestBuilderContext = null!, MembersToGenerate = parameters, TestInformation = null,
                Type = DataGeneratorType.ClassParameters, TestSessionId = string.Empty,
                TestClassInstance = null, ClassInstanceArguments = null
            });
            await Assert.That(evaluation.AnyRowInfers).IsFalse();
            await Assert.That(evaluation.RowsByIndex[0]!.Single().InfersTypeArguments).IsFalse();
        }

        // An own-class TestDataRow<string[][]> spreads to a string[] constructor value.
        var (values, _) = TUnit.Core.Helpers.TestDataRowUnwrapper.UnwrapArray([new TestDataRow<string[][]>([["bad"]])]);
        await Assert.That(GenericClassRowPartition.MatchesConstructorRow(values, generated, [typeof(int), typeof(int)])).IsFalse();
    }

    public sealed class ArrayComparerConstructor<T, U>(IComparer<T[]> first, U second = default!);
    public sealed class MatrixComparerConstructor<T, U>(IComparer<T[,]> first, U second = default!);

    [Test]
    [Arguments(false, false)]
    [Arguments(true, false)]
    [Arguments(false, true)]
    [Arguments(true, true)]
    public async Task Optional_Partition_Preserves_Array_Supertype_Comparers(bool matrix, bool referenceElement)
    {
        var open = matrix ? typeof(MatrixComparerConstructor<,>) : typeof(ArrayComparerConstructor<,>);
        var element = referenceElement ? typeof(string) : typeof(int);
        var closed = referenceElement
            ? (matrix ? typeof(MatrixComparerConstructor<string, int>) : typeof(ArrayComparerConstructor<string, int>))
            : (matrix ? typeof(MatrixComparerConstructor<int, int>) : typeof(ArrayComparerConstructor<int, int>));
        var generated = ParameterMetadataFactory.ForConstructor(open, true,
            new ParameterMetadata(typeof(object)) { Name = "first", TypeInfo = new ConstructedGeneric(typeof(IComparer<>), [new ConcreteType(typeof(object))]), CachedIsOptional = false },
            new ParameterMetadata(typeof(object)) { Name = "second", TypeInfo = new GenericParameter(1, false), CachedIsOptional = true });
        var reflected = closed.GetConstructors()[0].GetParameters().Select(p => (IMemberMetadata)new ParameterMetadata(p.ParameterType)
        {
            Name = p.Name!, TypeInfo = new ConcreteType(p.ParameterType), ReflectionInfo = p
        }).ToArray();
        object[] candidates = [Comparer<object>.Default, Comparer<Array>.Default,
            Comparer<System.Collections.IEnumerable>.Default, Comparer<IEnumerable<int>>.Default,
            Comparer<IList<int>>.Default, Comparer<IReadOnlyList<int>>.Default,
            Comparer<ICollection<int>>.Default, Comparer<IReadOnlyCollection<int>>.Default,
            Comparer<IEnumerable<object>>.Default, Comparer<IList<object>>.Default,
            Comparer<IEnumerable<string>>.Default, Comparer<IList<string>>.Default, Comparer<string>.Default];
        foreach (var parameters in new IMemberMetadata[][] { generated, reflected })
        {
            var source = new MethodDataSourceAttribute("Rows") { Factory = _ => Rows() };
            var partition = GenericClassRowPartition.Create([element, typeof(int)], GenericClassRowFallback.AllRows, [source], 0);
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = null!, MembersToGenerate = parameters, TestInformation = null,
                Type = DataGeneratorType.ClassParameters, TestSessionId = string.Empty,
                TestClassInstance = null, ClassInstanceArguments = null
            };
            var rows = new List<object?[]?>();
            await foreach (var factory in partition[0].GetDataRowsAsync(metadata))
            {
                rows.Add(await factory());
            }
            var expectedType = closed.GetConstructors()[0].GetParameters()[0].ParameterType;
            var accepted = candidates.Where(expectedType.IsInstanceOfType).ToArray();
            await Assert.That(rows.Count).IsEqualTo(accepted.Length + 1);
            await Assert.That(rows[0]![0]).IsNull();
            for (var i = 0; i < accepted.Length; i++)
            {
                await Assert.That(ReferenceEquals(rows[i + 1]![0], accepted[i])).IsTrue();
            }
        }

        async IAsyncEnumerable<Func<Task<object?[]?>>> Rows()
        {
            yield return () => Task.FromResult<object?[]?>([null]);
            foreach (var candidate in candidates)
            {
                yield return () => Task.FromResult<object?[]?>([candidate]);
            }
            await Task.CompletedTask;
        }
    }

    [Test]
    [Arguments(true, false, false)]
    [Arguments(false, false, true)]
    [Arguments(false, true, false)]
    public async Task Omitted_Optional_Rows_Use_Reference_Type_Variance_Only(bool valueTypeElement, bool invariant, bool expectedMatch)
    {
        object first = valueTypeElement ? new List<int>() : new List<string>();
        IMemberMetadata[] parameters =
        [
            new ParameterMetadata(typeof(object))
            {
                Name = "first",
                TypeInfo = new ConstructedGeneric(invariant ? typeof(List<>) : typeof(IEnumerable<>), [new ConcreteType(typeof(object))]),
                CachedIsOptional = false
            },
            new ParameterMetadata(typeof(int))
            {
                Name = "second",
                TypeInfo = new ConcreteType(typeof(int)),
                CachedIsOptional = true
            }
        ];

        var matches = GenericClassRowPartition.MatchesConstructorRow([first], parameters, []);
        await Assert.That(matches).IsEqualTo(expectedMatch);

        // Reflection metadata contains the closed type, while generated metadata describes its structure.
        var closedType = invariant ? typeof(List<object>) : typeof(IEnumerable<object>);
        parameters[0] = new ParameterMetadata(closedType)
        {
            Name = "first",
            TypeInfo = new ConcreteType(closedType),
            CachedIsOptional = false
        };
        var reflectionMatches = GenericClassRowPartition.MatchesConstructorRow([first], parameters, []);
        await Assert.That(reflectionMatches).IsEqualTo(expectedMatch);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Optional_Partition_Preserves_Boxed_Nullable_And_Contravariant_Rows(bool nullable)
    {
        TypeInfo generated = nullable
            ? new ConstructedGeneric(typeof(Nullable<>), [new GenericParameter(0, false)])
            : new ConstructedGeneric(typeof(IComparer<>),
                [new ConstructedGeneric(typeof(IEnumerable<>), [new GenericParameter(0, false)])]);
        var closed = nullable ? typeof(int?) : typeof(IComparer<IEnumerable<int>>);
        object valid = nullable ? 1 : Comparer<object>.Default;
        object invalid = nullable ? "wrong" : Comparer<string>.Default;
        foreach (var description in new[] { generated, new ConcreteType(closed) })
        {
            IMemberMetadata[] parameters =
            [
                new ParameterMetadata(closed) { Name = "first", TypeInfo = description, CachedIsOptional = false },
                new ParameterMetadata(typeof(int)) { Name = "second", TypeInfo = new GenericParameter(1, false), CachedIsOptional = true }
            ];
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([valid], parameters, [typeof(int), typeof(int)])).IsTrue();
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([invalid], parameters, [typeof(int), typeof(int)])).IsFalse();
            var source = new MethodDataSourceAttribute("Rows") { Factory = _ => Rows() };
            var partition = GenericClassRowPartition.Create([typeof(int), typeof(int)], GenericClassRowFallback.AllRows, [source], 0);
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = null!, MembersToGenerate = parameters, TestInformation = null,
                Type = DataGeneratorType.ClassParameters, TestSessionId = string.Empty,
                TestClassInstance = null, ClassInstanceArguments = null
            };
            var rows = new List<object?[]?>();
            await foreach (var factory in partition[0].GetDataRowsAsync(metadata))
            {
                rows.Add(await factory());
            }
            await Assert.That(rows.Count).IsEqualTo(2);
            await Assert.That(rows[0]![0]).IsNull();
            await Assert.That(ReferenceEquals(rows[1]![0], valid)).IsTrue();
        }

        async IAsyncEnumerable<Func<Task<object?[]?>>> Rows()
        {
            yield return () => Task.FromResult<object?[]?>([null]);
            yield return () => Task.FromResult<object?[]?>([valid]);
            yield return () => Task.FromResult<object?[]?>([invalid]);
            await Task.CompletedTask;
        }
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Contravariance_Substitutes_Generic_Supertypes(bool inheritedInterface)
    {
        TypeInfo element = new GenericParameter(0, false);
        TypeInfo nested = new ConstructedGeneric(inheritedInterface ? typeof(IReadOnlyList<>) : typeof(List<>), [element]);
        var closed = inheritedInterface ? typeof(IComparer<IReadOnlyList<string>>) : typeof(IComparer<List<string>>);
        foreach (var description in new TypeInfo[] { new ConstructedGeneric(typeof(IComparer<>), [nested]), new ConcreteType(closed) })
        {
            IMemberMetadata[] parameters =
            [
                new ParameterMetadata(closed) { Name = "first", TypeInfo = description, CachedIsOptional = false },
                new ParameterMetadata(typeof(int)) { Name = "second", TypeInfo = new ConcreteType(typeof(int)), CachedIsOptional = true }
            ];
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([Comparer<IEnumerable<object>>.Default], parameters, [typeof(string)])).IsTrue();
            await Assert.That(GenericClassRowPartition.MatchesConstructorRow([Comparer<IEnumerable<int>>.Default], parameters, [typeof(string)])).IsFalse();
        }
    }

    [Test]
    public async Task Null_Evaluated_Row_Uses_Original_Factory()
    {
        var calls = 0;
        var state = new EvaluatedDataRow(null, () =>
        {
            calls++;
            return Task.FromResult<object?[]?>(null);
        });

        await Assert.That(await state.FetchAsync()).IsNull();
        await Assert.That(await state.FetchAsync()).IsNull();
        await Assert.That(calls).IsEqualTo(2);
    }

    [Test]
    public async Task Partitions_Consume_Shared_Evaluated_Row_Once_Across_Enumerations()
    {
        var original = new object?[] { new object() };
        var calls = 0;
        var state = new EvaluatedDataRow(original, () =>
        {
            Interlocked.Increment(ref calls);
            return Task.FromResult<object?[]?>([new object()]);
        });
        var raw = new StaticDataSourceAttribute(original) { EvaluatedRows = [state] };
        var grouped = new StaticDataSourceAttribute(original) { EvaluatedRows = [state] };

        var rawFactories = await ReadFactories(raw);
        var groupedFactories = await ReadFactories(grouped);
        var repeatedFactories = await ReadFactories(grouped);
        var rows = await Task.WhenAll(rawFactories[0](), groupedFactories[0](), repeatedFactories[0]());

        await Assert.That(rows.Count(row => ReferenceEquals(row, original))).IsEqualTo(1);
        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(ReferenceEquals(rows[1]![0], rows[2]![0])).IsFalse();
    }

    [Test]
    public async Task Instance_Conversion_Preserves_Inferred_Source_Settings()
    {
        var source = new MethodDataSourceAttribute(typeof(GenericClassSourceRuntimeTests), "Rows");
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(string));

        var converted = source.ToInstanceVariant();
        await Assert.That(converted.FilterClassRows).IsTrue();
        await Assert.That(converted.InferredClassTypeArguments).IsEquivalentTo(new[] { typeof(string) });
        await Assert.That(converted.SkipIfEmpty).IsFalse();

        // The converted external source must still discard an all-null inferred row.
        converted.Factory = _ => OneRow(() => Task.FromResult<object?[]?>([null]));
        await Assert.That((await ReadFactories(converted)).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 0)]
    public async Task Inferred_Null_Row_Is_Retained_Only_For_Own_Class(bool external, int expectedCount)
    {
        var source = external
            ? new MethodDataSourceAttribute(typeof(GenericClassSourceRuntimeTests), "Rows")
            : new MethodDataSourceAttribute("Rows");
        source.Factory = _ => OneRow(() => Task.FromResult<object?[]?>([null]));
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(string));

        var rows = await ReadFactories(source);
        await Assert.That(rows.Count).IsEqualTo(expectedCount);
        if (rows.Count > 0)
        {
            await Assert.That((await rows[0]())![0]).IsNull();
        }
    }

    [Test]
    public async Task Inferred_Filter_Reuses_First_Fetch_Then_Creates_Fresh_Objects()
    {
        var calls = 0;
        var source = new MethodDataSourceAttribute("Rows")
        {
            Factory = _ => OneRow(() =>
            {
                calls++;
                return Task.FromResult<object?[]?>([new object()]);
            })
        };
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(object));

        var factories = await ReadFactories(source);
        await Assert.That(calls).IsEqualTo(1);
        var first = await factories[0]();
        await Assert.That(calls).IsEqualTo(1);
        var second = await factories[0]();
        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(ReferenceEquals(first![0], second![0])).IsFalse();
    }

    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 0)]
    public async Task Empty_Inferred_Source_Has_No_Explicit_Fallback(bool inferred, int expectedCount)
    {
        var source = new MethodDataSourceAttribute("Rows") { Factory = _ => EmptyRows() };
        GenericClassRowPartition.ConfigureInferredSource(source, inferred, typeof(string));

        var factories = await ReadFactories(source);
        await Assert.That(factories.Count).IsEqualTo(expectedCount);
        if (factories.Count > 0)
        {
            await Assert.That((await factories[0]())!.Length).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Declared_Constructor_Mapping_Overrides_Leading_Runtime_Value()
    {
        var source = new ArgumentsAttribute(7, "constructor-value");
        GenericClassRowPartition.ConfigureInferredSource(source, false, typeof(string));
        var partition = GenericClassRowPartition.Create([typeof(string)], GenericClassRowFallback.None, [source], 0);
        var rows = await ReadFactories(partition[0]);
        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That((await rows[0]())![1]).IsEqualTo("constructor-value");
    }

    [Test]
    [Arguments(false, 0)]
    [Arguments(true, 1)]
    public async Task Constraint_Violations_Report_Only_Actual_Source_Rows(bool populated, int expectedCount)
    {
        var source = new MethodDataSourceAttribute("Rows")
        {
            Factory = _ => populated ? OneRow(() => Task.FromResult<object?[]?>(["invalid"])) : EmptyRows(),
        };
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(string));
        var entry = new GenericConstraintViolationEntry
        {
            MethodName = "Test", FilePath = "test.cs", LineNumber = 1,
            MethodMetadata = null!, Messages = ["constraint violated"],
            DataSources = [GenericConstraintViolationDataSource.Create("constraint violated", source)],
        };
        var metadata = entry.ToTestMetadata(typeof(ConstraintSubject<>), "session");
        var rows = await ReadFactories(metadata.DataSources.Single());
        await Assert.That(rows.Count).IsEqualTo(expectedCount);
        await Assert.That(metadata.DataSources.Single().SkipIfEmpty).IsFalse();
        if (populated)
        {
            await Assert.That(async () => { await rows[0](); }).Throws<Exception>();
        }
    }

    private class ConstraintSubject<T> where T : struct;

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> OneRow(Func<Task<object?[]?>> factory)
    {
        await Task.CompletedTask;
        yield return factory;
    }

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> EmptyRows()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async Task<List<Func<Task<object?[]?>>>> ReadFactories(IDataSourceAttribute source)
    {
        var factories = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in source.GetDataRowsAsync(new DataGeneratorMetadata
        {
            TestBuilderContext = null!, MembersToGenerate = [], TestInformation = null,
            Type = DataGeneratorType.ClassParameters, TestSessionId = string.Empty,
            TestClassInstance = null, ClassInstanceArguments = null
        }))
        {
            factories.Add(factory);
        }
        return factories;
    }
}
