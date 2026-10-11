@echo off
setlocal
cd /d "%~dp0"
where msbuild >nul 2>nul
if errorlevel 1 (
    echo Open the Developer Command Prompt for Visual Studio, then run this file again.
    exit /b 1
)
msbuild AirlineReservation.sln /t:Rebuild /p:Configuration=Release /m
if errorlevel 1 exit /b 1
Tests\bin\Release\SkyWay.LogicTests.exe
if errorlevel 1 exit /b 1
echo Build and logic tests completed successfully. SQL and UI checks are listed in VALIDATION.md.
endlocal
