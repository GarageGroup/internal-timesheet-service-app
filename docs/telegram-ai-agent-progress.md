# Telegram AI Agent: журнал прогресса

Последнее обновление: 28.09.2026.

Связанный документ: [план реализации](telegram-ai-agent-implementation-plan.md).

Объяснение решения для руководителя: [management overview](telegram-ai-agent-management-overview.md).

Инструкция переноса в production: [production rollout runbook](telegram-ai-agent-production-rollout.md).

Изменения тестовой инфраструктуры фиксируются отдельно в [журнале изменений Azure](telegram-ai-agent-azure-change-log.md).

Разрешённые будущему агенту операции и выявленные ограничения собраны в [каталоге операций](telegram-ai-agent-operation-catalog.md).

## Текущее состояние

Проект находится на **этапе 3 — Semantic Kernel и read-only инструменты**.

Resolver пользователя, read-only инструменты, Foundry kernel, ограниченная история диалога, синхронный message endpoint и вызов из Telegram-бота реализованы и покрыты unit-тестами. API и бот развёрнуты в test; готовы таблица истории, deployment `gpt-5-mini`, RBAC, App Service settings и APIM message operation. Read-only end-to-end цепочка успешно проверена реальным Telegram-сообщением.

Первая проверка выявила перехват обычного текста диагностической profile-командой. Причина исправлена:
`AgentProfileCommand` больше не участвует в fallback parser-цепочке и остаётся доступен только через
`/profile`. Исправленный бот повторно развёрнут ZIP-пакетом; 8 тестов прошли. Требуется повторить
проверку обычным сообщением.

Повторный Telegram update был принят webhook, но не обработан Durable Entity после быстрого ZIP
recycle; agent API не вызывался. Function App перезапущен без очистки очередей. Следующая проверка
должна выполняться новым сообщением после восстановления Durable worker.

После штатного перезапуска Function App повторная проверка завершилась успешно: обычный вопрос о
доступных проектах прошёл через Telegram-бот, APIM, `POST /internal/agent/messages`, Semantic Kernel,
Foundry deployment `gpt-5-mini` и read-only project tool; пользователь получил корректный ответ.
Таким образом, read-only end-to-end вертикальный срез подтверждён в test.

При ручной проверке всех пяти read-only tools выявлена одна ошибка: `get_timesheets` объявлял параметры
Kernel-функции как `DateOnly`, а Semantic Kernel передавал даты модели как JSON-строки. Binder завершался
ошибкой преобразования `System.String` в `System.DateOnly` до вызова business-функции. Остальные tools
ручную проверку прошли без выявленных ошибок.

Граница `get_timesheets` исправлена: tool принимает строки строго в формате `yyyy-MM-dd`, явно и
invariant-преобразует их в `DateOnly`, после чего вызывает неизменённый типизированный контракт.
Некорректная дата возвращает безопасный `InvalidDateFormat`, а не исключение. Добавлены unit-тесты
валидного и некорректного формата и regression-тест вызова через настоящий `KernelArguments` binder.
Release-сборка прошла без ошибок и предупреждений; все тесты решения прошли. Изменение ещё не
развёртывалось в test.

Исправление `14ecd63 Fix timesheet tool date binding` развёрнуто пользователем в test и проверено
повторным Telegram-запросом «Какие списания были сегодня?». `get_timesheets` успешно принял даты,
выполнил типизированную функцию и вернул пользователю результат без ошибки преобразования. Ручная
проверка всех пяти доступных read-only tools завершена успешно.

```text
[x] Этап 0. Baseline и первичный аудит
[~] Этап 1. Identity и binding без LLM
[~] Этап 2. Общие business factories и проверка прав
[~] Этап 3. Semantic Kernel и read-only инструменты
[ ] Этап 4. Подтверждения и операции записи
[ ] Этап 5. Пилот и эксплуатация
```

Обозначения: `[x]` — завершено, `[~]` — выполняется, `[ ]` — не начато.

## Что уже сделано

### 25.09.2026 — архитектурный план

- Изучены API, Telegram-бот, mini app, SupportBot и консольный прототип Semantic Kernel.
- Выбрана архитектура: Semantic Kernel располагается в API, бот служит транспортом, бизнес-обработчики вызываются внутри процесса без HTTP-запросов API к самому себе.
- Определена схема авторизации: бот обращается к API с app-only токеном, а конечный пользователь определяется по серверной Telegram/CRM-привязке.
- Зафиксированы правила подтверждения изменяющих операций, идемпотентности, хранения заданий и постепенного включения функций.
- Создан файл `docs/telegram-ai-agent-implementation-plan.md`.

### 25.09.2026 — baseline

- Проверена версия SDK: .NET SDK 10.0.401.
- Выполнена исходная сборка и запуск всех существующих тестов.
- До добавления нового модуля все существующие тесты проходили.
- Подтверждено, что рабочее дерево до начала реализации содержало только новый каталог `docs/` с архитектурным документом.
- В репозитории и проверенных родительских каталогах не обнаружен `AGENTS.md`.

### 25.09.2026 — модуль Agent.Identity

В solution добавлены три проекта:

- `src/service/Agent.Identity/Contract`;
- `src/service/Agent.Identity/Core`;
- `src/service/Agent.Identity/Test`.

Реализованы:

- `AgentUserIdentity` — доверенные входные идентификаторы бота, Telegram-пользователя и чата;
- `AgentUserContext` — разрешённый контекст с `BotId`, `TelegramUserId`, `TelegramChatId`, ID привязки, CRM System User ID и Entra Object ID;
- `IAgentUserContextResolver` — публичный контракт resolver;
- `AgentUserContextResolver` — реализация поиска и проверки привязки;
- `AgentUserContextResolverDependency` — регистрация resolver в принятом в проекте стиле `PrimeFuncPack.Dependency`;
- `DbAgentUserBinding` — SQL-проекция привязки Telegram-пользователя и CRM-пользователя.

Resolver выполняет следующие проверки:

1. Идентификаторы бота и Telegram-пользователя должны быть положительными, chat ID не должен быть нулевым.
2. В первой версии разрешены только private chat, где `TelegramChatId == TelegramUserId`.
3. Поиск выполняется по активной CRM-записи и паре `(BotId, TelegramUserId)`.
4. Запрашивается не более двух записей, чтобы обнаружить неоднозначную привязку.
5. Отдельно обрабатываются отсутствующая привязка, несколько привязок и выполненный sign-out.
6. Проверяется, что CRM-пользователь не заблокирован.
7. Проверяется наличие Entra Object ID.
8. Проверяется корректность обязательных ID записи привязки и CRM-пользователя.
9. Ошибки SQL переводятся в типизированную ошибку `Unknown` с сохранением исходной причины.

Для соединения с `systemuser` используется `LEFT JOIN`. Благодаря этому существующая запись привязки с отсутствующим или некорректным CRM-пользователем не маскируется под обычное состояние `UserNotLinked`.

Добавлено 14 unit-тестов, покрывающих:

- неправильные входные идентификаторы;
- групповой чат;
- сформированный SQL-запрос;
- отсутствие привязки;
- инфраструктурную ошибку SQL;
- неоднозначную привязку;
- sign-out;
- заблокированного CRM-пользователя;
- отсутствие Entra Object ID;
- пустые обязательные ID;
- успешное создание пользовательского контекста.

Проверки после реализации:

- `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно;
- всего прошло 370 тестов, включая 14 новых;
- `git diff --check` — ошибок форматирования diff не обнаружено.

### 25.09.2026 — усиление User.SignIn

- Проверка Telegram Mini App `initData` вынесена в отдельный `TelegramWebAppDataValidator` с внедряемым `TimeProvider`.
- Параметры разбираются независимо от порядка; пустые, повторяющиеся и некорректно percent-encoded параметры отклоняются.
- HMAC-SHA256 вычисляется по алгоритму Telegram, hash декодируется как 32 байта и сравнивается через `CryptographicOperations.FixedTimeEquals`.
- `user.id` читается JSON-десериализацией, а не регулярным выражением.
- `auth_date` проверяется относительно серверного UTC-времени. По умолчанию TTL равен 5 минутам, clock skew — 30 секундам.
- TTL и clock skew добавлены в `appsettings.json` как `TelegramBot:WebAppDataMaxAgeMinutes` и `TelegramBot:WebAppDataClockSkewSeconds`.
- Перед upsert выполняется поиск активных привязок по `(BotId, TelegramUserId)`.
- Повторный вход того же CRM-пользователя остаётся идемпотентным.
- Привязка Telegram-пользователя к другому CRM-пользователю и несколько активных совпадений возвращают HTTP-конфликт `TelegramUserAlreadyLinked`.
- Предварительная проверка конфликта уменьшает риск неправильной привязки, но не заменяет уникальное ограничение в Dataverse: два одновременно выполняющихся первых входа теоретически всё ещё могут пройти проверку до записи.
- Добавлены тесты известного подписанного Telegram-вектора, срока действия, future timestamp, порядка параметров, подписи, JSON, дубликатов, Dataverse-запроса, повторного входа и конфликтов.

Проверки после реализации:

- `dotnet test src/endpoint/User.SignIn/Test/Test.csproj --no-restore` — успешно, 46 тестов;
- `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, всего 387 тестов;
- интеграционный вызов Telegram и реальная CRM не проверялись.

