using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Xml;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine.Xml;

internal static class JUnitXmlWriter
{
    /// <summary>
    /// Sanitizes a string to be XML-safe by removing invalid XML characters.
    /// According to XML 1.0 spec, valid characters are:
    /// #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
    /// </summary>
    private static string SanitizeForXml(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // At this point, value is guaranteed to not be null
        var builder = new StringBuilder(value!.Length);
        foreach (var ch in value!)
        {
            // Check if character is valid according to XML 1.0 spec
            if (ch == 0x9 || ch == 0xA || ch == 0xD ||
                (ch >= 0x20 && ch <= 0xD7FF) ||
                (ch >= 0xE000 && ch <= 0xFFFD))
            {
                builder.Append(ch);
            }
            else
            {
                // Replace invalid character with its hex representation
                builder.Append($"[0x{((int)ch):X}]");
            }
        }

        return builder.ToString();
    }

    public static string GenerateXml(
        IEnumerable<TestNodeUpdateMessage> testUpdates,
        string? filter)
    {
        // Get the last state for each test
        var lastStates = GetLastStates(testUpdates);

        if (lastStates.Count == 0)
        {
            return string.Empty;
        }

        // Calculate summary statistics
        var summary = CalculateSummary(lastStates.Values);

        // Get assembly and framework information
        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ?? "TestResults";
        var targetFramework = Assembly.GetExecutingAssembly()
                .GetCustomAttributes<TargetFrameworkAttribute>()
                .SingleOrDefault()
                ?.FrameworkDisplayName
            ?? RuntimeInformation.FrameworkDescription;

        using var stringWriter = new Utf8StringWriter();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
            NewLineOnAttributes = false
        };

        using var xmlWriter = XmlWriter.Create(stringWriter, settings);

        // Write XML structure
        xmlWriter.WriteStartDocument();

