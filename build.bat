@echo off
setlocal

set SOLUTION_DIR=%~dp0
set OUTPUT_FILE=%SOLUTION_DIR%build-output.txt

echo ============================================================ > "%OUTPUT_FILE%"
echo BUILD - NetflixSkipIntroMensageria >> "%OUTPUT_FILE%"
echo Data/Hora: %DATE% %TIME% >> "%OUTPUT_FILE%"
echo ============================================================ >> "%OUTPUT_FILE%"
echo. >> "%OUTPUT_FILE%"

echo Compilando solution...
dotnet build "%SOLUTION_DIR%NetflixSkipIntroMensageria.slnx" --configuration Debug >> "%OUTPUT_FILE%" 2>&1

echo. >> "%OUTPUT_FILE%"
echo ============================================================ >> "%OUTPUT_FILE%"
echo FIM DO BUILD >> "%OUTPUT_FILE%"
echo ============================================================ >> "%OUTPUT_FILE%"

echo.
echo Saida gravada em: %OUTPUT_FILE%
echo Abrindo arquivo...
start notepad "%OUTPUT_FILE%"