### 25.09.2026 — аудит Azure и закрытый контур agent API

- Подтверждено, что `func-internal-gtimesheet-test` размещает Telegram-бота, а API работает в `app-garage-timesheet-service-test`.
- В общем APIM найден API `garage-timesheet-api` с публичным префиксом `timesheet` и backend на App Service API.
- В APIM не обнаружена политика `validate-jwt`, Easy Auth у App Service выключен. Текущий `UseJwtReader()` нельзя считать достаточным доказательством криптографической проверки токена.
- Для маршрутов `/internal/agent/*` добавлен отдельный JWT Bearer handler. Он проверяет подпись Entra, issuer, audience, lifetime и signing key.
- После проверки токена дополнительно требуются app role и явное серверное сопоставление `client id -> BotId`. `BotId` не должен поступать из тела запроса.
- Контур выключен настройкой `Agent:Enabled = false`. В этом состоянии любой `/internal/agent/*` возвращает 404.
- При включении без tenant, audience, required role или хотя бы одного client mapping приложение завершится при старте с ошибкой конфигурации.
- Из `Profile.Get` выделена общая фабрика `UseProfileGetFunc`, чтобы будущий агент и существующий endpoint использовали одну бизнес-реализацию.
- Выяснено, что API и бот сейчас используют одну user-assigned managed identity. Для agent API нужна отдельная идентичность бота, иначе их невозможно различить по `azp`/`appid`.

Проверки:

- `dotnet build Internal.Timesheet.Service.slnx --no-restore` — успешно, без предупреждений;
- `dotnet test Internal.Timesheet.Service.slnx --no-build --no-restore` — успешно, 387 тестов;
- локальный запрос `POST /internal/agent/profile` при выключенном feature flag — HTTP 404;
- реальные Entra-токены ещё не проверялись, так как отдельная идентичность и app role пока не созданы.

### 25.09.2026 — первый внутренний read-only endpoint

- Добавлен endpoint `POST /internal/agent/profile`.
- В JSON принимаются только `TelegramUserId` и `TelegramChatId`; `BotId` поступает из доверенного claim `timesheet_bot_id`, сформированного после app-only аутентификации.
- `AgentUserContextResolver` подключён к приложению и разрешает Telegram-пользователя в серверный контекст.
- Профиль запрашивается через общую реализацию `IProfileGetFunc` с разрешённым `EntraObjectId`; бизнес-логика `Profile.Get` не дублируется.
- Ошибки разделены на invalid identity, отсутствующую привязку, недоступную привязку, отсутствующий профиль и неизвестную инфраструктурную ошибку.
- Legacy `UseJwtReader()` теперь применяется только к старым маршрутам. Маршруты `/internal/agent/*` проходят исключительно через новый Entra JWT Bearer контур.
- Добавлено 12 unit-тестов для всех вариантов resolver, передачи доверенного Entra ID, ошибок профиля и успешного результата.

Проверки:

- `dotnet build Internal.Timesheet.Service.slnx --no-restore` — успешно, без предупреждений;
- `dotnet test Internal.Timesheet.Service.slnx --no-build --no-restore` — успешно, 399 тестов;
- зарегистрированный `POST /internal/agent/profile` при `Agent:Enabled = false` возвращает HTTP 404;
- вызов с настоящим app-only токеном будет проверен после создания Azure identity и app role.

### 25.09.2026 — подготовка Entra для agent API

- У Function App `func-internal-gtimesheet-test` включена system-assigned managed identity.
- Существующая user-assigned identity сохранена; тип identity теперь `SystemAssigned, UserAssigned`.
- Создана App Registration `api-timesheet-agent-test` и соответствующий service principal.
- Для API настроен identifier URI и application role `Timesheet.Agent.Invoke` с типом участника `Application`.
- Роль назначена system-assigned identity Telegram-бота.
- В `app-garage-timesheet-service-test` записаны tenant, audience, required role и сопоставление identity бота с Telegram `BotId`.
- `Agent__Enabled` оставлен равным `false`; существующая версия приложения не открывает agent endpoint.
- Allowlist клиентов переведён с dictionary-ключа, содержащего GUID, на массив объектов `ClientId`/`BotId`, потому что App Service отклонил имя настройки с GUID-сегментом.
- Секреты и Telegram token в журнал не записывались.

### 25.09.2026 — Managed Identity клиент в Telegram-боте

- В `internal-timesheet-bot-app` добавлен typed HTTP client для `POST /internal/agent/profile`.
- Access token запрашивается через `ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)`, поэтому общая user-assigned identity не может быть случайно выбрана для agent-вызова.
- Scope формируется как `<AgentApi:Audience>/.default`, токен передаётся в стандартном заголовке Bearer.
- Добавлена диагностическая команда `/profile`; она передаёт Telegram chat ID как user/chat ID и выводит только имя и язык найденного профиля.
- Ошибка `404` предлагает пользователю выполнить вход через Mini App, остальные ошибки логируются без токена и возвращают нейтральное сообщение.
- В Function App записаны `AgentApi__BaseAddress` и `AgentApi__Audience`; приложение осталось в состоянии `Running`.
- Добавлен тестовый проект с тремя тестами scope/Bearer, HTTP-контракта и ошибочного статуса.
- Коммит бота: `a376719 Add managed identity agent profile client`.

Проверки:

- `dotnet build Internal.Timesheet.Bot.sln --no-restore` — успешно, без предупреждений;
- `dotnet test Internal.Timesheet.Bot.sln --no-restore` — успешно, 3 теста;
- версии API и бота ещё не развёрнуты, `Agent__Enabled` остаётся выключенным.

## Принятые решения

| Решение | Причина |
|---|---|
| Сначала реализовать identity без LLM | Авторизацию и выбор пользователя нельзя доверять модели |
| Хранить CRM ID и Entra Object ID раздельно | В существующих контрактах используются оба вида ID, и они не взаимозаменяемы |
| Не передавать ID пользователя как аргументы SK-функций | Пользовательский контекст должен поступать только из доверенного серверного resolver |
| Пока поддерживать только private chat | В private chat Telegram user ID совпадает с chat ID; для групп нужен отдельный безопасный сценарий |
| Загружать максимум две привязки | Этого достаточно, чтобы отличить единственную запись от неоднозначной конфигурации |
| Не подключать resolver к HTTP до усиления sign-in | Нельзя основывать новый доверенный контур на недостаточно проверенном `initData` |
| Не добавлять Semantic Kernel на этапе 1 | Сначала должна быть проверена детерминированная цепочка идентичности |
| Ограничить срок `initData` пятью минутами и 30 секундами clock skew | Снижает окно повторного использования подписанных данных; значения доступны в конфигурации |
| Конфликтующую привязку не перезаписывать автоматически | Перепривязка меняет владельца агентского контекста и требует отдельного явного flow |
| Защищать `/internal/agent/*` отдельным JWT Bearer handler | Существующий контур чтения claims и текущая конфигурация APIM не дают достаточной гарантии проверки токена |
| Держать agent API выключенным до готовности Entra | Неполная конфигурация безопасности не должна приводить к доступному endpoint |
| Определять `BotId` по доверенному client id | Вызывающая сторона не должна выбирать бот через пользовательский payload |
| Повторять структуру ближайшего существующего модуля | Единая структура `Contract`/`Endpoint`/`Func`/`Test.*` упрощает сопровождение и review |
| Использовать `AsyncPipeline`/`Pipeline` для orchestration | Новый код должен следовать принятой функциональной композиции и типизированной обработке `Result` |
| Сохранить `Agent.Profile.Get` до отдельного указания | Работающий endpoint нужен для демонстрации защищённого вертикального среза руководству |
| Не предоставлять агенту всё API | В allowlist входят только операции, необходимые для работы со списаниями: периоды, проекты, теги и CRUD списаний; профиль, подписки, уведомления и account flow не являются AI tools |

### 26.09.2026 — успешная сквозная проверка `/profile`

