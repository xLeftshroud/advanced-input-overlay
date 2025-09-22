@echo off
echo ========================================
echo  Advanced Input Overlay - Update and Start
echo ========================================
echo.

REM Check if dotnet CLI is available
dotnet --version >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Using .NET CLI for building...
    goto :dotnet_build
) else (
    echo .NET CLI not found, trying Visual Studio MSBuild...
    goto :msbuild_approach
)

:dotnet_build
echo [1/4] Cleaning previous build...
dotnet clean advanced-input-overlay.sln --configuration Release --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo ERROR: Clean failed!
    pause
    exit /b 1
)

echo [2/4] Building solution...
dotnet build advanced-input-overlay.sln --configuration Release --verbosity minimal
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
goto :build_success

:msbuild_approach
REM Try to find Visual Studio and setup build environment
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
        set "VSINSTALLDIR=%%i"
    )
)

REM Setup Visual Studio environment
if defined VSINSTALLDIR (
    echo Setting up Visual Studio environment...
    call "%VSINSTALLDIR%\Common7\Tools\VsDevCmd.bat" -no_logo >nul 2>&1
) else (
    echo ERROR: Could not find Visual Studio or .NET SDK!
    echo Please install Visual Studio with .NET workload or .NET SDK.
    pause
    exit /b 1
)

echo [1/4] Cleaning previous build...
msbuild advanced-input-overlay.sln /t:Clean /p:Configuration=Release /p:Platform=x64 /verbosity:minimal
if %ERRORLEVEL% neq 0 (
    echo ERROR: Clean failed!
    pause
    exit /b 1
)

echo [2/4] Building solution...
msbuild advanced-input-overlay.sln /p:Configuration=Release /p:Platform=x64 /verbosity:minimal
if %ERRORLEVEL% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

:build_success

echo [3/4] Build completed successfully!
echo.

echo [4/4] Starting applications...

REM Start the core application (adjust path if needed)
if exist "x64\Release\InputOverlayCore.exe" (
    echo Starting InputOverlayCore...
    start "" "x64\Release\InputOverlayCore.exe"
) else if exist "InputOverlayCore\x64\Release\InputOverlayCore.exe" (
    echo Starting InputOverlayCore...
    start "" "InputOverlayCore\x64\Release\InputOverlayCore.exe"
) else (
    echo Warning: InputOverlayCore.exe not found in expected location
)

REM Start the UI application (adjust path if needed)
if exist "InputOverlayUI\bin\Release\InputOverlayUI.exe" (
    echo Starting InputOverlayUI...
    start "" "InputOverlayUI\bin\Release\InputOverlayUI.exe"
) else if exist "InputOverlayUI\bin\Release\net8.0-windows\InputOverlayUI.exe" (
    echo Starting InputOverlayUI...
    start "" "InputOverlayUI\bin\Release\net8.0-windows\InputOverlayUI.exe"
) else (
    echo Warning: InputOverlayUI.exe not found in expected location
)

echo.
echo All done! Applications should be starting...
echo Press any key to close this window.
pause >nul