@echo off
rem Build Order Desk (sample thick client) with the C# compiler that ships with Windows (.NET Framework 4.x).
rem Output: app\bin\OrderDesk.exe
setlocal
set "HERE=%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo [build] .NET Framework 4.x C# compiler not found. 1>&2
    exit /b 1
)
if not exist "%HERE%bin" mkdir "%HERE%bin"
"%CSC%" /nologo /target:winexe /optimize+ /out:"%HERE%bin\OrderDesk.exe" ^
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "%HERE%OrderDesk.cs"
if errorlevel 1 exit /b 1
echo [build] %HERE%bin\OrderDesk.exe
