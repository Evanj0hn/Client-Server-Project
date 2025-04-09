@echo off
SET /P IP="Enter server IP: "
SET /P FILE="Enter data file name (must be in 'data' folder): "
SET /A "index = 1"
SET /A "count = 200"

:loop
if %index% leq %count% (
    START /MIN TelemetryClient.exe %IP% "%FILE%"
    SET /A index = %index% + 1
    @echo Started client #%index% with file "%FILE%"
    goto loop
)


