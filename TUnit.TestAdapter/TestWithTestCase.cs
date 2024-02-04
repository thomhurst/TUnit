using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TUnit.Core;

namespace TUnit.TestAdapter;

internal record TestWithTestCase(TestDetails Details, TestCase TestCase);