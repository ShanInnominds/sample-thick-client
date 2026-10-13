@echo off
rem Build and launch Order Desk (sample thick client).
rem
rem   start_order_desk.bat                       build, then launch
rem   start_order_desk.bat --no-build            launch the existing app\bin\OrderDesk.exe
rem   start_order_desk.bat --password secret     sign-in password for this run (else ORDERDESK_PASSWORD,
rem                                              else the default "orderdesk")
rem
rem The build takes about a second, so it always runs: the exe never lags behind the source.
setlocal
set "HERE=%~dp0"
set "EXE=%HERE%app\bin\OrderDesk.exe"
set "BUILD=1"

:args
if "%~1"=="" goto :start
if /I "%~1"=="--no-build" ( set "BUILD=0" & shift & goto :args )
if /I "%~1"=="--password" ( set "ORDERDESK_PASSWORD=%~2" & shift & shift & goto :args )
echo Unknown option: %~1 1>&2
echo Usage: start_order_desk.bat [--no-build] [--password PASSWORD] 1>&2
exit /b 2

:start
if "%BUILD%"=="1" (
    call "%HERE%app\build.bat"
    if errorlevel 1 exit /b 1
)
if not exist "%EXE%" (
    echo [start] %EXE% not found - run without --no-build. 1>&2
    exit /b 1
)
start "" "%EXE%"
echo [start] Order Desk launched. Sign in with any user name; password: %%ORDERDESK_PASSWORD%% or "orderdesk".
