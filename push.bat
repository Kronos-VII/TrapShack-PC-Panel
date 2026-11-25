@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"
title TrapShack Tweak Utility - push to GitHub
set "REPO=https://github.com/Kronos-VII/TrapShack-PC-Panel.git"

where git >nul 2>&1
if errorlevel 1 (
  echo Git is not installed. Get it from https://git-scm.com/download/win and run this again.
  pause & exit /b 1
)

if not exist ".git" (
  echo First run: setting up git in this folder...
  git init -b main >nul
  git remote add origin %REPO%
)
git remote get-url origin >nul 2>&1 || git remote add origin %REPO%
git branch -M main >nul 2>&1

git config user.name >nul 2>&1 || (
  set /p GN=Your name for commits: 
  git config user.name "!GN!"
)
git config user.email >nul 2>&1 || (
  set /p GE=Your GitHub email for commits: 
  git config user.email "!GE!"
)

echo.
git status --short
echo.
set "MSG="
set /p MSG=Describe this update (Enter for "Update"): 
if "!MSG!"=="" set "MSG=Update"

git add -A
git commit -m "!MSG!"
if errorlevel 1 echo (Nothing new to commit - pushing what is already committed.)

echo.
echo Pushing to %REPO% ...
git push -u origin main
if errorlevel 1 (
  echo.
  echo Push failed. If GitHub asked you to sign in, finish that and run push.bat again.
  echo If it says the remote already has other files, tell me and I will give you the one-line fix.
  pause & exit /b 1
)

REM ---- to publish an update for the in-app "Check for updates" button, run release.bat ----
echo.
echo To send an update to everyone's app, double-click release.bat.

echo.
echo Done.
pause
