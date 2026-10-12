# Ctrl+Alt+Stand — Codex entry point (AGENTS.md)

**POINTER ONLY.** The rules for this repository are in the sibling [`CLAUDE.md`](CLAUDE.md) — read it
first, in full. [`CONTRIBUTING.md`](CONTRIBUTING.md) carries the full process behind it.

No rules are duplicated here. This file previously held a complete copy of `CLAUDE.md`, and the copy
had already drifted: the two disagreed about the commit trailer, and a drifted copy still reads as
authoritative. If this file and `CLAUDE.md` ever disagree, `CLAUDE.md` wins.

## The one difference that applies to Codex

`CLAUDE.md` says agent commits end with a `Co-Authored-By` trailer naming the model that actually
wrote the change. For commits authored by Codex, name the Codex model that did the work, with no
Anthropic address, since Codex models are not Anthropic's. For example, on GPT-6 Astra:

    Co-Authored-By: Codex (GPT-6 Astra) <noreply@openai.com>

The model name is the part that matters and it changes with the model you ran; the address is a
non-delivering placeholder for the model's vendor.

Everything else in `CLAUDE.md` — what ships, the C# 5 and PowerShell 5.1 limits, the ASCII-only rule
for `.ps1` files and workflow `run:` blocks, versioning and release, change hygiene, and the testing
and reporting rules — applies unchanged.