        // <testsuites>
        xmlWriter.WriteStartElement("testsuites");
        xmlWriter.WriteAttributeString("name", SanitizeForXml(assemblyName));
        xmlWriter.WriteAttributeString("tests", summary.Total.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteAttributeString("failures", summary.Failures.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteAttributeString("errors", summary.Errors.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteAttributeString("skipped", summary.Skipped.ToString(CultureInfo.InvariantCulture));
        xmlWriter.WriteAttributeString("time", summary.TotalTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));
        xmlWriter.WriteAttributeString("timestamp", summary.Timestamp.ToString("o", CultureInfo.InvariantCulture));

        // Write test suite
        WriteTestSuite(xmlWriter, lastStates.Values, assemblyName, targetFramework, summary, filter);

        xmlWriter.WriteEndElement(); // testsuites
        xmlWriter.WriteEndDocument();
        xmlWriter.Flush();
        return stringWriter.ToString();
    }

    private static void WriteTestSuite(
        XmlWriter writer,
        IEnumerable<TestNodeUpdateMessage> tests,
        string assemblyName,
        string targetFramework,
        TestSummary summary,
        string? filter)
    {
        writer.WriteStartElement("testsuite");
        writer.WriteAttributeString("name", SanitizeForXml(assemblyName));
        writer.WriteAttributeString("tests", summary.Total.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("failures", summary.Failures.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("errors", summary.Errors.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("skipped", summary.Skipped.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("time", summary.TotalTime.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));
        writer.WriteAttributeString("timestamp", summary.Timestamp.ToString("o", CultureInfo.InvariantCulture));
        writer.WriteAttributeString("hostname", SanitizeForXml(Environment.MachineName));

        // Write properties
        WriteProperties(writer, targetFramework, filter);

        // Write test cases
        foreach (var test in tests)
        {
            WriteTestCase(writer, test);
        }

        writer.WriteEndElement(); // testsuite
    }

    private static void WriteProperties(XmlWriter writer, string targetFramework, string? filter)
    {
        writer.WriteStartElement("properties");

        writer.WriteStartElement("property");
        writer.WriteAttributeString("name", "framework");
        writer.WriteAttributeString("value", SanitizeForXml(targetFramework));
        writer.WriteEndElement();

        writer.WriteStartElement("property");
        writer.WriteAttributeString("name", "platform");
        writer.WriteAttributeString("value", SanitizeForXml(RuntimeInformation.OSDescription));
        writer.WriteEndElement();

        writer.WriteStartElement("property");
        writer.WriteAttributeString("name", "runtime");
        writer.WriteAttributeString("value", SanitizeForXml(RuntimeInformation.FrameworkDescription));
        writer.WriteEndElement();

        if (!string.IsNullOrEmpty(filter))
        {
            writer.WriteStartElement("property");
            writer.WriteAttributeString("name", "filter");
            writer.WriteAttributeString("value", SanitizeForXml(filter));
            writer.WriteEndElement();
        }

        writer.WriteEndElement(); // properties
    }

    private static void WriteTestCase(XmlWriter writer, TestNodeUpdateMessage test)
    {
        var testNode = test.TestNode;

        // Get test state
        var stateProperty = testNode.Properties.AsEnumerable()
            .OfType<TestNodeStateProperty>()
            .FirstOrDefault();

        // Get timing
        var timingProperty = testNode.Properties.AsEnumerable()
            .OfType<TimingProperty>()
            .FirstOrDefault();

        var duration = timingProperty?.GlobalTiming.Duration is { } d ? d : TimeSpan.Zero;

        // Get class and method names
        var testMethodIdentifier = testNode.Properties.AsEnumerable()
            .OfType<TestMethodIdentifierProperty>()
            .FirstOrDefault();

        var className = testMethodIdentifier?.TypeName ?? "UnknownClass";
        var testName = testNode.DisplayName;

        // Write testcase element
        writer.WriteStartElement("testcase");
        writer.WriteAttributeString("name", SanitizeForXml(testName));
        writer.WriteAttributeString("classname", SanitizeForXml(className));
        writer.WriteAttributeString("time", duration.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));

        // Write state-specific child elements
        switch (stateProperty)
        {
            case FailedTestNodeStateProperty failed:
                WriteFailure(writer, failed);
                break;

            case ErrorTestNodeStateProperty error:
                WriteError(writer, error);
                break;

            case TimeoutTestNodeStateProperty timeout:
                WriteTimeoutError(writer, timeout);
                break;

            case CancelledTestNodeStateProperty:
                WriteCancellationError(writer);
                break;

            case SkippedTestNodeStateProperty skipped:
                WriteSkipped(writer, skipped);
                break;

            case InProgressTestNodeStateProperty:
                WriteInProgressError(writer);
                break;

            case PassedTestNodeStateProperty:
                // No child element for passed tests
                break;
        }

        writer.WriteEndElement(); // testcase
    }

    private static void WriteFailure(XmlWriter writer, FailedTestNodeStateProperty failed)
    {
        writer.WriteStartElement("failure");

        var exception = failed.Exception;
        if (exception != null)
        {
            writer.WriteAttributeString("message", SanitizeForXml(exception.Message));
            writer.WriteAttributeString("type", exception.GetType().FullName ?? "AssertionException");
            writer.WriteString(SanitizeForXml(exception.ToString()));
        }
        else
        {
            var message = failed.Explanation ?? "Test failed";
            writer.WriteAttributeString("message", SanitizeForXml(message));
            writer.WriteAttributeString("type", "TestFailedException");
            writer.WriteString(SanitizeForXml(message));
        }

        writer.WriteEndElement(); // failure
    }

    private static void WriteError(XmlWriter writer, ErrorTestNodeStateProperty error)
    {
        writer.WriteStartElement("error");

        var exception = error.Exception;
        if (exception != null)
        {
            writer.WriteAttributeString("message", SanitizeForXml(exception.Message));
            writer.WriteAttributeString("type", exception.GetType().FullName ?? "Exception");
            writer.WriteString(SanitizeForXml(exception.ToString()));
        }
        else
        {
            var message = error.Explanation ?? "Test error";
            writer.WriteAttributeString("message", SanitizeForXml(message));
            writer.WriteAttributeString("type", "TestErrorException");
            writer.WriteString(SanitizeForXml(message));
        }

        writer.WriteEndElement(); // error
    }

    private static void WriteTimeoutError(XmlWriter writer, TimeoutTestNodeStateProperty timeout)
    {
        writer.WriteStartElement("error");
        var message = timeout.Explanation ?? "Test timed out";
        writer.WriteAttributeString("message", SanitizeForXml(message));
        writer.WriteAttributeString("type", "TimeoutException");
        writer.WriteString(SanitizeForXml(message));
        writer.WriteEndElement(); // error
    }

    private static void WriteCancellationError(XmlWriter writer)
    {
        writer.WriteStartElement("error");
        writer.WriteAttributeString("message", "Test was cancelled");
        writer.WriteAttributeString("type", "CancelledException");
        writer.WriteString("Test was cancelled");
        writer.WriteEndElement(); // error
    }

    private static void WriteInProgressError(XmlWriter writer)
    {
        writer.WriteStartElement("error");
        writer.WriteAttributeString("message", "Test never finished");
        writer.WriteAttributeString("type", "InProgressException");
        writer.WriteString("Test never finished");
        writer.WriteEndElement(); // error
    }

    private static void WriteSkipped(XmlWriter writer, SkippedTestNodeStateProperty skipped)
    {
        writer.WriteStartElement("skipped");
        var message = skipped.Explanation ?? "Test skipped";
        writer.WriteAttributeString("message", SanitizeForXml(message));
        writer.WriteString(SanitizeForXml(message));
        writer.WriteEndElement(); // skipped
    }

    private static Dictionary<string, TestNodeUpdateMessage> GetLastStates(
        IEnumerable<TestNodeUpdateMessage> tests)
    {
        var lastStates = new Dictionary<string, TestNodeUpdateMessage>();

        foreach (var test in tests)
        {
            lastStates[test.TestNode.Uid.Value] = test;
        }

        return lastStates;
    }

    private static TestSummary CalculateSummary(IEnumerable<TestNodeUpdateMessage> tests)
    {
        DateTimeOffset? earliestStartTime = null;

        var summary = new TestSummary();

        foreach (var test in tests)
        {
            summary.Total++;

            var stateProperty = test.TestNode.Properties.AsEnumerable()
                .OfType<TestNodeStateProperty>()
                .FirstOrDefault();

            var timing = test.TestNode.Properties.AsEnumerable()
                .OfType<TimingProperty>()
                .FirstOrDefault();

            if (timing?.GlobalTiming.Duration is { } durationValue)
            {
                summary.TotalTime += durationValue;

                // Track the earliest start time from actual test execution
                if (timing.GlobalTiming.StartTime is { } startTime)
                {
                    if (earliestStartTime is null || startTime < earliestStartTime)
                    {
                        earliestStartTime = startTime;
                    }
                }
            }

            switch (stateProperty)
            {
                case FailedTestNodeStateProperty:
                    summary.Failures++;
                    break;
                case ErrorTestNodeStateProperty:
                case TimeoutTestNodeStateProperty:
                case CancelledTestNodeStateProperty:
                case InProgressTestNodeStateProperty:
                    summary.Errors++;
                    break;
                case SkippedTestNodeStateProperty:
                    summary.Skipped++;
                    break;
            }
        }

        // Use earliest test start time, fallback to current time if no timing data available
        summary.Timestamp = earliestStartTime ?? DateTimeOffset.Now;

        return summary;
    }
}

file sealed class Utf8StringWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;
}

internal sealed class TestSummary
{
    public int Total { get; set; }
    public int Failures { get; set; }
    public int Errors { get; set; }
    public int Skipped { get; set; }
    public TimeSpan TotalTime { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
