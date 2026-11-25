@echo off
title TrapShack Tweak Utility - Build
REM Double-click this file. It builds the app and opens the folder with the finished .exe.
REM Extra options: build.bat -Run   (start it after building)
REM                build.bat -Clean (wipe old build files first)
REM                build.bat -Debug (quick debug build)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
echo.
pause
