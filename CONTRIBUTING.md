# Contributing to Ctrl+Alt+Stand

How work gets done in this repository. The short version: one issue per problem, one branch per
issue, docs and changelog land with the code, and **every version bump publishes a release
automatically**.

## The loop

1. **Log an issue** describing the symptom, the reproduction, and — once known — the root cause with
   `file:line` references. Issues are worth writing even for small things you intend to fix later.
2. **Branch** off `main`: `fix/short-slug`, `feat/short-slug`, `docs/short-slug`, `ci/short-slug`.
3. **Change the code, and the docs that describe it in the same commit.** A behavior change that
   leaves a stale sentence in a README is not finished.
4. **Add a `CHANGELOG.md` entry** under `## [Unreleased]`, in the user's language — what changed for
   the person using the app, not what changed in the source. CI enforces this for changes under
   `wpf/` or `src/`; tooling, CI, and docs-only changes are exempt.
5. **Open a PR** using the template. Link the issue with `Fixes #N` so merging closes it.
6. **Let CI pass**, then **rebase-merge** to keep history linear, and delete the branch.

## Commits

Conventional commits, lowercase, scoped where it helps: `fix(wpf):`, `docs:`, `ci:`, `chore(release):`.

The subject says what changed; the body says *why*, and names the mechanism if the bug was
non-obvious. Explain the trap, not just the patch — the next reader needs to avoid re-introducing it.

Commits made by an agent end with:

```
Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
```

## Versions and releases

`wpf/AssemblyInfo.cs` is the single source of truth for the version. The legacy WinForms source in
`src/` is frozen and carries its own unrelated version.

**Never hand-edit the version.** Run:

```powershell
.\scripts\New-Version.ps1 -Version 0.3.2
```

That bumps both version attributes, promotes the `[Unreleased]` changelog entries into a dated
section, and re-checks release readiness. It never pushes. Review the diff, commit
(`chore(release): v0.3.2`), and open a PR as usual.

**Publishing is automatic.** When a version bump lands on `main`, `.github/workflows/release.yml`
verifies readiness, builds, confirms the built `.exe` actually carries that version, runs both
checks, then creates the `v<version>` tag and publishes the release with `CtrlAltStand.exe` and its
SHA-256 attached. There is no separate tagging step to forget — that is precisely the step that got
forgotten before this existed.

Consequences worth knowing:

- A push that does not change the version finds the tag already present and exits quietly, so
  changelog and release-note edits are safe to land at any time.
- If a bump lands while its changelog entry is missing, the run **fails**. Adding the entry
  re-triggers it and the release goes out then.
- Release notes come from the `CHANGELOG.md` section for that version. To ship hand-written prose
  instead, add `docs/release-notes/v<version>.md` and it is used verbatim; the install instructions
  and SHA-256 are always appended.

### Scripts

| Script | Purpose |
|---|---|
| `scripts/Get-AppVersion.ps1` | Prints the version, failing if the two version attributes disagree |
| `scripts/Assert-ReleaseReady.ps1` | Gate: version matches, changelog section exists and is dated, `[Unreleased]` still present |
| `scripts/Get-ReleaseNotes.ps1` | Composes the release body from the changelog (or an override) plus install text and SHA-256 |
| `scripts/New-Version.ps1` | The only supported way to bump a version |

They are plain PowerShell so they can be run and debugged locally rather than only inside CI. Keep
them Windows PowerShell 5.1-compatible — that is what the workflows use.

### Keep PowerShell and workflows ASCII-only

**No em dashes, curly quotes, or other non-ASCII characters in `.ps1` files or workflow `run:`
blocks.** CI enforces this.

Windows PowerShell 5.1 reads a UTF-8 file with no BOM as Windows-1252. An em dash (`E2 80 94` in
UTF-8) decodes with its `0x94` byte as a right double quotation mark, which *terminates a PowerShell
string literal*. Depending on how the stray quotes balance out you get either a parse error or —
much worse — a script that parses into something other than what you wrote, runs partway, and exits
0. That is not theoretical: it made the first live run of `release.yml` skip publishing entirely
while reporting success.

Two things make this hard to catch locally:

- PowerShell 7 (`pwsh`) reads UTF-8 without a BOM correctly, so a script that works when you test it
  in `pwsh` can still be broken under the `powershell` shell CI uses. **Test release tooling with
  `powershell.exe -NoProfile`, not just `pwsh`.**
- A step that produces no output looks identical to a step that legitimately decided to do nothing.
  `release.yml` now has an explicit guard that fails when the tag check yields no decision, rather
  than letting silence skip the publish.

## Building and testing

```powershell
.\wpf\build.ps1
.\wpf\dist\CtrlAltStand.exe --self-test
.\wpf\dist\CtrlAltStand.exe --smoke-test
```

The build uses the in-box .NET Framework MSBuild; see [`wpf/README.md`](wpf/README.md) for the
toolchain details and the C# language level to write to.

### Testing a local build against an installed copy

If you also run an installed copy of the app, the two are separate files and building does not
update the installed one. The **version in the title bar and the tray tooltip** tells you which
build is in front of you. When a test result contradicts a change you just made, confirm the running
build before concluding anything:

```powershell
Get-Process CtrlAltStand | Select-Object Id, Path
```

### Manual checks

Some behavior cannot be covered by `--self-test` and must be exercised by hand. Say so in the PR
rather than implying automated coverage.

**Background transition cue** (fastest reproduction, no waiting for a phase to expire):

1. Start the app and minimize it — the taskbar flash is a no-op while the window is in the foreground.
2. Right-click the tray icon → **Skip phase**. This runs the same `AnnouncePhase()` path a real
   expiry runs, and clicking a tray menu item does not activate the main window.
3. The cue appears top-right and the taskbar button flashes.
4. Click the cue. It closes *and* the taskbar button settles, without touching the window.

Skipping a phase writes nothing to `settings.ini`, so saved schedules and memories are unaffected.

## Reporting results

State what was verified and what was not. "Builds clean and both checks pass; the visible behavior
is unverified" is a useful sentence. An unqualified "fixed" that rests on a build succeeding is not.
