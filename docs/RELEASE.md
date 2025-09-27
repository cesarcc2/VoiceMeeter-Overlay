# Release Process

This document describes how to version, package, and publish VMHud.

## Versioning

- Use SemVer: MAJOR.MINOR.PATCH
- Bump MINOR for new features; PATCH for bug fixes

## Changelog

- Maintain `CHANGELOG.md` at the repo root (or release notes in GitHub Releases)

## Build Artifacts

1) Clean and publish Release build (single file):
   ```powershell
   dotnet clean
   dotnet publish src/VMHud.App -c Release -r win-x64 \
     -p:PublishSingleFile=true \
     -p:IncludeNativeLibrariesForSelfExtract=true \
     -p:PublishTrimmed=true
   ```
2) Verify the produced EXE runs on a clean Windows VM with Voicemeeter installed

## Code Signing (optional)

- Sign the EXE with your code signing certificate using `signtool`

## Packaging

- Zip the publish folder contents into `VMHud-vX.Y.Z-win-x64.zip`
- Do not include Voicemeeter binaries; instruct users to install Voicemeeter

## Release

- Create a Git tag `vX.Y.Z`
- Draft a GitHub Release with:
  - Summary of changes
  - Known issues and upgrade notes
  - Assets: zip artifact

### Current

- Version: `v1.0.0`
- Notes: see `docs/VERSION-1.0.md` for highlights and next steps

## Post-Release

- Triage user reports
- Plan fixes and next milestones in `docs/ROADMAP.md`