- Пользователь авторизовался в тестовой Mini App, после чего отправил `/profile` тестовому Telegram-боту.
- Бот успешно получил app-only токен через system-assigned Managed Identity и вызвал отдельный agent API через APIM.
- API проверил Entra-токен, application role и allowlist клиента, определил доверенный `BotId`, разрешил Telegram-привязку и вернул профиль соответствующего CRM-пользователя.
- Бот показал пользователю данные его аккаунта. Повторный интерактивный вход для вызова `/profile` не потребовался.
- Отдельно проверены отрицательные сценарии на публичном agent endpoint: запрос без Bearer-токена возвращает HTTP 401; запрос с некорректным Bearer-токеном также возвращает HTTP 401.
- Таким образом, подтверждён вертикальный срез `Telegram -> bot -> Managed Identity -> APIM -> API -> user binding -> CRM profile`.
- Изменения инфраструктуры при этой проверке не выполнялись.

Не проверены отдельными интеграционными тестами: чужой валидный client ID, валидный токен без нужной роли, непривязанный Telegram-пользователь, групповой чат и поведение после sign-out.

### 28.09.2026 — выравнивание нового кода со стилем проекта

- `Agent.Profile.Get` приведён к структуре существующих endpoint: реализация перенесена в `Endpoint/Func`, тесты — в `Test/Test.Func`, сценарии расположены в `Func.Invoke.cs` и `Test.Invoke.cs`.
- `Agent.Identity` приведён к аналогичной структуре `Core/Resolver` и `Test/Test.Resolver`.
- Ручная async-orchestration `AgentProfileGetFunc` заменена на `AsyncPipeline` с типизированным преобразованием ошибок resolver и `Profile.Get`.
- Валидация, SQL-вызов и преобразование результата `AgentUserContextResolver` объединены в `AsyncPipeline`.
- В проект endpoint добавлена прямая зависимость `EarlyFuncPack.Core.AsyncPipeline` той же версии, что используется существующими endpoint.
- Основная инструкция дополнена обязательными правилами использования `AsyncPipeline`/`Pipeline`, повторения структуры ближайшего модуля и сохранения диагностического `Agent.Profile.Get` до отдельного решения владельца проекта.
- `Agent.Profile.Get` не удалён и продолжает оставаться демонстрационным вертикальным срезом.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 399 тестов прошли.

### 28.09.2026 — инвентаризация операций API

- Проанализированы все существующие endpoint и их внутренние business-функции.
- Операции разделены на read candidates, write blocked, system only и diagnostic.
- Для первой read-only итерации предложены профиль, периоды, последние/поисковые проекты, списания и теги.
- Зафиксировано требование отдельных agent adapters: модель не должна получать `SystemUserId`, `CallerObjectId`, `BotId`, Telegram ID или Entra Object ID как аргументы.
- Выявлен блокирующий риск `Timesheet.Update`: update input не устанавливает `CallerObjectId`, поэтому операцию нельзя подключать до аудита impersonation и ownership.
- Для `Project.GetSet` требуется проверить видимость Incident/Opportunity/Lead; для `Tag.GetSet` — происхождение Project ID; для всех списков — серверные лимиты.
- Код приложения, Azure и deployment в этом инкременте не изменялись.
- Создан `docs/telegram-ai-agent-operation-catalog.md`.

### 28.09.2026 — первый read-only agent adapter

- Добавлен модуль `src/service/Agent` со стандартной структурой `Contract/Core/Test`.
- Реализован adapter получения списаний за период поверх существующей `ITimesheetSetGetFunc`.
- Вход adapter содержит только даты; Entra Object ID подставляется из доверенного `AgentUserContext` и недоступен модели.
- Добавлена серверная проверка порядка дат и максимального диапазона; значение по умолчанию — 31 день.
- Результат преобразуется в отдельный agent DTO, не связанный с HTTP binding metadata.
- `TimesheetSetGetDependency` теперь отдельно предоставляет общую `ITimesheetSetGetFunc`; существующий HTTP endpoint продолжает использовать ту же реализацию.
- Добавлены 5 тестов: неправильный порядок дат, слишком длинный период, доверенный Entra ID, преобразование ошибки и успешное преобразование результата.
- Вопрос по отсутствующему `CallerObjectId` в `Timesheet.Update` помечен как обязательный для уточнения у руководства перед write-этапом.
- Adapter пока не подключён к Application, HTTP или Semantic Kernel; Azure не изменялся.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 404 теста прошли.

### 28.09.2026 — read-only adapter поиска проектов

- Добавлен adapter `Project.SearchSet` в существующий модуль `src/service/Agent`.
- Модель сможет задавать только поисковый текст и `top`; `CallerObjectId` подставляется из `AgentUserContext.EntraObjectId`.
- Уточнено различие идентификаторов: существующее свойство `ProjectSetSearchIn.SystemUserId` получено из claim `oid` и фактически содержит Entra Object ID, а не CRM primary key.
- Введены серверные ограничения: непустой текст до 100 символов, `top` от 1 до 20, значение по умолчанию 10.
- Существующая `IProjectSetSearchFunc` выделена из dependency composition и совместно используется HTTP endpoint и agent adapter.
- Ошибка доступа преобразуется в отдельный `Forbidden`, остальные ошибки — в `Unknown`.
- Adapter покрыт тестами в принятой структуре `Test/Test.Project.SearchSet`.
- Настройки пока не привязаны к `appsettings.json`, потому что agent-модуль ещё не зарегистрирован в Application.
- Application, HTTP, Semantic Kernel и Azure не изменялись.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 413 тестов прошли.

### 28.09.2026 — read-only adapter последних проектов

- Добавлен adapter `Project.GetLastSet` в модуль `src/service/Agent`.
- Модель передаёт только необязательный `top`; Entra Object ID подставляется из доверенного `AgentUserContext`.
- Сервер применяет `top = 10` по умолчанию и допускает значения от 1 до 20.
- Результат преобразуется в общий `AgentProjectItem` с сохранением комментария проекта.
- Существующая `ILastProjectSetGetFunc` выделена из dependency composition и совместно используется HTTP endpoint и adapter.
- Для одноимённых типов `ProjectItem` двух существующих endpoint в тестовом проекте заданы явные assembly aliases; публичные контракты не изменялись.
- Добавлено 6 тестовых сценариев с учётом разворачивания theory cases.
- Application, HTTP, Semantic Kernel и Azure не изменялись.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 419 тестов прошли.

### 28.09.2026 — read-only adapter периодов

- Добавлен adapter `Period.GetSet` в модуль `src/service/Agent`.
- Tool не принимает пользовательские параметры, но его контракт требует уже разрешённый `AgentUserContext`, чтобы он не использовался вне авторизованного agent flow.
- Результат преобразуется в `AgentPeriodItem` с названием, началом и концом периода.
- Существующая `IPeriodSetGetFunc` выделена из dependency composition и совместно используется HTTP endpoint и adapter.
- Инфраструктурные ошибки преобразуются в безопасный код `Unknown`.
- Добавлены 3 unit-теста вызова зависимости, ошибки и успешного преобразования.
- Application, HTTP, Semantic Kernel и Azure не изменялись.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 422 теста прошли.

### 28.09.2026 — read-only adapter тегов

- Добавлен adapter `Tag.GetSet` в модуль `src/service/Agent`.
- Модель передаёт только Project ID; Entra Object ID подставляется из доверенного `AgentUserContext`.
- Пустой Project ID отклоняется до вызова существующей функции.
- Результат ограничен первыми 20 тегами через `AgentTagSetGetOption`; значение будет связано с конфигурацией при регистрации agent-модуля.
- Существующая `ITagSetGetFunc` выделена из dependency composition и совместно используется HTTP endpoint и adapter.
- Зафиксировано ограничение: orchestration должен брать Project ID из разрешённого серверного результата либо повторно проверять проект.
- Добавлены 4 unit-теста: пустой ID, доверенный Entra ID, ошибка зависимости и ограничение успешного результата.
- Application, HTTP, Semantic Kernel и Azure не изменялись.

Проверка: `dotnet test Internal.Timesheet.Service.slnx --no-restore` — успешно, все 426 тестов прошли.

### 28.09.2026 — уточнение границ инструментов агента

- Принято решение не предоставлять модели всё существующее API.
- Allowlist Semantic Kernel ограничен функционально важными для списания времени областями: периоды, поиск/выбор проектов, теги и чтение/создание/изменение/удаление списаний.
- `Profile.Get`, `Profile.Update`, подписки, уведомления и операции входа/выхода не будут регистрироваться как AI tools.
- Ранее подготовленный adapter профиля удалён как ненужный; новых agent adapters для несвязанных областей создавать не следует.
- Диагностический `Agent.Profile.Get` сохраняется отдельным работающим endpoint для демонстрации авторизации руководству и не считается инструментом агента.

### 28.09.2026 — регистрация read-only adapters в Application

