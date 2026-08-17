# Prepares a new version: bumps wpf\AssemblyInfo.cs and promotes the CHANGELOG.md "Unreleased"
# entries into a dated section for that version.
#
# This is the only step a human (or agent) performs. Publishing is automatic: once the bumped
# AssemblyInfo.cs lands on main, .github\workflows\release.yml builds, tags v<version>, and
# publishes the release. See CONTRIBUTING.md.
#
#   .\scripts\New-Version.ps1 -Version 0.3.1
#   .\scripts\New-Version.ps1 -Version 0.4.0 -Commit
#
# Never pushes. Review the diff, then commit (or push the branch and open a PR) yourself.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Version,
    [string] $Date,
    [switch] $Commit
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 1.2.3, got '$Version'." }
if (-not $Date) { $Date = (Get-Date).ToString("yyyy-MM-dd") }
if ($Date -notmatch '^\d{4}-\d{2}-\d{2}$') { throw "Date must look like 2026-08-17, got '$Date'." }

$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$current = & (Join-Path $repo "scripts\Get-AppVersion.ps1")
if ([version]$Version -le [version]$current) {
    throw "New version $Version must be greater than the current version $current."
}

# --- CHANGELOG.md: move the Unreleased entries under a dated heading -------------------------
$changelogPath = Join-Path $repo "CHANGELOG.md"
$changelog = Get-Content -Raw -LiteralPath $changelogPath

$unreleased = [regex]::Match($changelog, '(?ms)^(?<head>##\s*\[Unreleased\][^\r\n]*\r?\n)(?<body>.*?)(?=^##\s|\z)')
if (-not $unreleased.Success) { throw "CHANGELOG.md has no '## [Unreleased]' section." }

$entries = $unreleased.Groups['body'].Value.Trim()
if ($entries -eq "") {
    throw "CHANGELOG.md has nothing under [Unreleased] — there is nothing to release."
}

$replacement = "## [Unreleased]`r`n`r`n## [$Version] - $Date`r`n`r`n$entries`r`n`r`n"
$changelog = $changelog.Remove($unreleased.Index, $unreleased.Length).Insert($unreleased.Index, $replacement)
Set-Content -LiteralPath $changelogPath -Value $changelog -NoNewline
Write-Host "CHANGELOG.md: promoted [Unreleased] to [$Version] - $Date" -ForegroundColor Green

# --- wpf\AssemblyInfo.cs: bump both version attributes --------------------------------------
$infoPath = Join-Path $repo "wpf\AssemblyInfo.cs"
$info = Get-Content -Raw -LiteralPath $infoPath
$info = [regex]::Replace($info, 'AssemblyVersion\("\d+\.\d+\.\d+(?:\.\d+)?"\)', ('AssemblyVersion("{0}.0")' -f $Version))
$info = [regex]::Replace($info, 'AssemblyFileVersion\("\d+\.\d+\.\d+(?:\.\d+)?"\)', ('AssemblyFileVersion("{0}.0")' -f $Version))
Set-Content -LiteralPath $infoPath -Value $info -NoNewline
Write-Host "wpf\AssemblyInfo.cs: $current -> $Version" -ForegroundColor Green

& (Join-Path $repo "scripts\Assert-ReleaseReady.ps1") -Version $Version
if ($LASTEXITCODE -ne 0) { throw "Release readiness check failed." }

if ($Commit) {
    Push-Location $repo
    try {
        git add CHANGELOG.md wpf/AssemblyInfo.cs
        if ($LASTEXITCODE -ne 0) { throw "git add failed." }
        git commit -m "chore(release): v$Version"
        if ($LASTEXITCODE -ne 0) { throw "git commit failed." }
    }
    finally { Pop-Location }
    Write-Host "`nCommitted chore(release): v$Version. Push it to main and the release workflow does the rest." -ForegroundColor Cyan
}
else {
    Write-Host "`nReview the diff, then commit CHANGELOG.md and wpf\AssemblyInfo.cs." -ForegroundColor Cyan
    Write-Host "Once that lands on main, release.yml tags v$Version and publishes the release." -ForegroundColor Cyan
}
