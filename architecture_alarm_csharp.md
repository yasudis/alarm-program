# Alarm Program - архитектура реализации на C#

Этот документ описывает, **как реализовывать приложение архитектурно** на C# так, чтобы проект было удобно развивать.

---

## 1) Архитектурный подход

Для проекта подойдет pragmatic clean architecture:

- UI слой (WPF) - только отображение и команды пользователя;
- Application слой - сценарии приложения (use-cases);
- Domain слой - сущности и правила;
- Infrastructure слой - работа с Event Log, Telegram API, хранением настроек.

Ключевая цель: бизнес-логика не должна зависеть от WPF и внешних API.

---

## 2) Предлагаемая структура solution

```text
AlarmProgram.sln
src/
  AlarmProgram.UI/                 # WPF
  AlarmProgram.Application/        # use-cases, orchestrators
  AlarmProgram.Domain/             # domain models, enums, rules
  AlarmProgram.Infrastructure/     # EventLog, Telegram, storage, logging adapters
tests/
  AlarmProgram.Tests.Unit/
  AlarmProgram.Tests.Integration/
```

---

## 3) Главные модули

## 3.1 Domain

- `MachineEventType` (Startup, Shutdown, Restart, UnexpectedShutdown, etc.)
- `MachineEvent` (тип, время, источник, host info)
- `AlertMessage` (готовый текст для отправки)
- `UserSettings` (Telegram token, chat id, flags событий)

## 3.2 Application

- `IEventCollector` - получает системные события.
- `IEventClassifier` - классифицирует сырые события в доменные.
- `IAlertFormatter` - форматирует сообщение (включая `{Uptime}`).
- `IHostUptimeProvider` - время работы хоста для heartbeat и снимка статуса.
- `INotificationChannel` - общий контракт отправки.
- `AlertOrchestrator` - главный pipeline:
  1) получить событие;
  2) проверить правила;
  3) собрать сообщение;
  4) отправить;
  5) залогировать результат.

## 3.3 Infrastructure

- `WindowsEventLogReader` (чтение Event Log).
- `TelegramNotificationChannel` (HTTP вызовы Telegram Bot API).
- `DiscordWebhookChannel` (опционально).
- `HttpWebhookNotificationChannel` (произвольный HTTPS JSON POST).
- `SmtpNotificationChannel` (опциональный Email через SMTP).
- `SettingsStore` (JSON + защита секретов, включая SMTP-пароль).
- `AppLogger` (Serilog/EventLog).

## 3.4 UI (WPF)

- ViewModels:
  - `MainViewModel`
  - `SettingsViewModel`
  - `LogsViewModel` (опционально)
- Команды:
  - `SaveSettingsCommand`
  - `SendTestAlertCommand`
  - `StartMonitoringCommand`

---

## 4) Поток данных (runtime flow)

1. Приложение стартует -> грузит настройки.
2. Мониторинг подписывается на источник системных событий.
3. При событии вызывается классификатор.
4. Если событие разрешено настройками:
   - формируется текст;
   - отправляется в Telegram/Discord.
5. Успех или ошибка фиксируется в лог.

---

## 5) Стратегия конфигурации и секретов

- Общие настройки: `appsettings.json`.
- Пользовательские настройки: отдельный локальный файл, например:
  - `%AppData%/AlarmProgram/settings.json`
- Секреты (Bot Token) хранить не в открытом виде:
  - использовать DPAPI (`ProtectedData`) на Windows.

---

## 6) Обработка ошибок и устойчивость

- Ошибки сети не валят приложение, а логируются.
- Для отправки использовать retry с ограничением попыток (2-3 раза).
- Для временных ошибок - экспоненциальная пауза.
- Для дублей событий - simple de-dup cache (например, hash события + TTL).

---

## 7) Логирование и наблюдаемость

Минимально:

- лог в файл (rolling per day);
- предупреждения/ошибки в Event Log;
- correlation id для цепочки "event -> send".

Логи должны отвечать на вопросы:

- какое событие обнаружено;
- какое сообщение сформировано;
- куда отправлено;
- какая ошибка случилась, если отправка не прошла.

---

## 8) Тестовая стратегия

Unit:

- классификатор событий;
- форматтер сообщений;
- фильтрация по пользовательским настройкам.

Integration:

- `TelegramNotificationChannel` с test bot;
- чтение тестовых записей Event Log (или подмена через mock provider).

E2E (ручные):

- чистый запуск;
- перезагрузка ПК;
- проверка, что алерт приходит корректно.

---

## 9) Скелет DI-конфигурации (концептуально)

```csharp
services.AddSingleton<ISettingsStore, SettingsStore>();
services.AddSingleton<IEventCollector, WindowsEventLogReader>();
services.AddSingleton<IEventClassifier, EventClassifier>();
services.AddSingleton<IAlertFormatter, AlertFormatter>();
services.AddSingleton<INotificationChannel, TelegramNotificationChannel>();
services.AddSingleton<AlertOrchestrator>();
```

Если нужен multi-channel, можно регистрировать несколько `INotificationChannel` и отправлять во все выбранные пользователем.

---

## 10) Архитектурные решения, которые стоит принять заранее

1. **Worker + WPF вместе или раздельно?**
   - Для MVP проще единое WPF-приложение с фоновым мониторингом.
   - Для Pro-версии можно вынести фон в отдельный Worker/Service.

2. **Long-running процесс как Windows Service?**
   - Не обязательно на первом этапе.
   - Нужен, если важна работа без входа пользователя и повышенная надежность.

3. **Локальный-only или cloud-ready?**
   - Начать с local-first.
   - Контракты интерфейсов делать так, чтобы позже добавить сервер без переписывания домена.

---

## 11) Полезные статьи и материалы

## Habr

- [.NET Core Workers как службы Windows](https://habr.com/ru/companies/microsoft/articles/446512/)
- [Служба Windows на C# в .NET 9 (Telegram.Bot)](https://habr.com/ru/articles/863770/)
- [Разработка бота для Telegram на платформе .NET](https://habr.com/ru/articles/855236/)
- [Быстрый старт с WPF. Привязка, INotifyPropertyChanged и MVVM](https://habr.com/ru/articles/427325/)
- [Джентльменский набор для создания WPF-приложений](https://habr.com/ru/articles/647259/)

## Официальная документация / практические гайды

- [Create Windows Service using BackgroundService (.NET)](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)
- [Telegram.Bot Book](https://telegrambots.github.io/book/)
- [NuGet Telegram.Bot](https://www.nuget.org/packages/Telegram.Bot)

---

## 12) Почему эта архитектура удачна для будущей разработки

- легко добавлять новые каналы уведомлений (email, Slack, webhook);
- можно перейти на Windows Service без слома ядра логики;
- удобно покрывать тестами, потому что UI отделен от домена;
- проще поддерживать и расширять продукт в долгую.
