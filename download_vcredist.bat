@echo off
echo Downloading Visual C++ Redistributables...
echo.

REM Download VC++ 2019 x64 redistributable
curl -L -o vc_redist.x64.exe https://aka.ms/vs/17/release/vc_redist.x64.exe

if %errorlevel% neq 0 (
    echo ERROR: Failed to download VC++ redistributable
    pause
    exit /b 1
)

echo.
echo Download complete! vc_redist.x64.exe has been saved to the project folder.
echo This file should be included with your Steam build.
pause