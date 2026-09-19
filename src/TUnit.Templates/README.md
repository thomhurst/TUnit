# TUnit.Templates

Some templates to help you get started with TUnit!

For more information, check out the repository at https://www.github.com/thomhurst/TUnit

## Options

The C#, F# and VB variants of the `TUnit` template support:

- `--enable-dotcover` - Adds the `JetBrains.dotCover.Framework` package for per-test coverage in dotCover, Rider and ReSharper.

The C# `TUnit` template additionally supports:

- `--framework <tfm>` - Target framework. Default `net10.0`; also `net9.0`, `net8.0`, `net481`, `net48`, `net472` and `net462`.

```bash
dotnet new TUnit -n MyTests --enable-dotcover
dotnet new TUnit -n MyTests --language F# --enable-dotcover
```