- В Application добавлена единая dependency composition для пяти разрешённых read-only adapters: списаний, поиска проектов, последних проектов, периодов и тегов.
- Каждый adapter использует ту же внутреннюю business-функцию, что и существующий HTTP endpoint; HTTP-вызов API к самому себе не используется.
- Серверные ограничения перенесены в общую секцию `Agent:Tools` файла `appsettings.json`: диапазон дат, `top`, длина поиска и число тегов.
- Добавлена прямая зависимость Application от контрактов и Core agent-модуля.
- Semantic Kernel, новый HTTP endpoint, Telegram-бот и Azure в этом инкременте не изменялись.

### 28.09.2026 — Semantic Kernel read-only plugin

- В agent-модуль добавлен native plugin Semantic Kernel `TimesheetRead`.
- Plugin публикует модели только пять функций утверждённого allowlist: списания, поиск проектов, последние проекты, периоды и теги.
- `AgentUserContext` передаётся в constructor plugin и не отображается в аргументах функций для модели.
- Ответ tool стандартизирован через `AgentReadToolResult`: данные возвращаются только при успехе, при ошибке модели доступен безопасный типизированный код без исходного исключения.
- Добавлен тест метаданных Kernel, который фиксирует точный набор из пяти функций и защищает от случайного появления профиля или других API-операций.
- Добавлены тесты передачи доверенного контекста и пользовательских аргументов во все пять adapters, а также безопасного преобразования ошибки.
- Подключение Foundry, создание рабочего Kernel, message endpoint, Telegram-бот и Azure не изменялись.

Проверка agent-модуля: 34 теста прошли.

### 28.09.2026 — фабрика Kernel для Azure AI Foundry

- Добавлена `AgentKernelFactory`, создающая отдельный Semantic Kernel для доверенного `AgentUserContext`.
- Первоначальный вариант с API key старого Foundry заменён на новый Foundry project endpoint и Entra ID.
- Используются `ProjectEndpoint`, `ModelId` и `TokenScope`; фабрика сама добавляет путь `/openai/v1/`.
- Стандартный `TokenCredential` регистрируется один раз на уровне host через общий инфраструктурный пакет; локально он использует активную сессию `az login`, а после развертывания в Azure — Managed Identity.
- `AgentKernelFactory` не создаёт `DefaultAzureCredential` напрямую: credential и `AgentFoundryOption` передаются в неё через общий для проекта `Pipeline/Dependency` composition root.
- В каждый созданный Kernel регистрируется только native plugin `TimesheetRead`; импорт Swagger и автоматическое предоставление всего API не используются.
- В общий `appsettings.json` добавлен пустой шаблон секции `Agent:Foundry`; реальные секреты в репозиторий не добавлялись.
- Конфигурация проверяет абсолютный HTTPS endpoint формата `*.services.ai.azure.com/api/projects/*`, непустые model ID и token scope.
- Тесты проверяют наличие chat completion service, точный allowlist функций и фактический вызов plugin с контекстом, переданным фабрике.
- Реальные запросы в Foundry, message endpoint, Telegram-бот и Azure не изменялись.

Проверка agent-модуля: 36 тестов прошли.

### 28.09.2026 — read-only message orchestration

- Добавлен `IAgentMessageFunc`, выполняющий один изолированный ход диалога через Semantic Kernel.
- В Kernel включён автоматический выбор только функций зарегистрированного `TimesheetRead`; write-инструменты по-прежнему отсутствуют.
- Системная инструкция явно запрещает заявлять о создании, изменении или удалении данных, требует уточнения при неоднозначном проекте и считает CRM-текст недоверенными данными.
- Текущая дата вычисляется на каждый запрос через внутренний `IDateProvider` в настраиваемом часовом поясе; по умолчанию используется `Europe/Moscow`. Реализация повторяет принятый в других проектах команды DateProvider-подход и позволяет фиксировать дату в тестах.
- Добавлены ограничение длины сообщения и безопасные ошибки без передачи модели/клиенту исходного исключения.
- Kernel factory выделена в интерфейс, поэтому orchestration тестируется без реального Foundry.
- Сервис собран через общий `Pipeline/Dependency`; HTTP endpoint и постоянная история пока не добавлялись.

Проверка agent-модуля: 38 тестов прошли.

### 28.09.2026 — bounded conversation history

- `AgentMessageFunc` принимает ранее сохранённые сообщения пользователя и агента и восстанавливает их в `ChatHistory` перед текущим сообщением.
- История считается внутренними серверными данными: будущий Telegram endpoint не должен принимать её от клиента, а должен загружать из принадлежащего пользователю хранилища.
- Количество сообщений истории ограничено `Agent:Message:MaxHistoryMessageCount`, по умолчанию 20; текст каждого элемента проходит ту же проверку размера, что и текущее сообщение.
- Поддерживаются только роли `User` и `Assistant`; системные инструкции и tool-сообщения из хранилища не принимаются.
- Добавлены тесты порядка/ролей восстановленной истории и отклонения истории сверх лимита.

Проверка: сборка всего решения без ошибок и предупреждений; 39 тестов agent-модуля прошли.

### 28.09.2026 — conversation gateway и optimistic concurrency

- Добавлен `IAgentConversationStore`: история читается и дополняется только вместе с доверенным `AgentUserContext`.
- Добавлен `AgentConversationMessageFunc`, который загружает серверную историю, выполняет read-only ход агента и сохраняет точную пару сообщений пользователя и ассистента.
- `AppendAsync` получает версию прочитанного диалога; storage provider обязан выполнять optimistic concurrency check и возвращать `Conflict`, если диалог уже изменился.
- При конфликте модель автоматически повторно не вызывается: вызывающий job должен сериализовать сообщения диалога либо безопасно решить, нужен ли новый ход.
- Добавлены тесты передачи истории, нормализации сохраняемого пользовательского текста, сохранения ответа, проброса доверенного контекста и конфликта версии.
- Реализация Azure storage, очередь, worker и HTTP endpoint пока не добавлялись; Azure-конфигурация не изменялась.

Проверка agent-модуля: 42 теста прошли.

### 28.09.2026 — Azure Table provider для истории диалога

- Добавлен отдельный infrastructure-модуль `Agent.Storage.Table` в принятой структуре `Contract` / `Api` / `Test`; `Api` реализует контракт через `Azure.Data.Tables` 12.13.0.
- Одна ограниченная история хранится в одной Table entity; ключ строится из доверенных `BotId`, `TelegramUserId` и `TelegramChatId`, а не из входного DTO Telegram.
- Запись выполняется с ETag прочитанной версии. Создание существующей entity, несовпадение ETag и исчезновение entity отображаются в `ConversationConflict`.
- Gateway теперь сохраняет всю новую ограниченную историю одним replace-запросом и оставляет только последние `Agent:Message:MaxHistoryMessageCount` элементов. Это позволяет выполнить атомарную ETag-проверку без повторного чтения внутри provider.
- Добавлены настройки `Agent:Storage:TableServiceEndpoint` и `Agent:Storage:ConversationTableName`; connection string и account key не используются, доступ выполняется общим `TokenCredential`.
- Таблица автоматически не создаётся приложением: её создание и выдача роли Managed Identity должны быть отдельным явно зафиксированным инфраструктурным шагом.
- На этом этапе никакие Azure-ресурсы и роли не изменялись.

Проверка: сборка всего решения без ошибок и предупреждений; 42 теста agent-модуля прошли.

### 28.09.2026 — unit-тесты Azure Table conversation storage

- Доступ к Azure SDK изолирован внутренним `IAgentConversationTableApi`, чтобы storage-логику проверять без сети, Azure account и эмулятора.
- Добавлен отдельный test-проект в структуру `Agent.Storage.Table/Test`.
- Storage-интерфейс, модели результата и failure code перенесены из общего `Agent/Contract` в `Agent.Storage.Table/Contract`; Agent Core зависит от отдельного контракта, а не от реализации `Api`.
- Дерево модуля приведено к принятой структуре: контракты сгруппированы в `Contract/Conversation`, реализация разделена на `Api/Api` и `Api/Table`, а тесты размещены зеркально в `Test/Test.Api`.
- Тестами покрыты отсутствие entity, восстановление сообщений и ETag, формирование доверенных partition/row keys, сериализация при создании и отображение HTTP 404/409/412 в `ConversationConflict`.
- Интеграционный запрос в Azure Table не выполнялся; Azure-конфигурация не изменялась.

Проверка: 6 тестов Table storage прошли.

### 28.09.2026 — базовое Table-хранилище заданий агента

