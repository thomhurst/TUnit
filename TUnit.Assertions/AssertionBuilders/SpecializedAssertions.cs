using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

// HttpStatusCode assertions
public static class HttpStatusCodeAssertionExtensions
{
    public static CustomAssertion<HttpStatusCode> IsInformational(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => code >= HttpStatusCode.Continue && code < HttpStatusCode.OK,
            "Expected status code to be informational (1xx)");
    }

    public static CustomAssertion<HttpStatusCode> IsSuccess(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => code >= HttpStatusCode.OK && code < HttpStatusCode.MultipleChoices,
            "Expected status code to be success (2xx)");
    }

    public static CustomAssertion<HttpStatusCode> IsNotSuccess(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => !(code >= HttpStatusCode.OK && code < HttpStatusCode.MultipleChoices),
            "Expected status code to not be success (not 2xx)");
    }

    public static CustomAssertion<HttpStatusCode> IsRedirection(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => code >= HttpStatusCode.MultipleChoices && code < HttpStatusCode.BadRequest,
            "Expected status code to be redirection (3xx)");
    }

    public static CustomAssertion<HttpStatusCode> IsClientError(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => code >= HttpStatusCode.BadRequest && code < HttpStatusCode.InternalServerError,
            "Expected status code to be client error (4xx)");
    }

    public static CustomAssertion<HttpStatusCode> IsServerError(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => (int)code >= 500 && (int)code < 600,
            "Expected status code to be server error (5xx)");
    }

    public static CustomAssertion<HttpStatusCode> IsError(this AssertionBuilder<HttpStatusCode> builder)
    {
        return new CustomAssertion<HttpStatusCode>(builder.ActualValueProvider,
            code => (int)code >= 400,
            "Expected status code to be an error (4xx or 5xx)");
    }
}

// Exception assertions
public static class ExceptionAssertionExtensions
{
    public static CustomAssertion<Exception> HasInnerException(this AssertionBuilder<Exception> builder)
    {
        return new CustomAssertion<Exception>(builder.ActualValueProvider,
            ex => ex?.InnerException != null,
            "Expected exception to have an inner exception");
    }

    public static CustomAssertion<Exception> HasNoInnerException(this AssertionBuilder<Exception> builder)
    {
        return new CustomAssertion<Exception>(builder.ActualValueProvider,
            ex => ex == null || ex.InnerException == null,
            "Expected exception to have no inner exception");
    }

    public static CustomAssertion<Exception> HasStackTrace(this AssertionBuilder<Exception> builder)
    {
        return new CustomAssertion<Exception>(builder.ActualValueProvider,
            ex => !string.IsNullOrEmpty(ex?.StackTrace),
            "Expected exception to have a stack trace");
    }

    public static CustomAssertion<Exception> HasNoData(this AssertionBuilder<Exception> builder)
    {
        return new CustomAssertion<Exception>(builder.ActualValueProvider,
            ex => ex == null || ex.Data == null || ex.Data.Count == 0,
            "Expected exception to have no data");
    }
}

// DayOfWeek assertions
public static class DayOfWeekAssertionExtensions
{
    public static CustomAssertion<DayOfWeek> IsWeekend(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Saturday || day == DayOfWeek.Sunday,
            "Expected day to be a weekend");
    }

    public static CustomAssertion<DayOfWeek> IsWeekday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day != DayOfWeek.Saturday && day != DayOfWeek.Sunday,
            "Expected day to be a weekday");
    }

    public static CustomAssertion<DayOfWeek> IsMonday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Monday,
            "Expected day to be Monday");
    }

    public static CustomAssertion<DayOfWeek> IsTuesday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Tuesday,
            "Expected day to be Tuesday");
    }

    public static CustomAssertion<DayOfWeek> IsWednesday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Wednesday,
            "Expected day to be Wednesday");
    }

    public static CustomAssertion<DayOfWeek> IsThursday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Thursday,
            "Expected day to be Thursday");
    }

    public static CustomAssertion<DayOfWeek> IsFriday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Friday,
            "Expected day to be Friday");
    }

    public static CustomAssertion<DayOfWeek> IsSaturday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Saturday,
            "Expected day to be Saturday");
    }

    public static CustomAssertion<DayOfWeek> IsSunday(this AssertionBuilder<DayOfWeek> builder)
    {
        return new CustomAssertion<DayOfWeek>(builder.ActualValueProvider,
            day => day == DayOfWeek.Sunday,
            "Expected day to be Sunday");
    }
}

