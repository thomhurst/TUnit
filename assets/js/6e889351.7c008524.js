"use strict";(self.webpackChunktunit_docs_site=self.webpackChunktunit_docs_site||[]).push([[8090],{3980:(e,t,s)=>{s.r(t),s.d(t,{assets:()=>l,contentTitle:()=>a,default:()=>u,frontMatter:()=>r,metadata:()=>n,toc:()=>c});const n=JSON.parse('{"id":"tutorial-assertions/extensibility/custom-assertions","title":"Custom Assertions","description":"The TUnit Assertions can be easily extended so that you can create your own assertions.","source":"@site/docs/tutorial-assertions/extensibility/custom-assertions.md","sourceDirName":"tutorial-assertions/extensibility","slug":"/tutorial-assertions/extensibility/custom-assertions","permalink":"/TUnit/docs/tutorial-assertions/extensibility/custom-assertions","draft":false,"unlisted":false,"tags":[],"version":"current","sidebarPosition":1,"frontMatter":{"sidebar_position":1},"sidebar":"tutorialSidebar","previous":{"title":"Extensibility","permalink":"/TUnit/docs/category/extensibility"},"next":{"title":"Chaining and Converting","permalink":"/TUnit/docs/tutorial-assertions/extensibility/chaining-and-converting"}}');var i=s(4848),o=s(8453);const r={sidebar_position:1},a="Custom Assertions",l={},c=[];function d(e){const t={code:"code",em:"em",h1:"h1",header:"header",li:"li",ol:"ol",p:"p",pre:"pre",ul:"ul",...(0,o.R)(),...e.components};return(0,i.jsxs)(i.Fragment,{children:[(0,i.jsx)(t.header,{children:(0,i.jsx)(t.h1,{id:"custom-assertions",children:"Custom Assertions"})}),"\n",(0,i.jsx)(t.p,{children:"The TUnit Assertions can be easily extended so that you can create your own assertions."}),"\n",(0,i.jsx)(t.p,{children:"In TUnit, there are two types of things we can assert on:"}),"\n",(0,i.jsxs)(t.ul,{children:["\n",(0,i.jsx)(t.li,{children:"Values"}),"\n",(0,i.jsx)(t.li,{children:"Delegates"}),"\n"]}),"\n",(0,i.jsxs)(t.p,{children:["Values is what you'd guess, some return value, such as a ",(0,i.jsx)(t.code,{children:"string"})," or ",(0,i.jsx)(t.code,{children:"int"})," or even a complex class."]}),"\n",(0,i.jsxs)(t.p,{children:["Delegates are bits of code that haven't executed yet - Instead they are passed into the assertion builder, and the TUnit assertion library will execute it. If it throws, then there will be an ",(0,i.jsx)(t.code,{children:"Exception"})," object we can check in our assertion."]}),"\n",(0,i.jsx)(t.p,{children:"So to create a custom assertion:"}),"\n",(0,i.jsxs)(t.ol,{children:["\n",(0,i.jsxs)(t.li,{children:["\n",(0,i.jsx)(t.p,{children:"There are multiple classes you can inherit from to simplify your needs:"}),"\n",(0,i.jsxs)(t.ol,{children:["\n",(0,i.jsxs)(t.li,{children:["If you want to assert a value has some expected data, then inherit from the ",(0,i.jsx)(t.code,{children:"ExpectedValueAssertCondition<TActual, TExpected>"})]}),"\n",(0,i.jsxs)(t.li,{children:["If you want to assert a value meets some criteria (e.g. IsNull) then inherit from ",(0,i.jsx)(t.code,{children:"ValueAssertCondition<TActual>"})]}),"\n",(0,i.jsxs)(t.li,{children:["If you want to assert a delegate threw or didn't throw an exception, inherit from ",(0,i.jsx)(t.code,{children:"DelegateAssertCondition"})," or ",(0,i.jsx)(t.code,{children:"ExpectedExceptionDelegateAssertCondition<TException>"})]}),"\n",(0,i.jsxs)(t.li,{children:["If those don't fit what you need, the most basic class to inherit from is ",(0,i.jsx)(t.code,{children:"BaseAssertCondition<TActual>"})]}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(t.li,{children:["\n",(0,i.jsxs)(t.p,{children:["For the generic types above, ",(0,i.jsx)(t.code,{children:"TActual"})," will be the type of object that is being asserted. For example if I started with ",(0,i.jsx)(t.code,{children:'Assert.That("Some text")'})," then ",(0,i.jsx)(t.code,{children:"TActual"})," would be a ",(0,i.jsx)(t.code,{children:"string"})," because that's what we're asserting on."]}),"\n",(0,i.jsxs)(t.p,{children:[(0,i.jsx)(t.code,{children:"TExpected"})," will be the data (if any) that you receive from your extension method, so you'll be responsible for passing this in. You must pass it to the base class via the base constructor: ",(0,i.jsx)(t.code,{children:"base(expectedValue)"})]}),"\n"]}),"\n",(0,i.jsxs)(t.li,{children:["\n",(0,i.jsxs)(t.p,{children:["Override the method:\n",(0,i.jsx)(t.code,{children:"protected override Task<AssertionResult> GetResult(...)"})]}),"\n",(0,i.jsxs)(t.p,{children:[(0,i.jsx)(t.code,{children:"AssertionResult"})," has static methods to represent a pass or a fail."]}),"\n",(0,i.jsx)(t.p,{children:"You will be passed relevant objects based on what you're asserting. These may or may not be null, so the logic is up to you."}),"\n",(0,i.jsxs)(t.p,{children:["Any ",(0,i.jsx)(t.code,{children:"Exception"})," object will be populated if your assertion is a Delegate type and the delegate threw."]}),"\n",(0,i.jsxs)(t.p,{children:["Any ",(0,i.jsx)(t.code,{children:"TActual"})," object will be populated if a value was passed into ",(0,i.jsx)(t.code,{children:"Assert.That(...)"}),", or a delegate with a return value was executed successfully."]}),"\n"]}),"\n",(0,i.jsxs)(t.li,{children:["\n",(0,i.jsxs)(t.p,{children:["Override the ",(0,i.jsx)(t.code,{children:"GetExpectation"}),' method to return a message representing what would have been a success, in the format of "to [Your Expectation]".\ne.g. Expected [Actual Value] ',(0,i.jsx)(t.em,{children:"to be equal to [Expected Value]"})]}),"\n"]}),"\n"]}),"\n",(0,i.jsxs)(t.p,{children:["When you return an ",(0,i.jsx)(t.code,{children:"AssertionResult.Fail"})," result, you supply a message. This is appended after the above statement with a ",(0,i.jsx)(t.code,{children:"but {Your Message}"}),"\ne.g. Expected [Actual Value] to be equal to [Expected Value] ",(0,i.jsx)(t.em,{children:"but it was null"})]}),"\n",(0,i.jsx)(t.p,{children:"In your assertion class, that'd be set up like:"}),"\n",(0,i.jsx)(t.pre,{children:(0,i.jsx)(t.code,{className:"language-csharp",children:'    protected override string GetExpectation()\n        => $"to be equal to {Format(expected).TruncateWithEllipsis(100)}";\n\n   protected override Task<AssertionResult> GetResult(string? actualValue, string? expectedValue)\n    {\n        if (actualValue is null)\n        {\n            return AssertionResult\n                .FailIf(\n                    () => expectedValue is not null,\n                    "it was null");\n        }\n\n        ...\n    }\n'})}),"\n",(0,i.jsxs)(t.ol,{children:["\n",(0,i.jsxs)(t.li,{children:["\n",(0,i.jsx)(t.p,{children:"Create the extension method!"}),"\n",(0,i.jsxs)(t.p,{children:["You need to create an extension off of either ",(0,i.jsx)(t.code,{children:"IValueSource<TActual>"})," or ",(0,i.jsx)(t.code,{children:"IDelegateSource<TActual>"})," - Depending on what you're planning to write an assertion for. By extending off of the relevant interface we make sure that it won't be shown where it doesn't make sense thanks to the C# typing system."]}),"\n",(0,i.jsxs)(t.p,{children:["Your return type for the extension method should be ",(0,i.jsx)(t.code,{children:"InvokableValueAssertionBuilder<TActual>"})," or ",(0,i.jsx)(t.code,{children:"InvokableDelegateAssertionBuilder<TActual>"})," depending on what type your assertion is."]}),"\n",(0,i.jsxs)(t.p,{children:["And then finally, you call ",(0,i.jsx)(t.code,{children:"source.RegisterAssertion(assertCondition, [...callerExpressions])"})," - passing in your newed up your custom assert condition class.\nThe argument expression array allows you to pass in ",(0,i.jsx)(t.code,{children:"[CallerArgumentExpression]"})," values so that your assertion errors show you the code executed to give clear exception messages."]}),"\n"]}),"\n"]}),"\n",(0,i.jsx)(t.p,{children:"Here's a fully fledged assertion in action:"}),"\n",(0,i.jsx)(t.pre,{children:(0,i.jsx)(t.code,{className:"language-csharp",children:'public static InvokableValueAssertionBuilder<string> Contains(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "", [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = "")\n    {\n        return valueSource.RegisterAssertion(\n            assertCondition: new StringEqualsAssertCondition(expected, stringComparison),\n            argumentExpressions: [doNotPopulateThisValue1, doNotPopulateThisValue2]\n            );\n    }\n'})}),"\n",(0,i.jsx)(t.pre,{children:(0,i.jsx)(t.code,{className:"language-csharp",children:'public class StringEqualsExpectedValueAssertCondition(string expected, StringComparison stringComparison)\n    : ExpectedValueAssertCondition<string, string>(expected)\n{\n    protected override string GetExpectation()\n        => $"to be equal to \\"{expected}\\"";\n\n    protected override async ValueTask<AssertionResult> GetResult(string? actualValue, string? expectedValue)\n    {\n        if (actualValue is null)\n        {\n            return AssertionResult\n                .FailIf(\n                    expectedValue is not null,\n                    "it was null");\n        }\n\n        return AssertionResult\n            .FailIf(\n                !string.Equals(actualValue, expectedValue, stringComparison),\n                $"found \\"{actualValue}\\"");\n    }\n}\n'})})]})}function u(e={}){const{wrapper:t}={...(0,o.R)(),...e.components};return t?(0,i.jsx)(t,{...e,children:(0,i.jsx)(d,{...e})}):d(e)}},8453:(e,t,s)=>{s.d(t,{R:()=>r,x:()=>a});var n=s(6540);const i={},o=n.createContext(i);function r(e){const t=n.useContext(o);return n.useMemo((function(){return"function"==typeof e?e(t):{...t,...e}}),[t,e])}function a(e){let t;return t=e.disableParentContext?"function"==typeof e.components?e.components(i):e.components||i:r(e.components),n.createElement(o.Provider,{value:t},e.children)}}}]);