- В `Agent.Storage.Table/Contract/Request` добавлены контракты задания, его состояния и хранилища.
- Идентификатор задания детерминированно строится из доверенного `BotId` и исходного `TelegramUpdateId`; повторная доставка одного Telegram update не создаёт второе задание.
- При повторном update исходный текст и locale сравниваются с сохранёнными значениями. Другой payload с тем же ключом возвращает конфликт.
- Чтение задания дополнительно проверяет `TelegramUserId` и `TelegramChatId`, поэтому знания одного `requestId` недостаточно для доступа к чужому результату.
- Реализованы только создание задания в состоянии `Queued` и безопасное чтение. Переходы состояний, outbox, Queue, worker и HTTP endpoint будут отдельными инкрементами.
- Для заданий предусмотрена отдельная настройка `Agent:Storage:RequestTableName`; в приложение она пока не подключена, Azure-ресурсы и роли не изменялись.

Проверка: 11 тестов модуля Table storage прошли.

### 28.09.2026 — конкурентно-безопасные переходы состояния задания

- Контракт задания дополнен состояниями `AwaitingConfirmation` и `Indeterminate`, необходимыми для будущих подтверждений и неопределённого результата внешней записи.
- В `IAgentRequestStore` добавлено обновление с обязательными ожидаемыми ETag и исходным состоянием.
- Перед записью повторно проверяется владелец задания. Несовпадение версии либо состояния возвращает `Conflict`, а HTTP 404 Table Storage отображается в `NotFound`.
- Table entity обновляется целиком с исходными идентификаторами Telegram и payload; результат либо безопасный код ошибки сохраняются вместе с новым состоянием.
- Допустимые бизнес-переходы будет определять worker/application-слой; storage отвечает только за атомарную проверку ожидаемого состояния и версии.
- Queue, outbox, worker, HTTP endpoint и Azure-конфигурация в этом инкременте не изменялись.

Проверка: 16 тестов модуля Table storage прошли.

### 28.09.2026 — transactional outbox заданий

- Создание request entity и соответствующей outbox entity теперь выполняется одной транзакцией Azure Table в общей partition вызывающего бота.
- Повторная доставка Telegram update по-прежнему возвращает существующее задание и не создаёт второй outbox элемент.
- Добавлен контракт чтения ограниченной пачки outbox и удаления элемента по ETag после успешной публикации в Queue.
- HTTP 404 при удалении считается идемпотентным успехом; конкурентная смена ETag возвращает `Conflict`.
- Outbox хранит только доверенные идентификаторы маршрутизации (`BotId`, Telegram user/chat и `RequestId`), а исходный текст остаётся в request entity.
- Параллельные dispatcher-ы в будущем могут отправить дубликат до удаления outbox; это ожидаемая at-least-once доставка, которую worker обязан дедуплицировать через состояние и ETag задания.
- Azure Queue-клиент, dispatcher, worker, HTTP endpoint и Azure-конфигурация в этом инкременте не добавлялись.

Проверка: 21 тест модуля Table storage прошёл.

### 28.09.2026 — отказ от очереди в первой версии

- По итогам повторного обсуждения выбрана синхронная схема `Telegram bot → POST /internal/agent/messages → Semantic Kernel → ответ`.
- Azure Queue, persistent request, status polling, transactional outbox, dispatcher и worker признаны преждевременными для текущего read-only этапа и удалены из рабочего кода.
- Ранее созданные request storage и переходы состояния также удалены отдельным изменением; существующие коммиты не переписывались, чтобы история решения оставалась прозрачной.
- Ограниченная история диалога в Azure Table сохраняется: она нужна независимо от способа выполнения запроса.
- Очередь будет возвращена только при подтверждённой проблеме с timeout, рестартами во время обработки, нагрузкой или появлении действительно длительных фоновых операций.
- Azure-ресурсы и конфигурация не изменялись.

Следующий инкремент: один защищённый синхронный `POST /internal/agent/messages`, который разрешает доверенный Telegram-контекст и вызывает готовый `IAgentConversationMessageFunc`.

### 28.09.2026 — синхронный endpoint сообщений агента

- Добавлен модуль `Agent.Message.Send` в принятой структуре `Contract` / `Endpoint/Func` / `Test/Test.Func`.
- `POST /internal/agent/messages` принимает доверенный `BotId` из claim вызывающего приложения, Telegram update/user/chat, текст и locale из JSON body.
- Endpoint сначала разрешает актуальную привязку пользователя через `IAgentUserContextResolver`, затем передаёт только доверенный `AgentUserContext` в `IAgentConversationMessageFunc`.
- Вызов построен через `AsyncPipeline`; HTTP endpoint не содержит Semantic Kernel, storage или Dataverse-логику.
- Ошибки identity, сообщения и конкурентного изменения истории отображаются в безопасные endpoint failure codes; детали инфраструктурных исключений наружу не проектируются.
- Endpoint зарегистрирован в основном приложении и защищается существующим контуром `/internal/agent/*`, feature flag и app-only авторизацией.
- Очередь, worker и status endpoint не возвращались; Azure-конфигурация не изменялась.

Проверка модуля endpoint: 14 тестов прошли.

### 28.09.2026 — обработка обычного текста в Telegram-боте

- В `internal-timesheet-bot-app` добавлен typed `AgentMessageApi`, использующий существующий app-only `AgentAccessTokenHandler` и `AgentApi:BaseAddress` / `AgentApi:Audience`.
- Добавлена команда `Logic.Agent.Message` в структуре существующего диагностического модуля; dependency собирается через `Dependency`.
- Parser принимает только обычное непустое текстовое сообщение и выполняется перед fallback `WelcomeCommand`; Telegram bot commands и вложения агенту не передаются.
- В API отправляются фактические `UpdateId`, `Message.From.Id`, `Chat.Id`, текст и Telegram language code. Пользовательские идентификаторы не извлекаются из текста.
- Ответ агента HTML-экранируется перед отправкой в Telegram. Для отсутствующей привязки и конфликта истории предусмотрены отдельные безопасные сообщения; техническая ошибка журналируется без показа деталей пользователю.
- Timeout typed HTTP client увеличен с 30 до 60 секунд для синхронного вызова Foundry. Очередь и фоновые задания не добавлялись.
- Azure-конфигурация не изменялась.

Проверка Telegram-бота: сборка успешна, 7 тестов прошли, включая новые тесты HTTP-контракта и parser-а.

## Что ещё не сделано на текущем этапе

- Не введены `BindingVersion`, отзыв ожидающих действий и централизованный logout.
- Не проверены и не исправлены существующие дубли привязок в реальной CRM; уникальное ограничение на
  `(BotId, TelegramUserId)` пока не добавлено.
- Диагностический `/profile` пока сохранён по решению пользователя и не передаётся модели как tool.
- Не реализованы preview, подтверждение, идемпотентность и аудит write-операций.
- Агент пока не создаёт и не изменяет timesheet.
- Не выполнены нагрузочные проверки, cost monitoring и production security/data-residency review.
- Production-инфраструктура не изменялась.

## Следующий инкремент

Подготовить дизайн подтверждаемого списания времени, не включая write tool до согласования контракта:

1. Уточнить у руководителя поведение `Timesheet.Update`: можно ли передавать `CallerObjectId` напрямую
   и является ли текущий контракт недоработкой.
2. Определить DTO preview и подтверждения, срок жизни подтверждения и защиту от повторной доставки.
3. Определить аудит и поведение при timeout/неопределённом результате Dataverse.
4. После согласования реализовать один write-сценарий через существующие `AsyncPipeline`, `Dependency`
   и принятую модульную структуру.
5. Повторить unit, integration и Telegram end-to-end тесты до production-подготовки.

### 29.09.2026 — аудит write-путей и дизайн подтверждения

- Повторно проверены существующие `Timesheet.Modify` и `Timesheet.Delete`, включая входные контракты, построение Dataverse-запросов и unit-тесты.
- Подтверждено: create и delete передают пользовательский Entra object ID в `CallerObjectId`; update не устанавливает `CallerObjectId` в итоговом Dataverse update-запросе.
- Для update пользовательский ID применяется только при чтении нового проекта. Если проект не меняется, write выполняется без использования переданного `SystemUserId` в рассмотренном коде.
- Определён первый write-сценарий: модель сможет только подготовить создание одного списания; исполнение выполняется отдельным серверным confirm flow по сохранённым аргументам, без повторного решения модели.
- Определены владелец pending action, TTL 10 минут, атомарный переход состояния, идемпотентные confirm/cancel и состояние `Indeterminate` при неизвестном результате Dataverse.
- До выбора CRM-side механизма идемпотентности создание нельзя автоматически повторять после timeout.
- Write tool, endpoint подтверждения, callback бота, Table entity и Azure-конфигурация не добавлялись. Рабочее read-only поведение не изменилось.

