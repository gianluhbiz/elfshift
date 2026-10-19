@echo off
rem Scarica gli ultimi file del gioco dal repository (serve Git installato).
rem Mettilo nella cartella del progetto Unity e fai doppio clic.
cd /d "%~dp0"
git fetch origin claude/friendly-planck-b1zodj
if errorlevel 1 goto errore
git checkout -f -B claude/friendly-planck-b1zodj origin/claude/friendly-planck-b1zodj
if errorlevel 1 goto errore
echo.
echo Fatto! Torna in Unity: ricompilera' da solo. Poi rilancia Elfshift - Fase 1 - 2 - Costruisci il laboratorio.
pause
exit /b 0
:errore
echo.
echo Qualcosa e' andato storto. Controlla di aver installato Git e di aver fatto la configurazione iniziale.
pause
exit /b 1
