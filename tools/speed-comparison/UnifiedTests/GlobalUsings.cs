#if TUNIT
global using TUnit.Core;
global using static TUnit.Core.HookType;
global using TUnit.Assertions;
global using TUnit.Assertions.Extensions;
#elif XUNIT3
global using Xunit;
#elif NUNIT
global using NUnit.Framework;
#elif MSTEST
global using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif