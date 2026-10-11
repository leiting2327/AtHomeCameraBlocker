@echo off
REM ============================================================
REM  AtHome Camera Network Blocker - one-click build script
REM  Output: AtHomeCameraBlocker.exe (single file, ~15 KB)
REM ============================================================
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set OUT=AtHomeCameraBlocker.exe
set ICONOPT=

if not exist "%CSC%" (
    echo [ERROR] csc.exe not found at %CSC%
    echo Please install .NET Framework 4.x or adjust the CSC path.
    exit /b 1
)

REM The custom icon is optional. Generate it with:  powershell -File src\generate-icon.ps1
if exist "src\app.ico" set ICONOPT=/win32icon:"src\app.ico"

echo [1/2] Compiling...
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ ^
  /out:"%OUT%" ^
  %ICONOPT% ^
  /win32manifest:"src\app.manifest" ^
  /reference:"System.Windows.Forms.dll" ^
  /reference:"System.Drawing.dll" ^
  /reference:"System.dll" ^
  "src\Program.cs"

if errorlevel 1 (
    echo [ERROR] Build failed.
    exit /b 1
)

echo [2/2] Done.
for %%A in ("%OUT%") do echo Output: %%~fA  (%%~zA bytes)
endlocal
