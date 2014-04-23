@echo off
setlocal
cd "%~dp0"
call build ^
&& call :mkdir dist ^
&& call :pack AppUpdater dist ^
&& call :pack AppUpdater.Runner.Source dist
goto :EOF

:mkdir
if exist %1 exit /b 0
mkdir %1

:pack
nuget pack -OutputDirectory %2 %1.nuspec
