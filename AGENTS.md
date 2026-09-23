# Ctrl+Alt+Stand — Codex entry point (AGENTS.md)

**POINTER ONLY.** The rules for this repository are in the sibling [`CLAUDE.md`](CLAUDE.md) — read it
first, in full. [`CONTRIBUTING.md`](CONTRIBUTING.md) carries the full process behind it.

No rules are duplicated here. This file previously held a complete copy of `CLAUDE.md`, and the copy
had already drifted: the two disagreed about the commit trailer, and a drifted copy still reads as
authoritative. If this file and `CLAUDE.md` ever disagree, `CLAUDE.md` wins.

## The one difference that applies to Codex

`CLAUDE.md` says agent commits end with a `Co-Authored-By` trailer. For commits authored by Codex,
that trailer is:

    Co-Authored-By: Codex Opus 5 <noreply@anthropic.com>

Everything else in `CLAUDE.md` — what ships, the C# 5 and PowerShell 5.1 limits, the ASCII-only rule
for `.ps1` files and workflow `run:` blocks, versioning and release, change hygiene, and the testing
and reporting rules — applies unchanged.
