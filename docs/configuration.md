# Alarm Program — Configuration Reference

Документ описывает, как настраивать приложение после запуска и какие ограничения проверяются валидатором.

## 1. Источники конфигурации

### `appsettings.json` (технические параметры приложения)

Файл находится в `src/AlarmProgram.UI/appsettings.json` и копируется в выходную папку при сборке.

| Ключ | Значение по умолчанию | Назначение |
|---|---|---|
| `App:ApplicationName` | `Alarm Program` | Имя приложения в логах и UI-контексте |
| `App:Environment` | `Development` | Имя окружения |
| `Logging:File:Path` | `%AppData%/AlarmProgram/logs/alarm-.log` | Путь к rolling-логам |
| `Logging:File:RetainedFileCountLimit` | `14` | Сколько файлов лога хранить |
| `Notifications:DefaultRetryCount` | `3` | Количество повторных попыток отправки |
| `Notifications:RetryDelaySeconds` | `2` | Пауза между retry |
| `Monitoring:PollIntervalSeconds` | `30` | Интервал опроса источников событий |
| `Monitoring:InitialLookbackMinutes` | `10` | Lookback при старте |
| `Monitoring:RecoveryLookbackHours` | `24` | Lookback после восстановления |
| `Monitoring:DeduplicationWindowSeconds` | `180` | Окно дедупликации алертов |
| `Monitoring:DefaultHeartbeatIntervalMinutes` | `60` | Дефолт heartbeat-интервал |
| `AlertJournal:FilePath` | `%AppData%/AlarmProgram/alert-journal.json` | Файл журнала алертов |
| `AlertJournal:MaxEntries` | `100` | Максимум записей журнала |
| `AlertOutbox:FilePath` | `%AppData%/AlarmProgram/alert-outbox.json` | Файл outbox |
| `AlertOutbox:MaxItems` | `200` | Лимит элементов outbox |

### `settings.json` (пользовательские настройки)

Файл хранится в `%AppData%/AlarmProgram/settings.json`.

- Большинство полей меняется через UI.
- Секреты (`TelegramBotToken`, `DiscordWebhookUrl`, `WebhookUrl`, `SmtpPassword`) сохраняются в зашифрованном виде (DPAPI).

## 2. Настройка каналов уведомлений

### Telegram

Обязательные поля при `TelegramEnabled=true`:

- `TelegramBotToken` — формат `123456789:...` (8-12 цифр, затем токен).
- `TelegramChatId` — username (`@name`) или numeric id (`-100...` и т.п.).

Можно указывать несколько chat id через `,`, `;`, пробел или перенос строки.

### Discord

При `DiscordEnabled=true`:

- обязателен `DiscordWebhookUrl`;
- URL должен быть `https://...` и относиться к `discord.com` / `discordapp.com`;
- путь должен начинаться с `/api/webhooks/`.

### HTTPS webhook

При `WebhookEnabled=true`:

- обязателен `WebhookUrl`;
- только `https`;
- длина URL до 2048 символов.

### Email (SMTP)

При `EmailEnabled=true`:

- `SmtpHost` обязателен, максимум 253 символа;
- `SmtpPort` от 1 до 65535;
- `SmtpFrom` — валидный email;
- `SmtpTo` — минимум 1 и максимум 10 email-адресов.

Несколько получателей в `SmtpTo` можно задавать через `,`, `;`, пробел или новую строку.

## 3. Настройка мониторинга и лимитов

Ключевые ограничения:

- `HeartbeatIntervalMinutes`: от 5 до 1440.
- `JournalRetentionDays`: от 0 до 365 (`0` = без автоочистки).
- `LowDiskSpaceThresholdPercent`: от 1 до 50.
- `BatteryLowThresholdPercent`: от 1 до 50.
- `HighCpuThresholdPercent`: от 50 до 99.
- `HighMemoryThresholdPercent`: от 50 до 99.
- `AlertCooldownMinutes`: от 0 до 1440.
- `StartupGracePeriodMinutes`: от 0 до 60.
- `MaxAlertsPerHour`: от 0 до 200 (`0` = без лимита).

## 4. Watchdog-настройки

### Процессы (`NotifyOnProcessDown`)

- `WatchedProcessNames` обязателен при включении.
- Допустимо до 10 процессов.
- Формат: имя процесса без пути (`nginx`, `notepad`, `app.exe`).
- `.exe` допускается и нормализуется автоматически.

Разделители: `,`, `;`, перенос строки.

### Службы (`NotifyOnServiceDown`)

- `WatchedServiceNames` обязателен при включении.
- До 10 служб.
- Указывать нужно Service Name (например, `Spooler`, `wuauserv`).

Разделители: `,`, `;`, перенос строки.

### Хосты (`NotifyOnHostUnreachable` / `NotifyOnHostRestored`)

- `WatchedHosts` обязателен при включении.
- До 10 хостов.
- Можно IP или DNS-имя (`8.8.8.8`, `nas.local`).
- Не допускаются пробелы и символы пути.

Разделители: `,`, `;`, пробел, перенос строки.

## 5. Пользовательские Event ID

Для `NotifyOnCustomEvent=true`:

- `CustomEventIds` обязателен;
- допустимый диапазон: 1..65535;
- максимум 20 id.

Разделители: `,`, `;`, пробел, перенос строки.

## 6. Тихие часы и дайджесты

- `QuietHoursEnabled`: окно времени задается парой `QuietHoursStart` / `QuietHoursEnd` (формат `HH:mm`).
- Если старт позже конца (например, `23:00` -> `07:00`), это считается ночным интервалом через полночь.
- `DailyDigestTime` и `WeeklyDigestTime` должны попадать в диапазон `00:00..23:59`.
- `WeeklyDigestDay`: один из дней недели (`Monday`, `Tuesday`, ...).

## 7. Диагностика

- Логи: `%AppData%\AlarmProgram\logs`.
- Кнопка **Открыть логи** в UI открывает этот каталог.
- Для безопасной диагностики используйте **Копировать диагностику** (без секретов).

## 8. Рекомендуемый порядок настройки

1. Включить Telegram и выполнить **Тестовую отправку**.
2. Включить нужные типы событий (startup/shutdown/restart и т.д.).
3. При необходимости добавить watchdog (процессы/службы/хосты).
4. Настроить quiet hours и лимиты шумных алертов.
5. Проверить лог и журнал алертов после тестового сценария.
