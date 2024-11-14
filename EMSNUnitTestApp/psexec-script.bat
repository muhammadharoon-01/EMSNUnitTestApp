@echo off
setlocal

REM Use PsExec to launch application as an admin
PsExec.exe -i 1 -h -d -u POWER\ibrar.sakhi -p Zaroon7890@ "C:\Program Files (x86)\Smart Wires\SmartInterface\SmartInterface.exe"

endlocal
