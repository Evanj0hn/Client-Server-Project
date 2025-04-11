@echo off
SET /P IP="Enter server IP: "
SET /P FILE="Enter Data File Name (must be in 'data' folder): "
SET /A "index = 1"
SET /A "count = 150"

:while
@echo %time%
:spawnloop
if %index% leq %count% (
    START /MIN TelemetryClient.exe %IP% "%FILE%"
    SET /A index = %index% + 1
    @echo Started client #%index%
    goto :spawnloop
)
timeout 300 > NUL  :: 5 minutes wait
SET /A index = 1
goto :while
