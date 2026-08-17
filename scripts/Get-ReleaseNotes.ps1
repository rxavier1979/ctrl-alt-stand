# Builds the GitHub release body for a version.
#
# Source of the "What's new" section, in order of preference:
#   1. docs\release-notes\v<version>.md  -- hand-written prose, used verbatim when you want the
#      friendlier phrasing used for v0.1.0-v0.3.0 rather than raw changelog wording.
#   2. The matching "## [<version>] - <date>" section of CHANGELOG.md.
#
# The Install section (and the SHA-256 of the shipped .exe, when -ExePath is given) is always
# appended, so every release carries the same install instructions and a verifiable hash.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Version,
    [string] $ExePath
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 1.2.3, got '$Version'." }

$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$override = Join-Path $repo ("docs\release-notes\v{0}.md" -f $Version)
if (Test-Path -LiteralPath $override) {
    $whatsNew = (Get-Content -Raw -LiteralPath $override).TrimEnd()
}
else {
    $changelogPath = Join-Path $repo "CHANGELOG.md"
    if (-not (Test-Path -LiteralPath $changelogPath)) { throw "Not found: $changelogPath" }
    $changelog = Get-Content -Raw -LiteralPath $changelogPath

    # Capture everything between this version's heading and the next "## " heading.
    $pattern = '(?ms)^##\s*\[' + [regex]::Escape($Version) + '\][^\r\n]*\r?\n(?<body>.*?)(?=^##\s|\z)'
    $m = [regex]::Match($changelog, $pattern)
    if (-not $m.Success) {
        throw ("CHANGELOG.md has no section for {0}. Add '## [{0}] - <date>' before releasing, " -f $Version) +
              "or provide docs\release-notes\v$Version.md."
    }

    $body = $m.Groups['body'].Value.Trim()
    if ($body -eq "") { throw "The CHANGELOG.md section for $Version is empty." }
    $whatsNew = "## What's new`r`n`r`n" + $body
}

$install = @"
## Install

Download ``CtrlAltStand.exe`` below and run it. No installer or additional runtime download is required on Windows 11.
"@

if ($ExePath) {
    if (-not (Test-Path -LiteralPath $ExePath)) { throw "Not found: $ExePath" }
    $sha = (Get-FileHash -LiteralPath $ExePath -Algorithm SHA256).Hash.ToUpperInvariant()
    $install = $install + "`r`n`r`n**SHA-256:** ``$sha``"
}

$whatsNew + "`r`n`r`n" + $install