Проверка: выполнен статический аудит исходного кода; сборка не требовалась, поскольку изменена только документация.

Вопрос по `CallerObjectId` закрыт после ручной проверки в test: `AgentUserContext.EntraObjectId` используется как caller identity будущих agent create/update/delete.

### 29.09.2026 — пробная impersonation для существующего Timesheet.Update

- По решению владельца проекта существующий `Timesheet.Update` теперь передаёт `TimesheetUpdateIn.SystemUserId` в `DataverseEntityUpdateIn.CallerObjectId` при любом наборе изменяемых полей.
- Изменение сделано только в старой пользовательской реализации update; write-инструменты агента по-прежнему не подключены.
- Unit-тесты update проверяют, что в Dataverse передаётся тот же пользовательский Entra object ID, который получен существующим контрактом из claim `oid`.
- После развёртывания на test владелец проекта вручную проверит обновление списания. Если Dataverse и существующие плагины не возвращают ошибок, одинаковая impersonation будет принята для будущих agent create/update/delete.
- Azure-конфигурация не изменялась.

Проверка: 92 теста `Timesheet.Modify` прошли; полная сборка решения завершилась без ошибок и предупреждений.

Ручная проверка после deployment: существующий `Timesheet.Update` с `CallerObjectId` работает корректно. Решение принято как единое правило impersonation для трёх будущих write-операций агента.

### 29.09.2026 — базовое хранилище ожидающего создания

- В `Agent.Storage.Table/Contract/Action` добавлен типизированный контракт подготовленного создания списания и состояния action.
- В `Agent.Storage.Table/Api/Action` добавлен provider создания и чтения pending action с отдельными option/dependency в стиле существующего conversation storage.
- Действие принадлежит одновременно текущим `BotId`, Telegram user/chat, `BindingId`, CRM system user и Entra object ID. Несовпадение любого owner-поля возвращается как отсутствие действия и не раскрывает чужие данные.
- Новый action всегда сохраняется в состоянии `Pending`; дата и decimal duration сериализуются инвариантно.
- Подготовленные данные включают точные дату, проект, тип проекта, длительность, комментарий и срок действия. Модель не управляет owner-полями.
- Provider пока не зарегистрирован в Application, write tool и confirm endpoint не добавлены, таблица `TimesheetAgentAction` в Azure не создавалась.
- После замечания владельца проекта из нового кода удалены все применения `!`; обязательные Table-поля читаются через явную проверку, сравнения оформлены через `is false`. Правило добавлено в основную инструкцию.

Проверка: 10 тестов `Agent.Storage.Table` прошли. Следующий инкремент — атомарные переходы состояния action по ETag, необходимые для безопасного confirm/cancel.

### 29.09.2026 — атомарные переходы состояния action

- В `IAgentActionStore` добавлен переход из явно ожидаемых версии и состояния в новое состояние.
- Перед обновлением provider повторно читает запись и проверяет полного владельца, `ETag` и текущее состояние.
- Обновление Azure Table выполняется с прочитанным `ETag`; HTTP 404, 409 и 412 отображаются в безопасный `Conflict`.
- Повторный либо конкурентный callback не сможет второй раз перевести одно действие из `Pending` в `Executing`.
- Storage не определяет допустимый бизнес-граф переходов: orchestration-слой будет выбирать ожидаемое и следующее состояния для confirm, cancel, success, failure и indeterminate.
- Правило проекта соблюдено: в новом коде отсутствует оператор `!`.
- Application, Semantic Kernel, Telegram-бот и Azure-конфигурация не изменялись.

Проверка: 17 тестов `Agent.Storage.Table` прошли. Следующий инкремент — application-функция подготовки создания с серверным TTL и проверкой проекта до сохранения `Pending`.

### 29.09.2026 — application-функция подготовки создания

- В Agent Contract/Core добавлен отдельный модуль `Timesheet.PrepareCreate` в принятой структуре `Contract` / `Core` / `Test`.
- Функция получает только бизнес-параметры и доверенный `AgentUserContext`; owner identity, `ActionId`, время создания и TTL не управляются моделью.
- Проверяются ID, имя и тип проекта, положительная длительность и обязательный комментарий.
- Перед созданием `Pending` выполняется повторный пользовательский поиск проекта; ID и тип должны присутствовать в результате, а каноническое название берётся с сервера.
- `DateProvider` расширен `UtcNow`, чтобы TTL вычислялся через принятую абстракцию времени и детерминированно тестировался.
- Подготовленное действие сохраняется через `IAgentActionStore`; конфликт и ошибки project search отображаются в типизированные failure codes.
- Функция собирается через `Dependency`, но пока не подключена к Application и не зарегистрирована как Semantic Kernel tool.
- В новом коде отсутствует оператор `!`; Azure-конфигурация не изменялась.

Проверка: 51 тест Agent Core прошёл. Следующий инкремент — безопасный `prepare_create_timesheet` plugin boundary и выдача структурированного preview вызывающему message flow без выполнения CRM write.

### 29.09.2026 — Semantic Kernel boundary подготовки создания

- Добавлен отдельный `AgentWritePlugin` с единственной функцией `prepare_create_timesheet`; функций confirm, execute, update и delete в plugin нет.
- На границе Kernel дата принимается строкой строгого формата `yyyy-MM-dd`, чтобы не повторять проблему автоматического binding в `DateOnly`.
- Остальные аргументы ограничены бизнес-данными: project ID/name/type, decimal duration и комментарий. Доверенный `AgentUserContext` передаётся конструктором plugin и отсутствует в схеме функции.
- Добавлен scoped `AgentPreparedActionCapture`, который сохраняет типизированный результат prepare отдельно от текста модели и разрешает не более одной успешно подготовленной операции за один ход.
- Ошибки даты, валидации и storage возвращаются модели безопасными кодами; при неуспешной подготовке capture освобождается для корректировки аргументов в том же ходе.
- Проверен реальный вызов функции через `Kernel.InvokeAsync` с `KernelArguments`, включая строковую дату и числовые значения.
- Plugin намеренно не зарегистрирован в рабочем `AgentKernelFactory`: текущий deployed агент остаётся read-only, пока action storage не подключён к Application и таблица не создана в Azure.
- В новом коде отсутствует оператор `!`; Azure-конфигурация не изменялась.

Проверка: 57 тестов Agent Core прошли. Следующий инкремент — передать capture в `AgentMessageOut`/HTTP response как структурированный preview, сохранив обратную совместимость текстового ответа.

### 29.09.2026 — структурированный preview в message response

- `IAgentKernelFactory` теперь создаёт `AgentKernelScope`, содержащий Kernel и capture конкретного хода; mutable capture не разделяется между пользователями или запросами.
- После завершения model turn `AgentMessageFunc` добавляет подготовленное действие в `AgentMessageOut` независимо от текста модели.
- `AgentConversationMessageFunc` продолжает сохранять в историю только пользовательский и ассистентский текст, но возвращает типизированный preview вызывающему endpoint.
- Ответ `POST /internal/agent/messages` расширен необязательным `preparedAction` с action ID, датой, проектом, типом, длительностью, комментарием и сроком действия.
- Поле `text` и существующий конструктор ответа сохранены, поэтому текущий бот-клиент остаётся обратно совместимым.
- Рабочий factory пока не регистрирует write-plugin, поэтому deployed endpoint фактически продолжает возвращать только текст; структурированный путь покрыт unit-тестами и будет активирован после подключения storage.
- В новом коде отсутствует оператор `!`; Azure-конфигурация не изменялась.

Проверка: 58 тестов Agent Core и 15 тестов `Agent.Message.Send` прошли. Следующий инкремент — подключить action storage и prepare-функцию в Application под отдельным feature flag, не включая исполнение или callback.

### 29.09.2026 — Application composition и feature flag подготовки

- Action Table provider и `AgentTimesheetCreatePrepareFunc` подключены в Application через существующие `Pipeline/Dependency` composition roots.
- Из-за ограничения `Dependency` в семь типов Kernel-инструменты объединены в типизированный `AgentKernelToolSet`; service locator и ручное получение зависимостей не используются.
- `AgentKernelFactory` регистрирует `TimesheetWritePreparation` только при `Agent:WritePreparation:Enabled = true`; значение по умолчанию в `appsettings.json` — `false`.
- Системный prompt синхронизирован с flag: при выключенном flag он сохраняет read-only правила, при включённом разрешает только prepare создания и запрещает заявлять о фактической записи до подтверждения.
- Добавлены настройки `Agent:Storage:ActionTableName`, `Agent:WritePreparation:ApprovalTtlMinutes` и `Agent:WritePreparation:ProjectSearchTop` с проверкой HTTPS endpoint, имени таблицы, положительного TTL и лимита поиска.
- Создание `TableClient` не обращается к Azure; при выключенном flag action table не используется. Поэтому код можно развернуть без изменения текущего поведения, оставив flag выключенным.
- Таблица `TimesheetAgentAction` в Azure не создавалась, app settings Azure не менялись, confirm/cancel и CRM execution отсутствуют.
- В новом коде отсутствует оператор `!`.

