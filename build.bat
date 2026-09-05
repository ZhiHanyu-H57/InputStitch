@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
if errorlevel 1 (
    echo.
    echo Release build failed. Review the error above.
    exit /b 1
)

echo.
echo Release files are ready in: %CD%\dist
exit /b 0
