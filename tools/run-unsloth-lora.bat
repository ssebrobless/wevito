@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-unsloth-lora.ps1" %*
exit /b %ERRORLEVEL%
