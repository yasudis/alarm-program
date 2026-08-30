# Alarm Program

Desktop-приложение для мониторинга состояния Windows-машины и отправки уведомлений в Telegram, Discord, HTTPS webhook и Email (SMTP).

## Что умеет

- Отслеживает системные события: startup/shutdown/restart, сеть, питание, сессии, USB, RDP и др.
- Поддерживает heartbeat, quiet hours, digest-уведомления и фильтрацию по критичности.
- Хранит журнал алертов и outbox для повторной отправки.
- Защищает секреты в локальных настройках (DPAPI).

## Требования

### Для запуска из исходников

- Windows 10/11 (x64).
- .NET SDK 10.0+.
- PowerShell 7+ (опционально, для `scripts/install.ps1`).

> Проект использует WPF и `net10.0-windows`, поэтому запуск UI поддерживается на Windows.

## Структура репозитория

- `src/AlarmProgram.UI` — WPF-приложение.
- `src/AlarmProgram.Application` — прикладная логика.
- `src/AlarmProgram.Infrastructure` — интеграции с ОС, каналами и хранилищами.
- `src/AlarmProgram.Domain` — доменные модели и валидация.
- `tests/*` — unit и integration тесты.
- `docs/*` — пользовательская и релизная документация.

## Запуск проекта из исходников

Из корня репозитория:

```powershell
dotnet restore .\AlarmProgram.sln
dotnet build .\AlarmProgram.sln -c Debug
dotnet run --project .\src\AlarmProgram.UI\AlarmProgram.UI.csproj -c Debug
```

После запуска откроется главное окно приложения.

## Сборка и запуск релизной версии

### Вариант 1: скрипт установки (рекомендуется)

```powershell
pwsh -File .\scripts\install.ps1
```

С установкой в кастомный каталог и ярлыком:

```powershell
pwsh -File .\scripts\install.ps1 -InstallDir "C:\Apps\AlarmProgram" -CreateDesktopShortcut
```

По умолчанию приложение устанавливается в `%LocalAppData%\AlarmProgram`.

### Вариант 2: вручную через publish

```powershell
dotnet publish .\src\AlarmProgram.UI\AlarmProgram.UI.csproj -c Release -p:PublishProfile=ReleaseSingleFile -o .\artifacts\publish\ReleaseSingleFile
```

Далее запустите:

```text
artifacts\publish\ReleaseSingleFile\AlarmProgram.UI.exe
```

## Как настраивать приложение

### 1) Базовая настройка после первого запуска

1. Включите нужный канал уведомлений (обычно начинают с Telegram).
2. Заполните обязательные поля канала.
3. Нажмите **Сохранить настройки**.
4. Нажмите **Тестовая отправка** и проверьте, что сообщение пришло.

Подробный пользовательский сценарий: `docs/quick-start.md`.

### 2) Где хранятся настройки

- Пользовательские настройки: `%AppData%\AlarmProgram\settings.json`
- Логи: `%AppData%\AlarmProgram\logs`
- Журнал алертов: `%AppData%\AlarmProgram\alert-journal.json`
- Outbox: `%AppData%\AlarmProgram\alert-outbox.json`

Секретные поля (токены/пароли/webhook URL) сохраняются в защищенном виде через DPAPI.

### 3) Низкоуровневая конфигурация (`appsettings.json`)

Файл: `src/AlarmProgram.UI/appsettings.json`

| Секция | Назначение |
|---|---|
| `App` | Имя приложения и окружение (`Environment`) |
| `Logging:LogLevel` | Уровни логирования |
| `Logging:File` | Путь к rolling-логам и глубина хранения |
| `Notifications` | Политика retry отправки |
| `Monitoring` | Интервалы опроса и дедупликации |
| `AlertJournal` | Файл и лимит записей журнала |
| `AlertOutbox` | Файл и лимит очереди повторной отправки |

Детальное описание параметров: `docs/configuration.md`.

## Проверка изменений и тесты

```powershell
dotnet test .\AlarmProgram.sln -c Debug
```

## Дополнительная документация

- `docs/quick-start.md` — первичный запуск для пользователя.
- `docs/configuration.md` — подробная настройка параметров.
- `docs/installer.md` — установка через скрипт.
- `docs/release-e2e-checklist.md` — чеклист перед релизом.
