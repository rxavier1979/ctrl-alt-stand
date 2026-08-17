# Prints the shipping version (major.minor.patch) of Ctrl+Alt+Stand.
#
# wpf\AssemblyInfo.cs is the single source of truth: the release workflow reads the version from
# here, so bumping it is what causes a release to be cut. The legacy WinForms source in src\ is
# frozen and carries its own, unrelated version — it is deliberately ignored here.

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$infoPath = Join-Path $repo "wpf\AssemblyInfo.cs"
if (-not (Test-Path -LiteralPath $infoPath)) { throw "Not found: $infoPath" }

$text = Get-Content -Raw -LiteralPath $infoPath

$asm = [regex]::Match($text, 'AssemblyVersion\("(?<v>\d+\.\d+\.\d+)(?:\.\d+)?"\)')
if (-not $asm.Success) { throw "Could not read AssemblyVersion from $infoPath." }

$file = [regex]::Match($text, 'AssemblyFileVersion\("(?<v>\d+\.\d+\.\d+)(?:\.\d+)?"\)')
if (-not $file.Success) { throw "Could not read AssemblyFileVersion from $infoPath." }

if ($asm.Groups['v'].Value -ne $file.Groups['v'].Value) {
    throw ("AssemblyVersion ({0}) and AssemblyFileVersion ({1}) disagree in {2}. " -f `
        $asm.Groups['v'].Value, $file.Groups['v'].Value, $infoPath)
}

$asm.Groups['v'].Value
