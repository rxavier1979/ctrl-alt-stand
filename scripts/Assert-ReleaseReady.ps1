# Gate that runs before a release is published: fails loudly if the repository is not in a
# releasable state for -Version. Keeps a half-finished bump from becoming a published download.
#
# Checks:
#   1. The version is a plain major.minor.patch.
#   2. wpf\AssemblyInfo.cs agrees with -Version (both version attributes).
#   3. CHANGELOG.md has a dated, non-empty section for the version — unless a hand-written
#      docs\release-notes\v<version>.md supplies the notes instead.
#   4. CHANGELOG.md still has an "## [Unreleased]" heading, so the next change has somewhere to go.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Version
)

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$problems = New-Object System.Collections.Generic.List[string]

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version must look like 1.2.3, got '$Version'."
}

$declared = & (Join-Path $repo "scripts\Get-AppVersion.ps1")
if ($declared -ne $Version) {
    $problems.Add("wpf\AssemblyInfo.cs declares $declared but the release is for $Version.")
}

$changelogPath = Join-Path $repo "CHANGELOG.md"
$changelog = Get-Content -Raw -LiteralPath $changelogPath

$override = Join-Path $repo ("docs\release-notes\v{0}.md" -f $Version)
$hasOverride = Test-Path -LiteralPath $override

$sectionPattern = '(?ms)^##\s*\[' + [regex]::Escape($Version) + '\]\s*-\s*\d{4}-\d{2}-\d{2}[^\r\n]*\r?\n(?<body>.*?)(?=^##\s|\z)'
$section = [regex]::Match($changelog, $sectionPattern)

if (-not $section.Success) {
    if ($hasOverride) {
        Write-Host "note: CHANGELOG.md has no dated section for $Version; using docs\release-notes\v$Version.md." -ForegroundColor Yellow
    }
    else {
        $problems.Add("CHANGELOG.md has no dated '## [$Version] - YYYY-MM-DD' section.")
    }
}
elseif ($section.Groups['body'].Value.Trim() -eq "") {
    $problems.Add("The CHANGELOG.md section for $Version is empty.")
}

if ($changelog -notmatch '(?m)^##\s*\[Unreleased\]') {
    $problems.Add("CHANGELOG.md is missing its '## [Unreleased]' heading.")
}

if ($problems.Count -gt 0) {
    Write-Host "Not ready to release $Version :" -ForegroundColor Red
    foreach ($p in $problems) { Write-Host "  - $p" -ForegroundColor Red }
    exit 1
}

Write-Host "Ready to release $Version." -ForegroundColor Green
exit 0   # explicit, so callers can rely on $LASTEXITCODE
