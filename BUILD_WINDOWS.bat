@echo off
setlocal
if "%UNITY_PATH%"=="" (
  echo Set UNITY_PATH to your Unity.exe, for example:
  echo set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe
  echo Then run this file again.
  pause
  exit /b 1
)
"%UNITY_PATH%" -batchmode -quit -projectPath "%~dp0" -executeMethod WhiteNoiseBuild.BuildWindows -logFile "%~dp0Build\build.log"
if errorlevel 1 (
  echo Build failed. Check Build\build.log
  pause
  exit /b 1
)
echo Build finished. Output: Build\Windows\WHITE_NOISE.exe
pause
