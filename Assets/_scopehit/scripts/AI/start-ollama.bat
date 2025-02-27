@echo off
title Ollama Server Starter
cls

:: Als Administrator ausführen
NET FILE 1>NUL 2>NUL
if '%errorlevel%' == '0' ( goto :run 
) else (
    echo Starte als Administrator...
    powershell -Command "Start-Process '%~dpnx0' -Verb RunAs"
    exit /b
)

:run
echo Ollama Server wird gestartet...
echo.
echo Bitte dieses Fenster offen lassen, solange Sie Ollama verwenden moechten.
echo Zum Beenden einfach dieses Fenster schliessen.
echo.
echo Server-IP-Adressen in Ihrem Netzwerk:
ipconfig | findstr "IPv4"
echo.
echo ----------------------------------------------------
echo Der Server ist unter folgender Adresse erreichbar:
echo http://IHRE-IP:11434/api/generate
echo ----------------------------------------------------
echo.

:: Setze Umgebungsvariable und starte Ollama
set OLLAMA_HOST=0.0.0.0
ollama serve

:: Fehlerbehandlung
if %errorlevel% neq 0 (
    echo.
    echo Ein Fehler ist aufgetreten! 
    echo Bitte pruefen Sie, ob Ollama korrekt installiert ist.
    echo.
    pause
    exit /b 1
)

pause
