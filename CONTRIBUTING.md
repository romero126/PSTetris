# Contributing to PSTetris

Thanks for your interest in contributing to PSTetris!

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (supports .NET Standard 2.0)
- PowerShell 5.1 or PowerShell 7+

## Building from Source

```powershell
git clone https://github.com/juanromer/PSTetris.git
cd PSTetris
.\Build-Module.ps1
```

This compiles the C# project in Release mode and stages the module into `output\PSTetris\`.

To build in Debug mode:

```powershell
.\Build-Module.ps1 -Configuration Debug
```

## Loading the Module Locally

After building, import the module directly:

```powershell
Import-Module .\output\PSTetris\PSTetris.psd1
Start-Tetris
```

## Project Structure

See [docs/architecture.md](docs/architecture.md) for a full breakdown of the codebase.

## Submitting Changes

1. Fork the repository and create a branch from `master`.
2. Make your changes.
3. Test locally by building and running the module.
4. Open a pull request with a clear description of what changed and why.

## Reporting Issues

Open an issue on [GitHub](https://github.com/juanromer/PSTetris/issues) with steps to reproduce and your terminal/OS details.
