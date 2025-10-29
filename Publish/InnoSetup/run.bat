@echo off
REM ============================================================
REM Simple Inno Setup build script (current directory)
REM ============================================================

REM Current directory
SET CUR_DIR=%~dp0

REM ISS file
SET ISS_FILE=%CUR_DIR%ScInstaller.iss

REM ISCC.exe path (portable, current directory)
SET ISCC_PATH=%CUR_DIR%ISCC.exe

REM Compile
"%ISCC_PATH%" "%ISS_FILE%"

REM Check compilation result
IF %ERRORLEVEL% EQU 0 (
    echo Build succeeded!
) ELSE (
    echo Build failed! Error code: %ERRORLEVEL%
)

pause
