﻿using System.Diagnostics;

namespace TUnit.TestProject;

public class DebugAssertFailureTests
{
    [Test]
    public void Test()
    {
        FailingMethod();
    }
    
    [Test]
    private void FailingMethod()
    {
        var @true = "true";
        Debug.Assert(@true is "false", "Some message");
    }
}