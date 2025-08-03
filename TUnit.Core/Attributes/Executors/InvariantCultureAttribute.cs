using System.Globalization;

namespace TUnit.Core.Executors;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public class InvariantCultureAttribute() : CultureAttribute(CultureInfo.InvariantCulture);
