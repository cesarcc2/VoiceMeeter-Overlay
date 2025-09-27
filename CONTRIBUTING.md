# Contributing to VMHud

Thanks for your interest in contributing! This guide helps you set up your environment and send effective pull requests.

## Development Setup

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or VS Code with C# Dev Kit
- Voicemeeter Potato installed for runtime validation

## Repo Structure

- `src/VMHud.App` — WPF UI application
- `src/VMHud.Core` — models, viewmodels, contracts, configuration
- `src/VMHud.Backend` — Voicemeeter interop and polling (stubs initially)
- `src/VMHud.Tests` — placeholder for tests
- `docs/` — architecture, spec, build, release, roadmap

## Build and Run

```powershell
dotnet restore
dotnet build -c Debug
dotnet run -c Debug --project src/VMHud.App
```

## Coding Guidelines

- C# 12, `nullable` and `implicit usings` enabled
- Prefer clear, small types with explicit names
- Keep UI logic in ViewModels (MVVM) and keep Views minimal
- Avoid premature abstractions; prefer simple, testable code

## Branching and PRs

- Branch from `main` with a descriptive name, e.g. `feature/hotkey-ui`
- Keep PRs focused; include a short summary, motivation, and screenshots if UI changes
- Update docs where relevant (Architecture/Tech-Spec/Build/README)

## Commit Messages

- Use imperative mood: "Add hotkey rebinding UI"
- Reference issues when applicable: `Fixes #123`

## Issue Reporting

- Provide steps to reproduce, expected vs actual behavior, logs if any
- Include OS version, Voicemeeter version, and app version

## Code of Conduct

Be respectful and constructive. We welcome contributors of all backgrounds and experience levels.
