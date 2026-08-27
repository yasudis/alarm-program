# Alarm Program — Installer (Windows)

Скрипт публикует self-contained Release-сборку и копирует её в каталог установки пользователя.

## Использование

```powershell
pwsh -File .\scripts\install.ps1
```

Опционально:

```powershell
pwsh -File .\scripts\install.ps1 -InstallDir "C:\Apps\AlarmProgram" -CreateDesktopShortcut
```

## Что делает скрипт

1. `dotnet publish` через профиль `ReleaseSingleFile.pubxml`.
2. Копирует output в `%LocalAppData%\AlarmProgram` (или указанный `-InstallDir`).
3. Кладёт рядом `QUICKSTART.txt` со ссылкой на `docs/quick-start.md`.
4. По флагу создаёт ярлык на рабочем столе.

После установки запустите `AlarmProgram.UI.exe` и пройдите Quick Start.
