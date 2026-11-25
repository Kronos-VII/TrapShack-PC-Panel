<#
  TrapShack Tweak Utility - build script.

  Easiest: double-click build.bat.

  From PowerShell:
      .\build.ps1            builds one 'TrapShack Tweak Utility.exe' into .\dist and opens that folder
      .\build.ps1 -Run       build, then start it (it asks for administrator rights)
      .\build.ps1 -Clean     delete bin, obj and dist first (use this if a build acts strangely)
      .\build.ps1 -Debug     quick debug build into .\bin, no packaging

  If the .NET 8 SDK is missing, this script offers to install it for you using winget.
#>
param(
    [switch]$Run,
    [switch]$NoOpen,
    [switch]$Debug,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

function Step($t) { Write-Host ""; Write-Host "== $t" -ForegroundColor Cyan }
function Fail($t) { Write-Host ""; Write-Host $t -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "  TRAPSHACK TWEAK UTILITY  -  build" -ForegroundColor Red

# ---- 1. .NET SDK --------------------------------------------------------------------------
function Get-SdkMajor {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { return 0 }
    try {
        $v = (& dotnet --version 2>$null | Select-Object -First 1)
        if ($v -match '^(\d+)\.') { return [int]$Matches[1] }
    } catch {}
    return 0
}

Step "Checking for the .NET SDK"
if ((Get-SdkMajor) -lt 8) {
    Write-Host "The .NET 8 SDK (or newer) is needed and was not found."
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        $a = Read-Host "Install it now with winget? (Y/n)"
        if ($a -eq '' -or $a -match '^[Yy]') {
            & winget install --id Microsoft.DotNet.SDK.8 -e --accept-package-agreements --accept-source-agreements
            # winget updates PATH for new windows only, so pick it up here.
            $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' +
                        [Environment]::GetEnvironmentVariable('Path', 'User')
        }
    }
    if ((Get-SdkMajor) -lt 8) {
        Fail ("Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 , then run build.bat again.`n" +
              "(If you just installed it, close this window and double-click build.bat once more.)")
    }
}
Write-Host ("Found .NET SDK " + (& dotnet --version)) -ForegroundColor Green

# ---- 1b. Tidy: only 'TrapShack Tweak Utility.csproj' may exist -------------------------------------------
Step "Checking project files"
Get-ChildItem -Path $PSScriptRoot -Recurse -Include *.csproj, *.sln, *.slnx -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|dist)\\' -and $_.Name -ne 'TrapShack Tweak Utility.csproj' } |
    ForEach-Object { Write-Host ("Removing leftover project file: " + $_.Name) -ForegroundColor Yellow; Remove-Item $_.FullName -Force }
# Stale restore data from an older project would hide the new package list, so always start from a fresh obj folder.
if (Test-Path (Join-Path $PSScriptRoot 'obj')) { Remove-Item (Join-Path $PSScriptRoot 'obj') -Recurse -Force }
Step "Restoring packages (needs internet the first time)"
& dotnet restore 'TrapShack Tweak Utility.csproj' -nologo
if ($LASTEXITCODE -ne 0) { Fail "Package restore failed. Check your internet connection and run build.bat again." }

# ---- 2. Clean ----------------------------------------------------------------------------
if ($Clean) {
    Step "Cleaning old build files"
    foreach ($d in 'bin', 'obj', 'dist') { if (Test-Path $d) { Remove-Item $d -Recurse -Force } }
}

$log = Join-Path $PSScriptRoot 'build.log'

# ---- 3. Build ----------------------------------------------------------------------------
if ($Debug) {
    Step "Building (Debug)"
    & dotnet build 'TrapShack Tweak Utility.csproj' -c Debug -nologo 2>&1 | Tee-Object -FilePath $log
    if ($LASTEXITCODE -ne 0) { Fail "Build failed. The full output is saved in build.log." }
    Write-Host "Done: bin\Debug\net8.0-windows\TrapShackTweakUtility.exe" -ForegroundColor Green
    exit 0
}

Step "Building TrapShack Tweak Utility (Release, single file). The first build downloads packages and takes a minute or two"
& dotnet publish 'TrapShack Tweak Utility.csproj' -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true -nologo -o dist 2>&1 | Tee-Object -FilePath $log
if ($LASTEXITCODE -ne 0) {
    Fail "Build failed. The full output is saved in build.log. Send me that file and I'll fix it."
}

$built = Join-Path $PSScriptRoot 'dist\TrapShackTweakUtility.exe'
if (-not (Test-Path $built)) { Fail "The build said it finished, but $built is missing." }
# Give the finished program its proper name.
$exe = Join-Path $PSScriptRoot 'dist\TrapShack Tweak Utility.exe'
if (Test-Path $exe) { Remove-Item $exe -Force }
Move-Item $built $exe
Get-ChildItem (Join-Path $PSScriptRoot 'dist') -Filter *.pdb -ErrorAction SilentlyContinue | Remove-Item -Force

$mb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "Done!  dist\TrapShack Tweak Utility.exe  ($mb MB)" -ForegroundColor Green
Write-Host "One file. Runs on any 64-bit Windows 10/11 PC with nothing else installed."

if ($Run) { Start-Process -FilePath $exe }
elseif (-not $NoOpen) { Start-Process explorer.exe -ArgumentList "/select,`"$exe`"" }
