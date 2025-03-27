"use strict";(self.webpackChunktunit_docs_site=self.webpackChunktunit_docs_site||[]).push([[6458],{7599:(t,e,n)=>{n.r(e),n.d(e,{assets:()=>u,contentTitle:()=>o,default:()=>l,frontMatter:()=>r,metadata:()=>s,toc:()=>c});const s=JSON.parse('{"id":"tutorial-basics/data-driven-tests","title":"Data Driven Tests","description":"It\'s common to want to repeat tests but pass in different values on each execution.","source":"@site/docs/tutorial-basics/data-driven-tests.md","sourceDirName":"tutorial-basics","slug":"/tutorial-basics/data-driven-tests","permalink":"/docs/tutorial-basics/data-driven-tests","draft":false,"unlisted":false,"tags":[],"version":"current","sidebarPosition":4,"frontMatter":{"sidebar_position":4},"sidebar":"tutorialSidebar","previous":{"title":"Running your tests","permalink":"/docs/tutorial-basics/running-your-tests"},"next":{"title":"Injectable Class Data Source","permalink":"/docs/tutorial-basics/class-data-source"}}');var a=n(4848),i=n(8453);const r={sidebar_position:4},o="Data Driven Tests",u={},c=[];function d(t){const e={code:"code",h1:"h1",header:"header",p:"p",pre:"pre",...(0,i.R)(),...t.components};return(0,a.jsxs)(a.Fragment,{children:[(0,a.jsx)(e.header,{children:(0,a.jsx)(e.h1,{id:"data-driven-tests",children:"Data Driven Tests"})}),"\n",(0,a.jsx)(e.p,{children:"It's common to want to repeat tests but pass in different values on each execution.\nWe can do that with a data driven test."}),"\n",(0,a.jsxs)(e.p,{children:["Compile-time known data can be injected via ",(0,a.jsx)(e.code,{children:"[Arguments(...)]"})," attributes.\nThis attribute takes an array of arguments. It can take as many as you like, but your test method has to have the same number of parameters and they must be the same type.\nIf you include multiple ",(0,a.jsx)(e.code,{children:"[Arguments]"})," attributes, your test will be repeated that many times, containing the data passed into the attribute."]}),"\n",(0,a.jsx)(e.p,{children:"When your test is executed, TUnit will pass the values provided in the attribute, into the test by the parameters.\nHere's an example:"}),"\n",(0,a.jsx)(e.pre,{children:(0,a.jsx)(e.code,{className:"language-csharp",children:"using TUnit.Assertions;\nusing TUnit.Assertions.Extensions;\nusing TUnit.Core;\n\nnamespace MyTestProject;\n\npublic class MyTestClass\n{\n    [Test]\n    [Arguments(1, 1, 2)]\n    [Arguments(1, 2, 3)]\n    [Arguments(2, 2, 4)]\n    [Arguments(4, 3, 7)]\n    [Arguments(5, 5, 10)]\n    public async Task MyTest(int value1, int value2, int expectedResult)\n    {\n        var result = Add(value1, value2);\n\n        await Assert.That(result).IsEqualTo(expectedResult);\n    }\n\n    private int Add(int x, int y)\n    {\n        return x + y;\n    }\n}\n"})})]})}function l(t={}){const{wrapper:e}={...(0,i.R)(),...t.components};return e?(0,a.jsx)(e,{...t,children:(0,a.jsx)(d,{...t})}):d(t)}},8453:(t,e,n)=>{n.d(e,{R:()=>r,x:()=>o});var s=n(6540);const a={},i=s.createContext(a);function r(t){const e=s.useContext(i);return s.useMemo((function(){return"function"==typeof t?t(e):{...e,...t}}),[e,t])}function o(t){let e;return e=t.disableParentContext?"function"==typeof t.components?t.components(a):t.components||a:r(t.components),s.createElement(i.Provider,{value:e},t.children)}}}]);