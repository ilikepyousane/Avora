@echo off
set PATH=C:\Program Files\Git\cmd;%PATH%
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set PROJECT=C:\Users\MECHREVO\Desktop\Avora\Avora\Avora.csproj
set EXE=C:\Users\MECHREVO\Desktop\Avora\Avora\bin\x64\Unpacked\net8.0-windows10.0.22000.0\win-x64\Avora.exe

echo === Building Avora ===
%MSBUILD% %PROJECT% /t:Build /p:Configuration=Unpacked /p:Platform=x64 /v:minimal

if %ERRORLEVEL% NEQ 0 (
    echo Build FAILED
    pause
    exit /b 1
)

echo === Launching Avora ===
taskkill /f /im Avora.exe 2>nul
timeout /t 1 /nobreak >nul
start "" %EXE%
echo Done.
