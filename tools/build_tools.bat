@echo off
rem Build tools\bin\UiaReport.exe (native UIA3 report) with the C# compiler that ships with Windows.
rem Generates the UI Automation interop assembly from UIAutomationCore.dll on first use (make_interop.ps1).
setlocal
set "HERE=%~dp0"
set "BIN=%HERE%bin"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [build_tools] .NET Framework 4.x C# compiler not found. 1>&2
    exit /b 1
)
if not exist "%BIN%" mkdir "%BIN%"
if not exist "%BIN%\Interop.UIAutomationClient.dll" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%HERE%make_interop.ps1" -OutDir "%BIN%" >nul
    if errorlevel 1 exit /b 1
)
"%CSC%" /nologo /optimize+ /out:"%BIN%\UiaReport.exe" /r:"%BIN%\Interop.UIAutomationClient.dll" ^
    /r:System.Web.Extensions.dll /r:System.Core.dll "%HERE%UiaReport.cs"
if errorlevel 1 exit /b 1
echo [build_tools] %BIN%\UiaReport.exe
