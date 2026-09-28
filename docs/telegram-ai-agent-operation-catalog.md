# Telegram AI Agent: каталог операций

Последнее обновление: 28.09.2026.

Документ описывает существующие бизнес-функции API и решение об их использовании будущим Semantic Kernel agent. Это инвентаризация, а не регистрация функций как AI tools.

Связанные документы:

- [план реализации](telegram-ai-agent-implementation-plan.md);
- [журнал прогресса](telegram-ai-agent-progress.md);
- [журнал изменений Azure](telegram-ai-agent-azure-change-log.md).

## Базовые правила

1. Semantic Kernel размещается внутри API и вызывает C# business-функции напрямую, без HTTP-вызова API к самому себе.
2. Telegram-бот обращается только к одному будущему endpoint `/internal/agent/messages`. `Agent.Profile.Get` временно сохраняется как диагностический и демонстрационный endpoint.
3. `BotId`, Telegram identity, CRM System User ID и Entra Object ID поступают только из доверенного `AgentUserContext`. Модель не получает их как управляемые аргументы.
4. В первую итерацию подключаются только read-only tools.
5. Любая операция записи требует серверного prepare/confirm flow, идемпотентности и аудита. Одного подтверждения в prompt недостаточно.
6. Наличие существующего HTTP endpoint не означает, что его внутренняя функция уже безопасна для агента.
7. Агент получает не всё API, а явный allowlist операций, необходимых для списания времени. Профиль, подписки, уведомления и account flow не являются AI tools.

## Классификация

| Статус | Значение |
|---|---|
| `Read candidate` | Можно готовить adapter/plugin после проверки входного контекста и лимитов результата |
| `Write blocked` | Не подключать до реализации подтверждений, идемпотентности и проверки прав |
| `System only` | Служебная операция, модель не должна выбирать её как tool |
| `Diagnostic` | Временная операция для проверки и демонстрации контура |
| `Excluded` | Операция осознанно не входит в функциональные границы агента |

## Матрица существующих операций

| Модуль / функция | Назначение | Пользовательские аргументы | Доверенный контекст | Тип | Решение для агента |
|---|---|---|---|---|---|
| `Agent.Profile.Get / IAgentProfileGetFunc` | Проверка Telegram-привязки и получение профиля | Нет | Bot ID, Telegram user/chat | Read | `Diagnostic`; сохранить до отдельного указания |
| `Profile.Get / IProfileGetFunc` | Имя и язык профиля пользователя текущего бота | Нет | Entra Object ID | Read | `Excluded`; не требуется для работы со списаниями |
| `Period.GetSet / IPeriodSetGetFunc` | Доступные периоды списания | Нет | Не требуется | Read | `Read candidate` |
| `Project.GetLastSet / ILastProjectSetGetFunc` | Последние проекты пользователя | Необязательный `top` | Entra Object ID | Read | `Read candidate`; ограничить `top` на сервере |
| `Project.GetSet / IProjectSetGetFunc` | Общий набор активных проектов с пользовательской историей | Нет | Entra Object ID | Read | `Read candidate`; дополнительно проверить видимость четырёх типов проектов |
| `Project.SearchSet / IProjectSetSearchFunc` | Поиск project/incident/opportunity/lead | Строка поиска, необязательный `top` | Entra Object ID как `CallerObjectId` | Read | `Read candidate`; ограничить длину строки и `top` |
| `Timesheet.GetSet / ITimesheetSetGetFunc` | Списания пользователя за диапазон дат | `dateFrom`, `dateTo` | Entra Object ID | Read | `Read candidate`; ограничить диапазон дат и размер результата |
| `Tag.GetSet / ITagSetGetFunc` | Хэштеги пользователя по выбранному проекту | Project ID | Entra Object ID | Read | `Read candidate`; Project ID должен происходить из разрешённого результата поиска/выбора |
| `Subscription.GetSet / ISubscriptionSetGetFunc` | Настройки уведомлений пользователя | Нет | CRM System User ID | Read | `Excluded`; не относится к работе со списаниями |
| `Timesheet.Modify / ITimesheetCreateFunc` | Создание списания | Дата, проект, длительность, комментарий | CRM System User ID как `CallerObjectId` | Write | `Write blocked` |
| `Timesheet.Modify / ITimesheetUpdateFunc` | Изменение списания | Timesheet ID и изменяемые поля | CRM System User ID | Write | `Write blocked`; update-запрос требует дополнительного аудита impersonation/ownership |
| `Timesheet.Delete / ITimesheetDeleteFunc` | Удаление списания | Timesheet ID | CRM System User ID как `CallerObjectId` | Write | `Write blocked` |
| `Profile.Update / IProfileUpdateFunc` | Изменение языка профиля | Language code | CRM System User ID | Write | `Excluded`; не относится к работе со списаниями |
| `Notification.Subscribe / INotificationSubscribeFunc` | Настройка ежедневных/еженедельных уведомлений | Тип и параметры уведомления | CRM System User ID | Write | `Excluded`; не относится к работе со списаниями |
| `Notification.Subscribe / INotificationUnsubscribeFunc` | Отключение уведомлений | Тип уведомления | CRM System User ID | Write | `Excluded`; не относится к работе со списаниями |
| `User.SignIn / IUserSignInFunc` | Создание Telegram/CRM-привязки по подписанному Mini App `initData` | Telegram `initData` | Интерактивный Entra user | Write | `System only`; никогда не вызывать моделью |
| `User.SignOut / IUserSignOutFunc` | Отзыв Telegram-привязки | Нет | Интерактивный Entra user | Write | `System only`; отдельный явный account flow, не обычный tool |

