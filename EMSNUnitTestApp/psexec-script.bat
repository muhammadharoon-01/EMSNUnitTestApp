@echo off
setlocal

REM Use PsExec to launch application as an admin
PsExec.exe -i 1 -h -d -u POWER\muhammad.haroon -p Dawood_0123 "C:\ProgramData\Jenkins\.jenkins\workspace\EMS\EMSNUnitTestApp\bin\Debug\net8.0\EMSApplication.exe"

endlocal
