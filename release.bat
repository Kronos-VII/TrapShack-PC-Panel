@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"
title TrapShack Tweak Utility - publish an update
REM Double-click this to send a new version to everyone's app.
REM It raises the version number, builds the program, and publishes it as a GitHub release.
REM The app's "Check for updates" button finds that release and installs it.

set "REPO=Kronos-VII/TrapShack-PC-Panel"

where gh >nul 2>&1
if errorlevel 1 (
  echo The GitHub CLI is needed once. Installing it with winget...
  winget install --id GitHub.cli -e --accept-source-agreements --accept-package-agreements
  echo Close this window and double-click release.bat again.
  pause
  exit /b 1
)
gh auth status >nul 2>&1
if errorlevel 1 (
  echo Sign in to GitHub - a browser window opens...
  gh auth login --web
)

echo.
echo == Raising the version number
for /f "delims=" %%v in ('powershell -NoProfile -Command "$p = Join-Path (Get-Location) 'TrapShack Tweak Utility.csproj'; [xml]$x = Get-Content -LiteralPath $p; $n = $x.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1; $v = [version]$n.Version; $nv = '{0}.{1}.{2}' -f $v.Major, $v.Minor, ($v.Build + 1); $n.Version = $nv; $x.Save($p); $nv"') do set "VER=%%v"
if "!VER!"=="" (
  echo Could not read the version number.
  pause
  exit /b 1
)
echo New version: !VER!

echo.
echo == Building
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" -NoOpen
if errorlevel 1 (
  echo Build failed, nothing was published.
  pause
  exit /b 1
)

echo.
set "NOTES="
set /p NOTES=What changed in this version? (one line): 
if "!NOTES!"=="" set "NOTES=Improvements and fixes."

echo.
echo == Publishing v!VER! on GitHub
gh release create "v!VER!" "dist\TrapShack Tweak Utility.exe" --repo "%REPO%" --title "TrapShack Tweak Utility v!VER!" --notes "!NOTES!"
if errorlevel 1 (
  echo Publishing failed. Make sure the code was pushed at least once with push.bat, then try again.
  pause
  exit /b 1
)
echo.
echo Done. Everyone's app will now find v!VER! under Settings, Check for updates.
pause
