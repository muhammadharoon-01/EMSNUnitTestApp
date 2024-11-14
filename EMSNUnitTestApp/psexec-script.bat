@echo off
setlocal

REM Use PsExec to launch application as an admin
PsExec.exe -i 1 -h -d -accepteula -u POWER\muhammad.haroon -p Dawood_0123!! "J:\SW-E2E\01-ATM Repository\Code Workspace\Working\EMSApplication.exe"

endlocal