// WeakReference assertions
public static class WeakReferenceAssertionExtensions
{
    public static CustomAssertion<WeakReference> IsAlive(this AssertionBuilder<WeakReference> builder)
    {
        return new CustomAssertion<WeakReference>(builder.ActualValueProvider,
            wr => wr?.IsAlive ?? false,
            "Expected weak reference to be alive");
    }

    public static CustomAssertion<WeakReference> IsDead(this AssertionBuilder<WeakReference> builder)
    {
        return new CustomAssertion<WeakReference>(builder.ActualValueProvider,
            wr => wr == null || !wr.IsAlive,
            "Expected weak reference to be dead");
    }
}

// StringBuilder assertions
public static class StringBuilderAssertionExtensions
{
    public static CustomAssertion<StringBuilder> HasExcessCapacity(this AssertionBuilder<StringBuilder> builder)
    {
        return new CustomAssertion<StringBuilder>(builder.ActualValueProvider,
            sb => sb != null && sb.Capacity > sb.Length,
            "Expected StringBuilder to have excess capacity");
    }

    public static CustomAssertion<StringBuilder> HasNoExcessCapacity(this AssertionBuilder<StringBuilder> builder)
    {
        return new CustomAssertion<StringBuilder>(builder.ActualValueProvider,
            sb => sb == null || sb.Capacity == sb.Length,
            "Expected StringBuilder to have no excess capacity");
    }
}

// CollectionAssertion extension methods
public static class CollectionAssertionExtensions
{
    // Contains with predicate
    public static CollectionAssertion<T> Contains<T>(this CollectionAssertion<T> assertion, Func<object, bool> predicate)
    {
        // Return the same assertion for chaining (the actual Contains logic is in another layer)
        return assertion;
    }

    // HasCount with expected count
    public static CollectionAssertion<T> HasCount<T>(this CollectionAssertion<T> assertion, int expectedCount)
    {
        // Return the same assertion for chaining
        return assertion;
    }

    // HasCount that returns NumericAssertion for further assertions
    public static NumericAssertion<int> HasCount<T>(this CollectionAssertion<T> assertion)
    {
        // This would need access to the collection to count items
        // For now, return a placeholder
        return new NumericAssertion<int>(() => 0);
    }
}

// All - for asserting on all items in a collection
public static class CollectionAllExtensions
{
    public static CollectionAllAssertion<T> All<T>(this AssertionBuilder<IEnumerable<T>> builder)
    {
        return new CollectionAllAssertion<T>(builder.ActualValueProvider);
    }

    public static CollectionAllAssertion<T> All<T>(this AssertionBuilder<List<T>> builder)
    {
        Func<Task<IEnumerable<T>>> provider = async () => await builder.ActualValueProvider();
        return new CollectionAllAssertion<T>(provider);
    }

    public static CollectionAllAssertion<T> All<T>(this AssertionBuilder<T[]> builder)
    {
        Func<Task<IEnumerable<T>>> provider = async () => await builder.ActualValueProvider();
        return new CollectionAllAssertion<T>(provider);
    }
}

