using System.Runtime.InteropServices;
using TUnit.Core;

namespace TUnit.TestProject;

public class CustomSkipAttribute() : SkipAttribute("Some reason");