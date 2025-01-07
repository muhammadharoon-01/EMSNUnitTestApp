@echo off
setlocal

REM Use PsExec to launch application as an admin
PsExec.exe -i 1 -h -d -u POWER\muhammad.haroon -p Abdul_0123 "J:\GitHub\EMSNUnitTestApp\EMSNUnitTestApp\bin\Debug\net8.0\EMSApplication.exe"

endlocal