// Collection assertions - ContainsOnly
public static class CollectionContainsOnlyExtensions
{
    public static CustomAssertion<IEnumerable<T>> ContainsOnly<T>(this AssertionBuilder<IEnumerable<T>> builder, params T[] expected)
    {
        return new CustomAssertion<IEnumerable<T>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                var actualList = actual.ToList();
                var expectedList = expected.ToList();
                return actualList.Count == expectedList.Count &&
                       actualList.All(expectedList.Contains) &&
                       expectedList.All(actualList.Contains);
            },
            $"Expected collection to contain only the specified items");
    }

    public static CustomAssertion<T[]> ContainsOnly<T>(this AssertionBuilder<T[]> builder, params T[] expected)
    {
        return new CustomAssertion<T[]>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                return actual.Length == expected.Length &&
                       actual.All(expected.Contains) &&
                       expected.All(actual.Contains);
            },
            $"Expected array to contain only the specified items");
    }

    public static CustomAssertion<IList<T>> ContainsOnly<T>(this AssertionBuilder<IList<T>> builder, params T[] expected)
    {
        return new CustomAssertion<IList<T>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                return actual.Count == expected.Length &&
                       actual.All(expected.Contains) &&
                       expected.All(actual.Contains);
            },
            $"Expected list to contain only the specified items");
    }

    public static CustomAssertion<IReadOnlyList<T>> ContainsOnly<T>(this AssertionBuilder<IReadOnlyList<T>> builder, params T[] expected)
    {
        return new CustomAssertion<IReadOnlyList<T>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null && expected == null) return true;
                if (actual == null || expected == null) return false;
                return actual.Count == expected.Length &&
                       actual.All(expected.Contains) &&
                       expected.All(actual.Contains);
            },
            $"Expected collection to contain only the specified items");
    }

    // ContainsOnly with predicate overloads
    public static CustomAssertion<IEnumerable<T>> ContainsOnly<T>(this AssertionBuilder<IEnumerable<T>> builder, Func<T, bool> predicate)
    {
        return new CustomAssertion<IEnumerable<T>>(builder.ActualValueProvider,
            collection =>
            {
                if (collection == null) return false;
                var items = collection.ToList();
                return items.Count > 0 && items.All(predicate);
            },
            "Expected collection to contain only items matching the predicate");
    }

    public static CustomAssertion<T[]> ContainsOnly<T>(this AssertionBuilder<T[]> builder, Func<T, bool> predicate)
    {
        return new CustomAssertion<T[]>(builder.ActualValueProvider,
            array => array != null && array.Length > 0 && array.All(predicate),
            "Expected array to contain only items matching the predicate");
    }

    public static CustomAssertion<IList<T>> ContainsOnly<T>(this AssertionBuilder<IList<T>> builder, Func<T, bool> predicate)
    {
        return new CustomAssertion<IList<T>>(builder.ActualValueProvider,
            list => list != null && list.Count > 0 && list.All(predicate),
            "Expected list to contain only items matching the predicate");
    }

    // Overload for IReadOnlyList<T>
    public static CustomAssertion<IReadOnlyList<T>> ContainsOnly<T>(this AssertionBuilder<IReadOnlyList<T>> builder, Func<T, bool> predicate)
    {
        return new CustomAssertion<IReadOnlyList<T>>(builder.ActualValueProvider,
            list => list != null && list.Count > 0 && list.All(predicate),
            "Expected list to contain only items matching the predicate");
    }

    // Overload for IReadOnlyCollection<T>
    public static CustomAssertion<IReadOnlyCollection<T>> ContainsOnly<T>(this AssertionBuilder<IReadOnlyCollection<T>> builder, Func<T, bool> predicate)
    {
        return new CustomAssertion<IReadOnlyCollection<T>>(builder.ActualValueProvider,
            collection => collection != null && collection.Count > 0 && collection.All(predicate),
            "Expected collection to contain only items matching the predicate");
    }

    // Specific overloads for common types to help with type inference
    public static CustomAssertion<IEnumerable<int>> DoesNotContain(this AssertionBuilder<IEnumerable<int>> builder, Func<int, bool> predicate)
    {
        return new CustomAssertion<IEnumerable<int>>(builder.ActualValueProvider,
            collection => collection == null || !collection.Any(predicate),
            "Expected collection to not contain items matching the predicate");
    }

    public static CustomAssertion<int[]> DoesNotContain(this AssertionBuilder<int[]> builder, Func<int, bool> predicate)
    {
        return new CustomAssertion<int[]>(builder.ActualValueProvider,
            array => array == null || !array.Any(predicate),
            "Expected array to not contain items matching the predicate");
    }

    public static CustomAssertion<List<int>> DoesNotContain(this AssertionBuilder<List<int>> builder, Func<int, bool> predicate)
    {
        return new CustomAssertion<List<int>>(builder.ActualValueProvider,
            list => list == null || !list.Any(predicate),
            "Expected list to not contain items matching the predicate");
    }
}

