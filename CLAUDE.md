# Working in this repository

Read [`CONTRIBUTING.md`](CONTRIBUTING.md) for the full process. This file is the short list of things
that are easy to get wrong here.

## What ships

- `wpf/` is the shipping app and the download. `src/` is the legacy WinForms version, **frozen** — do
  not port fixes into it unless asked, and do not bump its version.
- Build with `.\wpf\build.ps1` (in-box .NET Framework MSBuild). Checks:
  `.\wpf\dist\CtrlAltStand.exe --self-test` and `--smoke-test`, both must exit 0.
- The C# is written to the in-box compiler's level (**C# 5** — no expression-bodied members, no
  string interpolation, no `?.`). Match it.
- Repository PowerShell must run on **Windows PowerShell 5.1** — no ternary or null-coalescing
  operators.

## Version and release

- `wpf/AssemblyInfo.cs` owns the version. Bump it **only** via `.\scripts\New-Version.ps1 -Version x.y.z`.
- A version bump landing on `main` publishes a GitHub release automatically. Treat that as a
  publishing action: get explicit confirmation before merging a bump.
- Every code change under `wpf/` or `src/` needs a `CHANGELOG.md` entry under `## [Unreleased]`,
  written in user-facing language. CI enforces it.

## Change hygiene

- Update the docs that describe the behavior in the same change — `README.md` for user-facing
  behavior, `wpf/README.md` for implementation and gotchas.
- When a bug's cause is non-obvious, document the trap where the code lives, not just in the commit
  message. Example: `wpf/README.md` → *Taskbar flash lifetime*.
- Conventional commits (`fix(wpf):`, `docs:`, `ci:`, `chore(release):`), body explains *why*, and
  agent commits end with the `Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>` trailer.
- Branch off `main`, PR with `Fixes #N`, rebase-merge to keep history linear.

## Testing and reporting

- Distinguish "builds and self-tests pass" from "the behavior was observed." Say which one you have.
  Cue/tray/taskbar behavior is **not** covered by `--self-test`; see the manual procedure in
  `CONTRIBUTING.md`.
- Before trusting a manual test result, confirm which binary is running — an installed copy elsewhere
  on the machine is not updated by building here. The version in the title bar and tray tooltip
  identifies the build; `Get-Process CtrlAltStand | Select-Object Id, Path` confirms the file.
- Give **absolute paths** in commands meant to be run, not repo-relative ones.
