using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.TestAdapter;

public record TestWithTestCase(TestDetails Details, TestCase TestCase);