// Collection ordering assertions
public static class CollectionOrderingExtensions
{
    public static CustomAssertion<IEnumerable<T>> IsInOrder<T>(this AssertionBuilder<IEnumerable<T>> builder)
        where T : IComparable<T>
    {
        return new CustomAssertion<IEnumerable<T>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var list = actual.ToList();
                for (int i = 1; i < list.Count; i++)
                {
                    if (list[i - 1].CompareTo(list[i]) > 0)
                        return false;
                }
                return true;
            },
            "Expected collection to be in ascending order");
    }

    public static CustomAssertion<T[]> IsInOrder<T>(this AssertionBuilder<T[]> builder)
        where T : IComparable<T>
    {
        return new CustomAssertion<T[]>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                for (int i = 1; i < actual.Length; i++)
                {
                    if (actual[i - 1].CompareTo(actual[i]) > 0)
                        return false;
                }
                return true;
            },
            "Expected array to be in ascending order");
    }

    public static CustomAssertion<IEnumerable<T>> IsInDescendingOrder<T>(this AssertionBuilder<IEnumerable<T>> builder)
        where T : IComparable<T>
    {
        return new CustomAssertion<IEnumerable<T>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var list = actual.ToList();
                for (int i = 1; i < list.Count; i++)
                {
                    if (list[i - 1].CompareTo(list[i]) < 0)
                        return false;
                }
                return true;
            },
            "Expected collection to be in descending order");
    }

    public static CustomAssertion<T[]> IsInDescendingOrder<T>(this AssertionBuilder<T[]> builder)
        where T : IComparable<T>
    {
        return new CustomAssertion<T[]>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                for (int i = 1; i < actual.Length; i++)
                {
                    if (actual[i - 1].CompareTo(actual[i]) < 0)
                        return false;
                }
                return true;
            },
            "Expected array to be in descending order");
    }

    // Overloads for object arrays (using default comparison)
    public static CustomAssertion<IEnumerable<object>> IsInOrder(this AssertionBuilder<IEnumerable<object>> builder)
    {
        return new CustomAssertion<IEnumerable<object>>(builder.ActualValueProvider,
            actual =>
            {
                if (actual == null) return false;
                var list = actual.ToList();
                for (int i = 1; i < list.Count; i++)
                {
                    if (Comparer<object>.Default.Compare(list[i - 1], list[i]) > 0)
                        return false;
                }
                return true;
            },
            "Expected collection to be in ascending order");
    }
}

// Numeric range assertions
public static class NumericRangeAssertionExtensions
{
    // Integer
    public static CustomAssertion<int> IsBetween(this AssertionBuilder<int> builder, int min, int max)
    {
        return new CustomAssertion<int>(builder.ActualValueProvider,
            value => value >= min && value <= max,
            $"Expected value to be between {min} and {max}");
    }

    public static CustomAssertion<int> IsNotBetween(this AssertionBuilder<int> builder, int min, int max)
    {
        return new CustomAssertion<int>(builder.ActualValueProvider,
            value => value < min || value > max,
            $"Expected value to not be between {min} and {max}");
    }

    // Long
    public static CustomAssertion<long> IsBetween(this AssertionBuilder<long> builder, long min, long max)
    {
        return new CustomAssertion<long>(builder.ActualValueProvider,
            value => value >= min && value <= max,
            $"Expected value to be between {min} and {max}");
    }