Проверка: 59 тестов Agent Core и 15 тестов `Agent.Message.Send` прошли; полная сборка завершилась без ошибок и предупреждений. Следующий инфраструктурный шаг перед включением — создать таблицу в test и добавить настройки с flag `false`, затем отдельно включить prepare-only smoke test.

### 29.09.2026 — test-инфраструктура для подготовленных действий

- В test Storage Account `stinternalgtimesheettest` создана таблица `TimesheetAgentAction`.
- В test App Service добавлены имя action table, TTL 10 минут и лимит поиска проектов 20.
- `Agent__WritePreparation__Enabled` установлен в `false`: агент остаётся read-only, новый сценарий пользователям не включён.
- Новые Managed Identity и RBAC assignments не потребовались; используется уже выданный API доступ к существующему Storage Account.
- Применение App Settings могло вызвать recycle; после изменения API подтверждён в состоянии `Running`.
- APIM, Telegram-бот, Foundry, Entra ID и production не изменялись.
- Точные ресурсы, команды, влияние и откат записаны в журнале изменений Azure.

Проверка: таблица найдена через Azure CLI, четыре настройки прочитаны из App Service с ожидаемыми значениями. Следующий инкремент — после развёртывания коммита `eed96a2` временно включить prepare-only flow в test и проверить создание структурированного preview и записи `Pending`, не выполняя запись в CRM.

### 29.09.2026 — сквозная проверка prepare-only flow

- CI/CD deployment API подтверждён на коммите `0e9a6b4`; App Service работает.
- В test включён `Agent__WritePreparation__Enabled=true`; production не изменялся.
- Проверка выполнена синтетическими Telegram updates через защищённый webhook настоящего test-бота. Вся остальная цепочка была реальной: Durable Entity → Managed Identity → APIM → API → Semantic Kernel → Foundry → Storage/Dataverse tools → Telegram API.
- Позитивный запрос подготовил `Pending` action на `Test 01` с датой `2026-09-29`, длительностью `0.5`, точным комментарием и TTL 10 минут.
- Чтение списаний после подготовки подтвердило отсутствие новой записи в Dataverse.
- Неизвестный проект и пустой комментарий не создали action.
- При запросе двух операций за один ход capture разрешил сохранить только первую; обе существующие тестовые записи остались `Pending`.
- Текст модели уже предлагает подтвердить действие кнопкой, но бот пока не использует структурированный `preparedAction` и не отображает inline keyboard. Это ожидаемый следующий этап, а не выполненная возможность.
- Ответ при пустом комментарии сформулирован неточно: модель предлагает подтвердить пустое значение, хотя серверная валидация всё равно запрещает его. При реализации UI системную инструкцию нужно уточнить: запросить комментарий без предложения подтвердить недопустимое действие.
- Обнаружен security-риск существующей телеметрии: полный Telegram Bot API URL с токеном попадает в Application Insights trace. Значение не переносилось в Git. Нужны redaction/suppression и последующая ротация токена до production.
- Визуальное отображение сообщений в Telegram-клиенте остаётся подтвердить вручную; серверная телеметрия уже подтверждает успешный `sendMessage`.

Проверка: webhook HTTP 204, agent endpoint HTTP 200, Telegram send HTTP 200; содержимое conversation/action tables и отсутствие фактического списания проверены. Следующий инкремент — контракт confirm/cancel и Telegram inline keyboard, начиная с безопасного получения action только его владельцем.

### 29.09.2026 — Core-функции confirm/cancel без CRM execution

- В Agent Contract/Core/Test добавлены отдельные модули `Timesheet.ConfirmCreate` и `Timesheet.CancelCreate` с принятой папочной структурой.
- Обе функции получают только доверенный `AgentUserContext` и `ActionId`; бизнес-аргументы подготовленного списания повторно не принимаются.
- Action загружается через `IAgentActionStore`, который проверяет полного владельца: bot, Telegram user/chat, binding, CRM system user и Entra object ID. Чужое действие отображается как `NotFound`.
- Confirm атомарно переводит только `Pending → Executing`, cancel — только `Pending → Cancelled`.
- TTL проверяется серверным `DateProvider.UtcNow`. Истёкшее pending-действие атомарно переводится в `Expired` и не может стать `Executing`.
- Переход выполняется с сохранённой ETag/version и ожидаемым состоянием `Pending`; повторный либо конкурентный вызов не может выполнить второй переход.
- Добавлены отдельные failure codes для пустого Action ID, отсутствия, истечения, неверного состояния, конфликта и неизвестной storage-ошибки.
- Функции зарегистрированы как `Dependency`-расширения Core, но не подключены к Application, HTTP endpoint или Telegram callback.
- Реальный `ITimesheetCreateFunc` не вызывается; Dataverse и Azure-конфигурация не изменялись.
- В новом коде отсутствует оператор `!`.

Проверка: 77 тестов Agent Core и 17 тестов Action Table прошли; полная solution-сборка завершилась без ошибок и предупреждений. Следующий инкремент — execution orchestration после успешного `Pending → Executing` с использованием сохранённых аргументов и доверенного caller identity, включая конечные состояния `Succeeded`, `Failed` и `Indeterminate`.

### 29.09.2026 — execution orchestration создания списания

- Confirm после атомарного `Pending → Executing` вызывает существующий `ITimesheetCreateFunc` напрямую, без HTTP к собственному API и без повторного участия модели.
- `TimesheetCreateIn` строится только из сохранённого action; caller identity берётся из `AgentUserContext.EntraObjectId`. Вход confirm по-прежнему содержит только `ActionId`.
- Успешный CRM-вызов переводит action `Executing → Succeeded`.
- Детерминированные бизнес-ошибки (`BadRequest`, неверный тип, пустое описание, `Forbidden`, отсутствующий проект) переводят action `Executing → Failed` и возвращают безопасный код.
- Неизвестная ошибка либо исключение во время CRM-вызова переводит action `Executing → Indeterminate`; автоматический retry запрещён, потому что существующий create-контракт возвращает `Unit` и не позволяет доказать, была ли запись создана до потери ответа.
- Терминальный переход использует повторно загруженную ETag версии `Executing` и выполняется с `CancellationToken.None`, чтобы разрыв клиентского запроса после начала CRM write не отменил фиксацию результата.
- Если результат CRM известен, но терминальное состояние не удалось сохранить, confirm возвращает `Indeterminate`. Action может остаться `Executing` и потребует будущей процедуры reconciliation; повторное создание автоматически не выполняется.
- Application, HTTP endpoint, Telegram callback и Azure не изменялись; развернутый агент пока не может вызвать confirm.
- В новом коде отсутствует оператор `!`.

Проверка: 85 тестов Agent Core прошли; полная solution-сборка завершилась без ошибок и предупреждений. Следующий инкремент — подключить confirm/cancel к Application и добавить защищённый callback endpoint, который повторно разрешает `AgentUserContext` по доверенным Telegram identifiers.

### 29.09.2026 — защищённый endpoint решения по action

- Добавлен endpoint-модуль `Agent.Action.Decide` в принятой структуре `Contract` / `Endpoint` / `Test`.
- Маршрут `POST /internal/agent/actions/{actionId}/decision` принимает только Telegram update/user/chat и enum `Confirm`/`Cancel`; дата, проект, длительность, комментарий и caller identity отсутствуют во входном контракте.
- `BotId` поступает из доверенного claim `timesheet_bot_id`, сформированного существующим app-only middleware после проверки токена, app role и allowlist клиента.
- Перед confirm/cancel endpoint повторно разрешает полный `AgentUserContext` через текущий binding. Чужой action не раскрывается и отображается как `ActionNotFound`.
- Confirm и cancel собраны через `Pipeline/Dependency`; confirm использует существующий `ITimesheetCreateFunc` напрямую, без self-HTTP.
- Endpoint дополнительно закрыт `Agent:WritePreparation:Enabled`: при `false` отказ происходит до user resolver, Storage и Dataverse.
- Добавлены типизированные HTTP failure codes для identity, binding, action state/TTL/conflict, бизнес-ошибок создания и `Indeterminate`.
- Для `Timesheet.Modify` добавлен отдельный dependency `UseTimesheetCreateFunc`, чтобы переиспользовать существующую реализацию создания без endpoint set и service locator.
- Проекты Contract/Endpoint/Test добавлены в solution, Application и `Program`.
- APIM operation и Telegram callback пока не добавлены; deployed API/бот не изменились. Azure не изменялся.
- В новом коде отсутствует оператор `!`.