## Первая read-only группа

Первый безопасный набор native plugins предлагается ограничить следующими возможностями:

1. `GetPeriods` — получить допустимые периоды.
2. `GetRecentProjects` — показать последние проекты пользователя.
3. `SearchProjects` — найти проекты по тексту с серверным пределом результата.
4. `GetTimesheets` — показать списания пользователя за ограниченный диапазон.
5. `GetProjectTags` — получить подсказки тегов для уже выбранного проекта.

`Project.GetSet` можно добавить после проверки фактической видимости данных для Project, Incident, Opportunity и Lead, если поиска и последних проектов будет недостаточно. `Profile.Get`, `Profile.Update`, `Subscription.GetSet`, `Notification.Subscribe`, `User.SignIn` и `User.SignOut` в allowlist tools не входят. Диагностический `Agent.Profile.Get` временно остаётся отдельным endpoint для демонстрации авторизации и не регистрируется в Semantic Kernel.

## Обязательные agent adapters

Нельзя регистрировать текущие контракты endpoint напрямую как SK functions: они содержат доверенные идентификаторы рядом с пользовательскими аргументами. Для каждой разрешённой операции нужен тонкий adapter в agent-модуле:

```text
Аргументы модели + AgentUserContext
                ↓
        Agent read adapter
                ↓
 Существующая business-функция
```

Adapter обязан:

- подставлять Entra/CRM ID только из `AgentUserContext`;
- валидировать диапазоны, длины строк и `top`;
- возвращать модели минимальный DTO без служебных полей;
- преобразовывать инфраструктурные ошибки в безопасные типизированные результаты;
- не принимать произвольный `SystemUserId`, `CallerObjectId`, `BotId` или Telegram ID.

## Выявленные риски и проверки до подключения

### `Timesheet.Update`

`TimesheetJson.BuildDataverseUpdateInput` сейчас не устанавливает `CallerObjectId`, тогда как create/delete и чтение проекта используют пользовательскую impersonation. До подключения update необходимо доказать ownership/permission check либо добавить эквивалентный доверенный caller context. Особенно опасен сценарий обновления без смены проекта: в нём нет предварительного Dataverse-чтения проекта с caller identity.

Перед началом реализации write-инструментов обязательно напомнить владельцу проекта уточнить у руководства: отсутствие `CallerObjectId` является осознанной частью текущей CRM-авторизации или недоработкой, которую следует исправить передачей caller identity. До ответа `Timesheet.Update` остаётся заблокированным для агента.

### `Project.GetSet`

Пользовательский ID применяется к истории списаний проектов, но запросы Incident, Opportunity и Lead выглядят общими. До выдачи полного набора модели необходимо интеграционно подтвердить, что SQL/API слой не раскрывает пользователю недоступные записи.

### `Tag.GetSet`

Функция фильтрует историю по Entra user и Project ID. Adapter не должен позволять модели использовать случайный ID, полученный из prompt; ID должен быть выбран из серверно разрешённого набора проектов либо повторно проверен.

### Размеры результатов

Для `top`, диапазона дат и строк поиска нужны серверные пределы независимо от аргументов модели. Это защищает CRM/SQL, prompt budget и Telegram response size.

### Операции записи

Ни одна write-функция не регистрируется в Kernel до появления:

- immutable prepared action с владельцем и сроком жизни;
- явного подтверждения того же Telegram-пользователя;
- идемпотентного execution key;
- атомарного состояния `Prepared -> Executing -> Succeeded/Failed/Unknown`;
- проверки повторных сообщений и callback;
- аудита безопасных параметров и результата.

## Следующий технический инкремент

Не подключая модель, создать read-only agent application layer в принятой структуре проекта. Начать с одной вертикали `GetTimesheets` либо `SearchProjects`:

1. контракт tool-friendly аргументов без identity;
2. adapter, получающий `AgentUserContext` отдельно;
3. вызов существующей business-функции через `AsyncPipeline`;
4. unit-тесты подстановки доверенного ID, лимитов и преобразования ошибок;
5. никаких новых публичных endpoint и изменений Azure.

После проверки шаблона распространить его на остальные read-only candidates и только затем подключать Semantic Kernel/Azure AI Foundry.

## Реализованный шаблон read adapter

Первая вертикаль реализована для `Timesheet.GetSet` в `src/service/Agent`:

