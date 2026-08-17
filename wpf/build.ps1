# Build Ctrl+Alt+Stand (WPF) with the in-box .NET Framework MSBuild -- no SDK or Visual Studio required.
# Output: dist\CtrlAltStand.exe
#
# Falls back to a modern 'msbuild' or 'dotnet' if either is on PATH.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    $fw64 = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    $fw32 = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

    if (Test-Path $fw64)       { $msbuild = $fw64 }
    elseif (Test-Path $fw32)   { $msbuild = $fw32 }
    elseif (Get-Command msbuild -ErrorAction SilentlyContinue) { $msbuild = "msbuild" }
    else { $msbuild = $null }

    if ($msbuild) {
        Write-Host "Building with: $msbuild" -ForegroundColor Cyan
        & $msbuild .\CtrlAltStand.csproj /p:Configuration=Release /nologo /v:minimal
        if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE." }
    }
    elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
        dotnet build .\CtrlAltStand.csproj -c Release
    }
    else {
        throw "No MSBuild found. Expected the in-box .NET Framework MSBuild at $fw64."
    }

    Write-Host "`nBuilt: $(Join-Path $root 'dist\CtrlAltStand.exe')" -ForegroundColor Green
}
finally {
    Pop-Location
}