Проверка: 30 тестов `Agent.Action.Decide` прошли; полная solution-сборка завершилась без ошибок и предупреждений. Следующий инкремент — добавить typed callback client и inline keyboard в Telegram-бот, затем после deployment создать test APIM operation для decision route.

### 29.09.2026 — Telegram-клиент подтверждения и отмены

- В боте ответ `/internal/agent/messages` расширен типизированным `preparedAction`; при отсутствии действия поведение обычных ответов не меняется.
- Если API вернул подготовленное действие, бот показывает inline-кнопки «Подтвердить» и «Отменить».
- В `callback_data` помещаются только префикс решения и `ActionId` в компактном `N`-формате; Telegram user/chat/update берутся из подписанного Telegram update, а не из данных кнопки.
- Добавлена отдельная callback-команда в принятой структуре `Logic.Agent.Action`, зарегистрированная перед обработчиком обычного текста.
- Callback вызывает typed client защищённого endpoint `POST /internal/agent/actions/{actionId}/decision`. Клиент использует тот же `HttpClient` и существующий `AgentAccessTokenHandler`, поэтому отдельный токен или секрет не добавляется.
- После ответа API клавиатура удаляется, а пользователь получает отдельное сообщение об успешном списании или отмене. Ошибка Telegram после успешного API-вызова не переопределяет фактический результат операции.
- `NotFound` и `Conflict` отображаются безопасно. Для `Conflict` бот не предлагает слепой retry: такой статус также может означать `Indeterminate`, поэтому пользователь должен сначала проверить списания.
- Изучен существующий confirmation flow в SupportBot. Полный `ChatFlow` не переносился: он предназначен для хранения пошагового состояния, а состояние agent action уже хранится сервером и адресуется через opaque `ActionId`.
- Отдельно подтверждено архитектурное решение не переносить action state из API в Telegram-бот. Бот отвечает только за UI и доставку решения; API остаётся источником истины для payload, owner, TTL, ETag, идемпотентности и результата Dataverse. В будущем `ChatFlow` может хранить шаги интерфейса, но не заменяет серверный action store.
- Текущая версия Telegram engine поддерживает получение callback и inline keyboard, но не предоставляет отдельной операции `answerCallbackQuery`. Прямой вызов Telegram API в обход движка не добавлялся; после test smoke необходимо проверить поведение индикатора нажатия в клиенте.
- В новом коде отсутствует оператор `!`.
- Этот инкремент ещё не закоммичен и не развернут. API deployment, APIM и Azure-конфигурация не изменялись.

Проверка: все 17 тестов bot solution прошли; сборка завершилась без ошибок и предупреждений. Следующий шаг после review и коммита — развернуть API с endpoint решения, добавить его operation в test APIM, развернуть бот и вручную проверить confirm/cancel/repeated callback.

### 29.09.2026 — test APIM route для решения

- В test agent API добавлена operation `post-agent-action-decision`: `POST /internal/agent/actions/{actionId}/decision`.
- Operation не имеет собственной policy и наследует существующие backend certificate и JWT forwarding общей agent API policy.
- Другие Azure-ресурсы и production не изменялись; изменение подробно записано в Azure change log с шагами отката и переноса.
- API endpoint и новая версия бота на момент добавления operation ещё не развёрнуты, поэтому сквозная проверка ожидает deployment API.

Проверка: operation повторно прочитана из APIM с ожидаемыми ID, HTTP method, URL template и обязательным `actionId`. Следующий шаг — deployment API пользователем, после чего ZIP deployment бота и сквозной smoke test confirm/cancel.

### 29–30.09.2026 — deployment бота и prepare smoke test

- Пользовательский CI/CD deployment API подтверждён на коммите `b95db9a`; decision endpoint присутствует в test App Service.
- Telegram-бот с inline-кнопками развёрнут ZIP-пакетом из коммита `239f29f`; deployment `11fc58dd-c1c8-427b-9b68-c070c6cfd664` завершён успешно.
- Перед deployment прошли все 17 Release-тестов. Publish-пакет не содержал `launchSettings.json` и `local.settings.json`.
- Function App находится в `Running/Normal`, обнаружены `HandleBotEntity`, `HandleBotHttp` и `HealthCheck`.
- Test APIM decision route без токена вернул `401`, подтвердив сохранение app-only защиты.
- Первый update после deployment опоздал к Durable Entity и завершил agent request с `499`; выполнен один restart только test Function App без изменения настроек.
- После прогрева два prepare-запроса завершились через agent API с HTTP 200 и создали два `Pending` action. До подтверждения Dataverse write отсутствует.
- Telegram dependencies завершились HTTP 200; реальные сообщения с preview и кнопками должны быть доставлены пользователю.
- Синтетический callback с тестовым message ID не дошёл до decision endpoint, поэтому callback flow пока не считается проверенным. Оба action остались `Pending` и должны быть отменены реальной Telegram-кнопкой либо истечь по TTL.
- Неверная кодировка комментария в первых тестовых action вызвана локальным PowerShell smoke payload; приложение получило уже искажённую строку и корректно сохранило переданное значение.

Проверка: API deployment, APIM authentication boundary, bot ZIP deployment, prepare path, Action Table и Telegram send подтверждены. Следующий шаг — одно ручное нажатие «Отменить» на реальной кнопке, затем проверить decision request, переход `Pending → Cancelled`, удаление клавиатуры и пользовательский ответ.

### 30.09.2026 — реальный callback отмены и исправление response enum

- Пользователь нажал реальную кнопку «Отменить» на свежем action.
- Первый callback дошёл до decision endpoint с HTTP 200 и фактически перевёл action в `Cancelled`, но бот показал общий текст ошибки.
- Причина подтверждена телеметрией: API возвращает `decision` как строковый enum, а typed client пытался десериализовать его без `JsonStringEnumConverter`.
- В боте добавлены отдельные response serializer options со string-enum converter. Формат request не менялся.
- Unit-тест теперь использует фактический JSON `"decision":"Confirm"`; все 17 Release-тестов и проверка форматирования прошли.
- Исправленный бот развёрнут повторным ZIP deployment `bc528e4e-75cf-4335-81a4-d1e124ee2a1e`; Function App перезапущен и подтверждён в состоянии `Running/Normal`.
- На втором свежем action пользователь снова нажал «Отменить». Decision endpoint вернул HTTP 200 за 391 мс, action перешёл в `Cancelled`, бот ответил «Списание отменено.», коррелированных исключений нет.
- Ни один cancel smoke test не выполнял запись в Dataverse.

Проверка: реальный Telegram callback → Durable Entity → Managed Identity → APIM → decision endpoint → Action Table → Telegram response подтверждён полностью для ветки Cancel. Осталось подтвердить удаление inline-клавиатуры в клиенте и отдельно безопасно проверить ветку Confirm на контролируемом тестовом списании.

## Открытые вопросы

- Подтвердить с владельцем безопасности выбранные значения: `auth_date` 5 минут и clock skew 30 секунд.
- Должен ли один CRM-пользователь иметь возможность привязать несколько Telegram-аккаунтов к одному боту?
- Нужно ли разрешать осознанную перепривязку Telegram-пользователя к другой корпоративной учётной записи, и каким подтверждением её защищать?
- Есть ли в CRM существующие дубли по `(BotId, TelegramUserId)`?
- Для production подтвердить отдельный APIM agent route с backend certificate; в test этот маршрут уже
  выбран и успешно проверен.
- Локальный `launchSettings.json` содержит Telegram bot token, но файл исключён через `.gitignore` и Git его не отслеживает. Ротация из-за одного только локального хранения не требуется; секрет всё равно не следует выводить в логи или пересылать.
- Устранить логирование полного Telegram Bot API URL в Application Insights и после этого ротировать test token; для production включить проверку отсутствия секретов в telemetry до запуска.

## Правила ведения журнала

После каждого завершённого инкремента необходимо:

1. Обновить дату и текущий этап.
2. Кратко перечислить изменённые компоненты и фактическое поведение.
3. Записать принятые архитектурные решения и выявленные ограничения.
4. Указать выполненные команды проверки и их результат.
5. Отдельно отметить то, что не было проверено в реальной инфраструктуре.
6. Сформулировать один ближайший инкремент, не смешивая несколько крупных этапов.
7. Не записывать в журнал секреты, токены, Telegram `initData`, connection strings и персональные данные.
8. Любое изменение Entra ID, Managed Identity, App Service, Function App, APIM, webhook или deployment-конфигурации параллельно фиксировать в журнале изменений Azure с инструкцией для production.