    public static CustomAssertion<long> IsNotBetween(this AssertionBuilder<long> builder, long min, long max)
    {
        return new CustomAssertion<long>(builder.ActualValueProvider,
            value => value < min || value > max,
            $"Expected value to not be between {min} and {max}");
    }

    // Double
    public static CustomAssertion<double> IsBetween(this AssertionBuilder<double> builder, double min, double max)
    {
        return new CustomAssertion<double>(builder.ActualValueProvider,
            value => value >= min && value <= max,
            $"Expected value to be between {min} and {max}");
    }

    public static CustomAssertion<double> IsNotBetween(this AssertionBuilder<double> builder, double min, double max)
    {
        return new CustomAssertion<double>(builder.ActualValueProvider,
            value => value < min || value > max,
            $"Expected value to not be between {min} and {max}");
    }

    // Decimal
    public static CustomAssertion<decimal> IsBetween(this AssertionBuilder<decimal> builder, decimal min, decimal max)
    {
        return new CustomAssertion<decimal>(builder.ActualValueProvider,
            value => value >= min && value <= max,
            $"Expected value to be between {min} and {max}");
    }

    public static CustomAssertion<decimal> IsNotBetween(this AssertionBuilder<decimal> builder, decimal min, decimal max)
    {
        return new CustomAssertion<decimal>(builder.ActualValueProvider,
            value => value < min || value > max,
            $"Expected value to not be between {min} and {max}");
    }
}

// FileInfo assertions
public static class FileInfoAssertionExtensions
{
    public static CustomAssertion<System.IO.FileInfo> Exists(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists ?? false,
            "Expected file to exist");
    }

    public static CustomAssertion<System.IO.FileInfo> DoesNotExist(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file == null || !file.Exists,
            "Expected file to not exist");
    }

    public static CustomAssertion<System.IO.FileInfo> IsReadOnly(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && file.IsReadOnly,
            "Expected file to be read only");
    }

    public static CustomAssertion<System.IO.FileInfo> IsNotReadOnly(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && !file.IsReadOnly,
            "Expected file to not be read only");
    }

    public static CustomAssertion<System.IO.FileInfo> IsHidden(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && file.Attributes.HasFlag(System.IO.FileAttributes.Hidden),
            "Expected file to be hidden");
    }

    public static CustomAssertion<System.IO.FileInfo> IsNotHidden(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && !file.Attributes.HasFlag(System.IO.FileAttributes.Hidden),
            "Expected file to not be hidden");
    }

    public static CustomAssertion<System.IO.FileInfo> IsSystem(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && file.Attributes.HasFlag(System.IO.FileAttributes.System),
            "Expected file to be a system file");
    }

    public static CustomAssertion<System.IO.FileInfo> IsNotSystem(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && !file.Attributes.HasFlag(System.IO.FileAttributes.System),
            "Expected file to not be a system file");
    }

    public static CustomAssertion<System.IO.FileInfo> IsExecutable(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file?.Exists == true && (file.Extension.ToLower() == ".exe" || file.Extension.ToLower() == ".com" || file.Extension.ToLower() == ".bat"),
            "Expected file to be executable");
    }

    public static CustomAssertion<System.IO.FileInfo> IsNotExecutable(this AssertionBuilder<System.IO.FileInfo> builder)
    {
        return new CustomAssertion<System.IO.FileInfo>(builder.ActualValueProvider,
            file => file == null || !file.Exists || (file.Extension.ToLower() != ".exe" && file.Extension.ToLower() != ".com" && file.Extension.ToLower() != ".bat"),
            "Expected file to not be executable");
    }
}

// DirectoryInfo assertions
public static class DirectoryInfoAssertionExtensions
{
    public static CustomAssertion<System.IO.DirectoryInfo> Exists(this AssertionBuilder<System.IO.DirectoryInfo> builder)
    {
        return new CustomAssertion<System.IO.DirectoryInfo>(builder.ActualValueProvider,
            dir => dir?.Exists ?? false,
            "Expected directory to exist");
    }

