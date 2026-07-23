using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    internal string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(CustomDisplayName))
        {
            return CustomDisplayName!;
        }

        if (_cachedDisplayName != null)
        {
            return _cachedDisplayName;
        }

        // Check for data source display name (from TestDataRow or ArgumentsAttribute.DisplayName)
        if (!string.IsNullOrEmpty(DataSourceDisplayName))
        {
            _cachedDisplayName = DisplayNameSubstitutor.Substitute(
                DataSourceDisplayName!,
                TestDetails.MethodMetadata.Parameters,
                TestDetails.TestMethodArguments,
                ArgumentDisplayFormatters);
            return _cachedDisplayName;
        }

        // Use expression-based display format when no explicit DisplayName was provided
        if (!string.IsNullOrEmpty(DataSourceExpression))
        {
            _cachedDisplayName = $"{TestDetails.TestName}({DataSourceExpression})";
            return _cachedDisplayName;
        }

        if (TestDetails.TestMethodArguments.Length == 0)
        {
            _cachedDisplayName = TestDetails.TestName;
            return TestDetails.TestName;
        }

        var argsLength = TestDetails.TestMethodArguments.Length;
        var parameters = TestDetails.MethodMetadata.Parameters;
        var sb = StringBuilderPool.Get();
        try
        {
            sb.Append(TestDetails.TestName);
            sb.Append('(');

            for (var i = 0; i < argsLength; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                var parameterType = i < parameters.Length ? parameters[i].Type : null;
                sb.Append(ArgumentFormatter.Format(TestDetails.TestMethodArguments[i], parameterType, ArgumentDisplayFormatters));
            }

            sb.Append(')');
            _cachedDisplayName = sb.ToString();
            return _cachedDisplayName;
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    internal void InvalidateDisplayNameCache()
    {
        _cachedDisplayName = null;
    }

    /// <summary>
    /// Gets the data source attribute instance that generated this test's class (constructor) arguments,
    /// e.g. a <c>[ClassDataSource&lt;T&gt;]</c> applied to the test class. Never null — returns
    /// <see cref="NoDataSource.Instance"/> when the test class has no constructor data source.
    /// </summary>
    public IDataSourceAttribute ClassDataSource => _testBuilderContext.ClassDataSourceAttribute ?? NoDataSource.Instance;

    /// <summary>
    /// Gets the data source attribute instance that generated this test's method arguments,
    /// e.g. an <c>[Arguments]</c>, <c>[MethodDataSource]</c> or <c>[ClassDataSource&lt;T&gt;]</c> applied
    /// to the test method. Never null — returns <see cref="NoDataSource.Instance"/> when the test
    /// method has no data source.
    /// </summary>
    public IDataSourceAttribute MethodDataSource => _testBuilderContext.DataSourceAttribute ?? NoDataSource.Instance;

    string ITestMetadata.DefinitionId => _testBuilderContext.DefinitionId;
    TestDetails ITestMetadata.TestDetails
    {
        get => TestDetails;
        set => TestDetails = value;
    }

    string ITestMetadata.TestName => TestDetails.TestName;

    string ITestMetadata.DisplayName
    {
        get => GetDisplayName();
        set => CustomDisplayName = value;
    }

    Type? ITestMetadata.DisplayNameFormatter
    {
        get => DisplayNameFormatter;
        set => DisplayNameFormatter = value;
    }
}
