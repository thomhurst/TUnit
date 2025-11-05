using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    internal string GetDisplayName()
    {
        if(!string.IsNullOrEmpty(CustomDisplayName))
        {
            return CustomDisplayName!;
        }

        if (_cachedDisplayName != null)
        {
            return _cachedDisplayName;
        }

        if (TestDetails.TestMethodArguments.Length == 0)
        {
            _cachedDisplayName = TestDetails.TestName;
            return TestDetails.TestName;
        }

        var argsLength = TestDetails.TestMethodArguments.Length;
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
                sb.Append(ArgumentFormatter.Format(TestDetails.TestMethodArguments[i], ArgumentDisplayFormatters));
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