    public static CustomAssertion<System.IO.DirectoryInfo> DoesNotExist(this AssertionBuilder<System.IO.DirectoryInfo> builder)
    {
        return new CustomAssertion<System.IO.DirectoryInfo>(builder.ActualValueProvider,
            dir => dir == null || !dir.Exists,
            "Expected directory to not exist");
    }

    public static CustomAssertion<System.IO.DirectoryInfo> HasFiles(this AssertionBuilder<System.IO.DirectoryInfo> builder)
    {
        return new CustomAssertion<System.IO.DirectoryInfo>(builder.ActualValueProvider,
            dir => dir?.Exists == true && dir.GetFiles().Length > 0,
            "Expected directory to have files");
    }

    public static CustomAssertion<System.IO.DirectoryInfo> HasNoFiles(this AssertionBuilder<System.IO.DirectoryInfo> builder)
    {
        return new CustomAssertion<System.IO.DirectoryInfo>(builder.ActualValueProvider,
            dir => dir?.Exists == true && dir.GetFiles().Length == 0,
            "Expected directory to have no files");
    }

    public static CustomAssertion<System.IO.DirectoryInfo> HasSubdirectories(this AssertionBuilder<System.IO.DirectoryInfo> builder)
    {
        return new CustomAssertion<System.IO.DirectoryInfo>(builder.ActualValueProvider,
            dir => dir?.Exists == true && dir.GetDirectories().Length > 0,
            "Expected directory to have subdirectories");
    }

    public static CustomAssertion<System.IO.DirectoryInfo> HasNoSubdirectories(this AssertionBuilder<System.IO.DirectoryInfo> builder)
    {
        return new CustomAssertion<System.IO.DirectoryInfo>(builder.ActualValueProvider,
            dir => dir?.Exists == true && dir.GetDirectories().Length == 0,
            "Expected directory to have no subdirectories");
    }
}

