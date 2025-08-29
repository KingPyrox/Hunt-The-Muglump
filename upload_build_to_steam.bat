@echo off
echo Building and uploading Hunt the Muglump to Steam...
echo.

REM Set the path to your SteamCMD installation
set STEAMCMD_PATH=C:\steamcmd\steamcmd.exe

REM Check if SteamCMD exists
if not exist "%STEAMCMD_PATH%" (
    echo ERROR: SteamCMD not found at %STEAMCMD_PATH%
    echo Please install SteamCMD or update the path in this script
    pause
    exit /b 1
)

REM Login to Steam (you'll be prompted for credentials and Steam Guard)
echo Logging into Steam...
"%STEAMCMD_PATH%" +login YOUR_STEAM_USERNAME +run_app_build ..\app_build_1193210.vdf +quit

echo.
echo Build upload complete!
pause