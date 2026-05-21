@echo off
setlocal enabledelayedexpansion

rem ============================================================
rem OpenCad2D - cleanup + zip package script
rem
rem Uso:
rem   1. Copia questo file nella root del progetto OpenCad2D
rem   2. Esegui: invia.bat
rem
rem Crea uno zip con nome:
rem   yyyyMMdd_opencad2d.zip
rem
rem Contenuto incluso:
rem   - OpenCad2D.sln
rem   - README.md
rem   - docs
rem   - src
rem   - tests
rem
rem Prima della compressione elimina tutte le cartelle bin e obj
rem presenti sotto src e tests.
rem ============================================================

cd /d "%~dp0"

echo.
echo === OpenCad2D package script ===
echo Root: %CD%
echo.

if not exist "OpenCad2D.sln" (
    echo ERRORE: OpenCad2D.sln non trovato.
    echo Esegui questo script dalla root del progetto OpenCad2D.
    exit /b 1
)

if not exist "README.md" (
    echo ERRORE: README.md non trovato.
    exit /b 1
)

if not exist "docs" (
    echo ERRORE: cartella docs non trovata.
    exit /b 1
)

if not exist "src" (
    echo ERRORE: cartella src non trovata.
    exit /b 1
)

if not exist "tests" (
    echo ERRORE: cartella tests non trovata.
    exit /b 1
)

echo Pulizia cartelle bin e obj sotto src e tests...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop';" ^
    "$roots = @('src', 'tests');" ^
    "$dirs = foreach ($root in $roots) {" ^
    "    if (Test-Path -LiteralPath $root) {" ^
    "        Get-ChildItem -LiteralPath $root -Directory -Recurse -Force | Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'obj' }" ^
    "    }" ^
    "};" ^
    "$dirs = @($dirs | Sort-Object FullName -Descending);" ^
    "foreach ($dir in $dirs) {" ^
    "    Write-Host ('Cancello: ' + $dir.FullName);" ^
    "    Remove-Item -LiteralPath $dir.FullName -Recurse -Force;" ^
    "};" ^
    "Write-Host ('Cartelle eliminate: ' + $dirs.Count);"

if errorlevel 1 (
    echo ERRORE durante la pulizia di bin/obj.
    exit /b 1
)

echo.
echo Verifica pulizia bin/obj...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$remaining = @();" ^
    "foreach ($root in @('src', 'tests')) {" ^
    "    if (Test-Path -LiteralPath $root) {" ^
    "        $remaining += Get-ChildItem -LiteralPath $root -Directory -Recurse -Force | Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'obj' };" ^
    "    }" ^
    "};" ^
    "if ($remaining.Count -gt 0) {" ^
    "    Write-Host 'ERRORE: restano ancora cartelle bin/obj:';" ^
    "    $remaining | ForEach-Object { Write-Host $_.FullName };" ^
    "    exit 1;" ^
    "} else {" ^
    "    Write-Host 'Pulizia confermata: nessuna cartella bin/obj sotto src e tests.';" ^
    "}"

if errorlevel 1 (
    echo ERRORE: pulizia incompleta.
    exit /b 1
)

echo.
echo Calcolo nome zip...

for /f %%I in ('powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-Date -Format yyyyMMdd"') do set TODAY=%%I

set ZIP_NAME=%TODAY%_opencad2d.zip
set STAGING_DIR=%TEMP%\opencad2d_package_%RANDOM%%RANDOM%

echo Nome zip: %ZIP_NAME%
echo Cartella temporanea: %STAGING_DIR%
echo.

if exist "%ZIP_NAME%" (
    echo Rimuovo zip esistente: %ZIP_NAME%
    del /f /q "%ZIP_NAME%"
)

if exist "%STAGING_DIR%" (
    rmdir /s /q "%STAGING_DIR%"
)

mkdir "%STAGING_DIR%" || (
    echo ERRORE: impossibile creare la cartella temporanea.
    exit /b 1
)

echo Copia file e cartelle nello staging...
echo.

copy "OpenCad2D.sln" "%STAGING_DIR%\OpenCad2D.sln" > nul
copy "README.md" "%STAGING_DIR%\README.md" > nul

robocopy "docs" "%STAGING_DIR%\docs" /E /NFL /NDL /NJH /NJS /NP > nul
if errorlevel 8 (
    echo ERRORE durante la copia di docs.
    rmdir /s /q "%STAGING_DIR%"
    exit /b 1
)

robocopy "src" "%STAGING_DIR%\src" /E /NFL /NDL /NJH /NJS /NP > nul
if errorlevel 8 (
    echo ERRORE durante la copia di src.
    rmdir /s /q "%STAGING_DIR%"
    exit /b 1
)

robocopy "tests" "%STAGING_DIR%\tests" /E /NFL /NDL /NJH /NJS /NP > nul
if errorlevel 8 (
    echo ERRORE durante la copia di tests.
    rmdir /s /q "%STAGING_DIR%"
    exit /b 1
)

echo Creo archivio zip...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop'; Compress-Archive -Path '%STAGING_DIR%\*' -DestinationPath '%CD%\%ZIP_NAME%' -Force"

if errorlevel 1 (
    echo ERRORE durante la creazione dello zip.
    rmdir /s /q "%STAGING_DIR%"
    exit /b 1
)

echo Pulizia staging...
rmdir /s /q "%STAGING_DIR%"

echo.
echo Completato.
echo Creato: %ZIP_NAME%
echo.

endlocal
exit /b 0