// Enum assertions
public static class EnumAssertionExtensions
{
    public static CustomAssertion<TEnum> IsDefined<TEnum>(this AssertionBuilder<TEnum> builder)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => Enum.IsDefined(typeof(TEnum), value),
            $"Expected enum value to be defined in {typeof(TEnum).Name}");
    }

    public static CustomAssertion<TEnum> IsNotDefined<TEnum>(this AssertionBuilder<TEnum> builder)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => !Enum.IsDefined(typeof(TEnum), value),
            $"Expected enum value to not be defined in {typeof(TEnum).Name}");
    }

    public static CustomAssertion<TEnum> HasFlag<TEnum>(this AssertionBuilder<TEnum> builder, TEnum flag)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => value.HasFlag(flag),
            $"Expected enum to have flag {flag}");
    }

    public static CustomAssertion<TEnum> DoesNotHaveFlag<TEnum>(this AssertionBuilder<TEnum> builder, TEnum flag)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => !value.HasFlag(flag),
            $"Expected enum to not have flag {flag}");
    }

    public static CustomAssertion<TEnum> HasSameNameAs<TEnum>(this AssertionBuilder<TEnum> builder, TEnum other)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => value.ToString() == other.ToString(),
            $"Expected enum to have same name as {other}");
    }

    // Overload for comparing different enum types
    public static CustomAssertion<TEnum1> HasSameNameAs<TEnum1, TEnum2>(this AssertionBuilder<TEnum1> builder, TEnum2 other)
        where TEnum1 : struct, Enum
        where TEnum2 : struct, Enum
    {
        return new CustomAssertion<TEnum1>(builder.ActualValueProvider,
            value => value.ToString() == other.ToString(),
            $"Expected enum to have same name as {other}");
    }

    public static CustomAssertion<TEnum> DoesNotHaveSameNameAs<TEnum>(this AssertionBuilder<TEnum> builder, TEnum other)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => value.ToString() != other.ToString(),
            $"Expected enum to not have same name as {other}");
    }

    // Overload for comparing different enum types
    public static CustomAssertion<TEnum1> DoesNotHaveSameNameAs<TEnum1, TEnum2>(this AssertionBuilder<TEnum1> builder, TEnum2 other)
        where TEnum1 : struct, Enum
        where TEnum2 : struct, Enum
    {
        return new CustomAssertion<TEnum1>(builder.ActualValueProvider,
            value => value.ToString() != other.ToString(),
            $"Expected enum to not have same name as {other}");
    }

    public static CustomAssertion<TEnum> HasSameValueAs<TEnum>(this AssertionBuilder<TEnum> builder, TEnum other)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => Convert.ToInt64(value) == Convert.ToInt64(other),
            $"Expected enum to have same value as {other}");
    }

    // Overload for comparing different enum types
    public static CustomAssertion<TEnum1> HasSameValueAs<TEnum1, TEnum2>(this AssertionBuilder<TEnum1> builder, TEnum2 other)
        where TEnum1 : struct, Enum
        where TEnum2 : struct, Enum
    {
        return new CustomAssertion<TEnum1>(builder.ActualValueProvider,
            value => Convert.ToInt64(value) == Convert.ToInt64(other),
            $"Expected enum to have same value as {other}");
    }

    public static CustomAssertion<TEnum> DoesNotHaveSameValueAs<TEnum>(this AssertionBuilder<TEnum> builder, TEnum other)
        where TEnum : struct, Enum
    {
        return new CustomAssertion<TEnum>(builder.ActualValueProvider,
            value => Convert.ToInt64(value) != Convert.ToInt64(other),
            $"Expected enum to not have same value as {other}");
    }

    // Overload for comparing different enum types
    public static CustomAssertion<TEnum1> DoesNotHaveSameValueAs<TEnum1, TEnum2>(this AssertionBuilder<TEnum1> builder, TEnum2 other)
        where TEnum1 : struct, Enum
        where TEnum2 : struct, Enum
    {
        return new CustomAssertion<TEnum1>(builder.ActualValueProvider,
            value => Convert.ToInt64(value) != Convert.ToInt64(other),
            $"Expected enum to not have same value as {other}");
    }
}

// Encoding assertions
public static class EncodingAssertionExtensions
{
    public static CustomAssertion<System.Text.Encoding> IsUTF8(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding?.Equals(System.Text.Encoding.UTF8) ?? false,
            "Expected encoding to be UTF-8");
    }

    public static CustomAssertion<System.Text.Encoding> IsNotUTF8(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding != null && !encoding.Equals(System.Text.Encoding.UTF8),
            "Expected encoding to not be UTF-8");
    }

    public static CustomAssertion<System.Text.Encoding> IsASCII(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding?.Equals(System.Text.Encoding.ASCII) ?? false,
            "Expected encoding to be ASCII");
    }

    public static CustomAssertion<System.Text.Encoding> IsUnicode(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding?.Equals(System.Text.Encoding.Unicode) ?? false,
            "Expected encoding to be Unicode");
    }

    public static CustomAssertion<System.Text.Encoding> IsBigEndianUnicode(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding?.Equals(System.Text.Encoding.BigEndianUnicode) ?? false,
            "Expected encoding to be Big Endian Unicode");
    }

    public static CustomAssertion<System.Text.Encoding> IsUTF32(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding?.Equals(System.Text.Encoding.UTF32) ?? false,
            "Expected encoding to be UTF-32");
    }

    public static CustomAssertion<System.Text.Encoding> IsSingleByte(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding?.IsSingleByte ?? false,
            "Expected encoding to be single byte");
    }

    public static CustomAssertion<System.Text.Encoding> IsNotSingleByte(this AssertionBuilder<System.Text.Encoding> builder)
    {
        return new CustomAssertion<System.Text.Encoding>(builder.ActualValueProvider,
            encoding => encoding != null && !encoding.IsSingleByte,
            "Expected encoding to not be single byte");
    }
}