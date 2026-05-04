@echo off
chcp 65001 >nul
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish-root.ps1"
if errorlevel 1 (
    echo 发布失败
    pause
    exit /b 1
)

echo 发布完成
pause
exit /b 0
