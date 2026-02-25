@echo off
setlocal

set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=dev"

set "ROOT=%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%tools\test-latest.ps1" -Version "%VERSION%"
exit /b %ERRORLEVEL%
