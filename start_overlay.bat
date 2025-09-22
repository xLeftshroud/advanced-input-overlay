@echo off
echo Starting Advanced Input Overlay Software...

echo Starting C++ Core Engine...
start "Core" cmd /k "cd /d InputOverlayCore\x64\Release && InputOverlayCore.exe"

timeout /t 3 /nobreak >nul

echo Starting WPF UI...
start "UI" cmd /k "cd /d InputOverlayUI\bin\Release\net8.0-windows && InputOverlayUI.exe"

echo Both applications started!
pause