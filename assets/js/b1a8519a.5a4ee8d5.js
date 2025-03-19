"use strict";(self.webpackChunktunit_docs_site=self.webpackChunktunit_docs_site||[]).push([[1647],{3177:(t,e,n)=>{n.r(e),n.d(e,{assets:()=>l,contentTitle:()=>o,default:()=>d,frontMatter:()=>a,metadata:()=>i,toc:()=>c});const i=JSON.parse('{"id":"tutorial-extras/explicit","title":"Explicit","description":"If you want a test to only be run explicitly (and not part of all general tests) then you can add the [ExplicitAttribute].","source":"@site/docs/tutorial-extras/explicit.md","sourceDirName":"tutorial-extras","slug":"/tutorial-extras/explicit","permalink":"/TUnit/docs/tutorial-extras/explicit","draft":false,"unlisted":false,"tags":[],"version":"current","sidebarPosition":9,"frontMatter":{"sidebar_position":9},"sidebar":"tutorialSidebar","previous":{"title":"Test Filters","permalink":"/TUnit/docs/tutorial-extras/test-filters"},"next":{"title":"Skipping Tests","permalink":"/TUnit/docs/tutorial-extras/skip"}}');var s=n(4848),r=n(8453);const a={sidebar_position:9},o="Explicit",l={},c=[];function u(t){const e={code:"code",h1:"h1",header:"header",p:"p",pre:"pre",...(0,r.R)(),...t.components};return(0,s.jsxs)(s.Fragment,{children:[(0,s.jsx)(e.header,{children:(0,s.jsx)(e.h1,{id:"explicit",children:"Explicit"})}),"\n",(0,s.jsxs)(e.p,{children:["If you want a test to only be run explicitly (and not part of all general tests) then you can add the ",(0,s.jsx)(e.code,{children:"[ExplicitAttribute]"}),"."]}),"\n",(0,s.jsx)(e.p,{children:"This can be added to a test method or a test class."}),"\n",(0,s.jsx)(e.p,{children:"A test is considered 'explicitly' run when all filtered tests have an explicit attribute on them."}),"\n",(0,s.jsxs)(e.p,{children:["That means that you could run all tests in a class with an ",(0,s.jsx)(e.code,{children:"[Explicit]"})," attribute. Or you could run a single method with an ",(0,s.jsx)(e.code,{children:"[Explicit]"})," attribute. But if you try to run a mix of explicit and non-explicit tests, then the ones with an ",(0,s.jsx)(e.code,{children:"[Explicit]"})," attribute will be excluded from the run."]}),"\n",(0,s.jsx)(e.p,{children:"This can be useful for 'Tests' that make sense in a local environment, and maybe not part of your CI builds. Or they could be helpers that ping things to warm them up, and by making them explicit tests, they are easily runnable, but don't affect your overall test suite."}),"\n",(0,s.jsx)(e.pre,{children:(0,s.jsx)(e.code,{className:"language-csharp",children:"using TUnit.Core;\n\nnamespace MyTestProject;\n\npublic class MyTestClass\n{\n    [Test]\n    [Explicit]\n    public async Task MyTest()\n    {\n        \n    }\n}\n"})})]})}function d(t={}){const{wrapper:e}={...(0,r.R)(),...t.components};return e?(0,s.jsx)(e,{...t,children:(0,s.jsx)(u,{...t})}):u(t)}},8453:(t,e,n)=>{n.d(e,{R:()=>a,x:()=>o});var i=n(6540);const s={},r=i.createContext(s);function a(t){const e=i.useContext(r);return i.useMemo((function(){return"function"==typeof t?t(e):{...e,...t}}),[e,t])}function o(t){let e;return e=t.disableParentContext?"function"==typeof t.components?t.components(s):t.components||s:a(t.components),i.createElement(r.Provider,{value:e},t.children)}}}]);