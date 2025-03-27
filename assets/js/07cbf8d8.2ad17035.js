"use strict";(self.webpackChunktunit_docs_site=self.webpackChunktunit_docs_site||[]).push([[1377],{2239:(e,n,t)=>{t.r(n),t.d(n,{assets:()=>s,contentTitle:()=>i,default:()=>l,frontMatter:()=>a,metadata:()=>r,toc:()=>d});const r=JSON.parse('{"id":"tutorial-advanced/dependency-injection","title":"Dependency Injection","description":"Dependency Injection can be set up by leveraging the power of the Data Source Generators.","source":"@site/docs/tutorial-advanced/dependency-injection.md","sourceDirName":"tutorial-advanced","slug":"/tutorial-advanced/dependency-injection","permalink":"/docs/tutorial-advanced/dependency-injection","draft":false,"unlisted":false,"tags":[],"version":"current","sidebarPosition":13,"frontMatter":{"sidebar_position":13},"sidebar":"tutorialSidebar","previous":{"title":"Data Source Generators","permalink":"/docs/tutorial-advanced/data-source-generators"},"next":{"title":"Property Injection","permalink":"/docs/tutorial-advanced/property-injection"}}');var o=t(4848),c=t(8453);const a={sidebar_position:13},i="Dependency Injection",s={},d=[];function p(e){const n={code:"code",h1:"h1",header:"header",p:"p",pre:"pre",...(0,c.R)(),...e.components};return(0,o.jsxs)(o.Fragment,{children:[(0,o.jsx)(n.header,{children:(0,o.jsx)(n.h1,{id:"dependency-injection",children:"Dependency Injection"})}),"\n",(0,o.jsx)(n.p,{children:"Dependency Injection can be set up by leveraging the power of the Data Source Generators."}),"\n",(0,o.jsx)(n.p,{children:"TUnit provides you an abstract class to handle most of the logic for you, you need to simply provide the implementation on how to create a DI Scope, and then how to get or create an object when given its type."}),"\n",(0,o.jsxs)(n.p,{children:["So create a new class that inherits from ",(0,o.jsx)(n.code,{children:"DependencyInjectionDataSourceAttribute<TScope>"})," and pass through the Scope type as the generic argument."]}),"\n",(0,o.jsx)(n.p,{children:"Here's an example of that using the Microsoft.Extensions.DependencyInjection library:"}),"\n",(0,o.jsx)(n.pre,{children:(0,o.jsx)(n.code,{className:"language-csharp",children:"using TUnit.Core;\n\nnamespace MyTestProject;\n\npublic class MicrosoftDependencyInjectionDataSourceAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>\n{\n    private static readonly IServiceProvider ServiceProvider = CreateSharedServiceProvider();\n\n    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)\n    {\n        return ServiceProvider.CreateAsyncScope();\n    }\n\n    public override object? Create(IServiceScope scope, Type type)\n    {\n        return scope.ServiceProvider.GetService(type);\n    }\n    \n    private static IServiceProvider CreateSharedServiceProvider()\n    {\n        return new ServiceCollection()\n            .AddSingleton<SomeClass1>()\n            .AddSingleton<SomeClass2>()\n            .AddTransient<SomeClass3>()\n            .BuildServiceProvider();\n    }\n}\n\n[MicrosoftDependencyInjectionDataSource]\npublic class MyTestClass(SomeClass1 someClass1, SomeClass2 someClass2, SomeClass3 someClass3)\n{\n    [Test]\n    public async Task Test()\n    {\n        // ...\n    }\n}\n"})})]})}function l(e={}){const{wrapper:n}={...(0,c.R)(),...e.components};return n?(0,o.jsx)(n,{...e,children:(0,o.jsx)(p,{...e})}):p(e)}},8453:(e,n,t)=>{t.d(n,{R:()=>a,x:()=>i});var r=t(6540);const o={},c=r.createContext(o);function a(e){const n=r.useContext(c);return r.useMemo((function(){return"function"==typeof e?e(n):{...n,...e}}),[n,e])}function i(e){let n;return n=e.disableParentContext?"function"==typeof e.components?e.components(o):e.components||o:a(e.components),r.createElement(c.Provider,{value:n},e.children)}}}]);