- tool-friendly вход содержит только `DateFrom` и `DateTo`;
- Entra Object ID берётся из доверенного `AgentUserContext`;
- adapter вызывает общую `ITimesheetSetGetFunc`, используемую также HTTP endpoint;
- неправильный порядок дат отклоняется до бизнес-вызова;
- диапазон ограничен option `MaxDateRangeInDays`, по умолчанию 31 день;
- наружу возвращается отдельный минимальный agent DTO;
- orchestration реализована через `AsyncPipeline`.

Adapter зарегистрирован во внутренней dependency composition приложения, но пока не передан Semantic Kernel и не доступен через HTTP.

Вторая вертикаль реализована для `Project.SearchSet`:

- вход содержит поисковый текст и необязательный `top`;
- `CallerObjectId` формируется из доверенного Entra Object ID, а не CRM primary key;
- поисковый текст обрезается по краям, пустое значение отклоняется;
- максимальная длина текста по умолчанию — 100 символов;
- `top` по умолчанию равен 10, допустимый максимум — 20;
- `Forbidden` сохраняется как отдельный безопасный код ошибки;
- результат преобразуется в общий компактный `AgentProjectItem`.

Пределы вынесены в `AgentProjectSetSearchOption` и связаны с секцией `Agent:Tools:Project` в `appsettings.json`.

Третья вертикаль реализована для `Project.GetLastSet`:

- вход содержит только необязательный `top`;
- Entra Object ID подставляется из доверенного `AgentUserContext`;
- `top` по умолчанию равен 10, допустимый максимум — 20;
- результат использует общий `AgentProjectItem` и сохраняет комментарий проекта;
- adapter вызывает общую `ILastProjectSetGetFunc`, используемую HTTP endpoint.

Пределы вынесены в `AgentLastProjectSetGetOption` и связаны с секцией `Agent:Tools:Project` в `appsettings.json`.

Четвёртая вертикаль реализована для `Period.GetSet`:

- tool не принимает пользовательские аргументы;
- вызов разрешён только после формирования `AgentUserContext`, хотя существующей бизнес-функции identity не требуется;
- результат преобразуется в компактные `AgentPeriodItem` с названием и границами периода;
- adapter вызывает общую `IPeriodSetGetFunc`, используемую HTTP endpoint;
- инфраструктурные ошибки преобразуются в безопасный `Unknown`.

Пятая вертикаль реализована для `Tag.GetSet`:

- вход содержит только Project ID;
- Entra Object ID подставляется из доверенного `AgentUserContext`;
- пустой Project ID отклоняется до вызова бизнес-функции;
- количество тегов ограничено `AgentTagSetGetOption.MaxTags`, по умолчанию 20;
- adapter вызывает общую `ITagSetGetFunc`, используемую HTTP endpoint.

Сам adapter подтверждает изоляцию истории тегов по Entra user, но не доказывает, что Project ID был выбран из разрешённого набора. Будущий message orchestration должен передавать сюда ID из результата `SearchProjects`/`GetRecentProjects` либо выполнять отдельную проверку проекта.

Все пять разрешённых read adapters собраны в `App.Agent.Tools.cs`. Их лимиты читаются из общей секции `Agent:Tools`; существующие HTTP endpoint продолжают использовать прежние business-функции и собственные настройки. Semantic Kernel пока не подключён, поэтому эти зависимости ещё не доступны модели.

## Реализованный Semantic Kernel read plugin

В `src/service/Agent/Core/Plugin.Read` добавлен native plugin `TimesheetRead` на Semantic Kernel 1.80.0. Он содержит ровно пять функций из утверждённого allowlist:

1. `get_timesheets`;
2. `search_projects`;
3. `get_recent_projects`;
4. `get_periods`;
5. `get_project_tags`.

Plugin создаётся для одного доверенного `AgentUserContext`; identity не входит в схемы аргументов функций. Результат каждой функции имеет единый безопасный формат `IsSuccess`, `Data`, `ErrorCode`. Текст и исходное исключение инфраструктурной ошибки модели не возвращаются.

`Profile.Get`, `Subscription.GetSet`, уведомления, account flow и write-функции в plugin отсутствуют. Plugin пока не добавлен в рабочий Kernel и не вызывается через HTTP — это следующий отдельный инкремент.

Фабрика `AgentKernelFactory` создаёт отдельный Kernel для переданного `AgentUserContext`, подключает новый Microsoft Foundry project endpoint через OpenAI v1 connector и регистрирует только `TimesheetRead`. Connector использует `ProjectEndpoint`, `ModelId` и `TokenScope`; путь `/openai/v1/` добавляется фабрикой. `AgentFoundryOption` и зарегистрированный на уровне host стандартный `TokenCredential` передаются в фабрику через `Pipeline/Dependency`. API key не используется: локально credential использует `az login`, в Azure — Managed Identity. Создание Kernel не выполняет сетевой запрос; обращение к Foundry начнётся только на этапе message orchestration.

`AgentMessageFunc` выполняет один read-only ход: создаёт Kernel для доверенного `AgentUserContext`, добавляет системную инструкцию с актуальной датой и часовым поясом и вызывает chat completion с автоматическим выбором функций. История пока не загружается и не сохраняется; это намеренно оставлено следующему durable-инкременту. Исключения Foundry наружу не передаются.
