"use strict";(self.webpackChunktunit_docs_site=self.webpackChunktunit_docs_site||[]).push([[7757],{3086:(e,t,n)=>{n.r(t),n.d(t,{assets:()=>c,contentTitle:()=>i,default:()=>u,frontMatter:()=>r,metadata:()=>a,toc:()=>d});const a=JSON.parse('{"id":"tutorial-advanced/repeating","title":"Repeating","description":"If you want to repeat a test, add a [RepeatAttribute] onto your test method or class. This takes an int of how many times you\'d like to repeat. Each repeat will show in the test explorer as a new test.","source":"@site/docs/tutorial-advanced/repeating.md","sourceDirName":"tutorial-advanced","slug":"/tutorial-advanced/repeating","permalink":"/docs/tutorial-advanced/repeating","draft":false,"unlisted":false,"tags":[],"version":"current","sidebarPosition":4,"frontMatter":{"sidebar_position":4},"sidebar":"tutorialSidebar","previous":{"title":"Retrying","permalink":"/docs/tutorial-advanced/retrying"},"next":{"title":"Timeouts","permalink":"/docs/tutorial-advanced/timeouts"}}');var s=n(4848),o=n(8453);const r={sidebar_position:4},i="Repeating",c={},d=[];function l(e){const t={code:"code",h1:"h1",header:"header",p:"p",pre:"pre",...(0,o.R)(),...e.components};return(0,s.jsxs)(s.Fragment,{children:[(0,s.jsx)(t.header,{children:(0,s.jsx)(t.h1,{id:"repeating",children:"Repeating"})}),"\n",(0,s.jsxs)(t.p,{children:["If you want to repeat a test, add a ",(0,s.jsx)(t.code,{children:"[RepeatAttribute]"})," onto your test method or class. This takes an ",(0,s.jsx)(t.code,{children:"int"})," of how many times you'd like to repeat. Each repeat will show in the test explorer as a new test."]}),"\n",(0,s.jsx)(t.p,{children:"This can be used on base classes and inherited to affect all tests in sub-classes."}),"\n",(0,s.jsx)(t.pre,{children:(0,s.jsx)(t.code,{className:"language-csharp",children:"using TUnit.Core;\n\nnamespace MyTestProject;\n\npublic class MyTestClass\n{\n    [Test]\n    [Repeat(3)]\n    public async Task MyTest()\n    {\n        \n    }\n}\n"})})]})}function u(e={}){const{wrapper:t}={...(0,o.R)(),...e.components};return t?(0,s.jsx)(t,{...e,children:(0,s.jsx)(l,{...e})}):l(e)}},8453:(e,t,n)=>{n.d(t,{R:()=>r,x:()=>i});var a=n(6540);const s={},o=a.createContext(s);function r(e){const t=a.useContext(o);return a.useMemo((function(){return"function"==typeof e?e(t):{...t,...e}}),[t,e])}function i(e){let t;return t=e.disableParentContext?"function"==typeof e.components?e.components(s):e.components||s:r(e.components),a.createElement(o.Provider,{value:t},e.children)}}}]);