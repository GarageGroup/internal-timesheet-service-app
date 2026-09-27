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

## Классификация

| Статус | Значение |
|---|---|
| `Read candidate` | Можно готовить adapter/plugin после проверки входного контекста и лимитов результата |
| `Write blocked` | Не подключать до реализации подтверждений, идемпотентности и проверки прав |
| `System only` | Служебная операция, модель не должна выбирать её как tool |
| `Diagnostic` | Временная операция для проверки и демонстрации контура |

## Матрица существующих операций

| Модуль / функция | Назначение | Пользовательские аргументы | Доверенный контекст | Тип | Решение для агента |
|---|---|---|---|---|---|
| `Agent.Profile.Get / IAgentProfileGetFunc` | Проверка Telegram-привязки и получение профиля | Нет | Bot ID, Telegram user/chat | Read | `Diagnostic`; сохранить до отдельного указания |
| `Profile.Get / IProfileGetFunc` | Имя и язык профиля пользователя текущего бота | Нет | Entra Object ID | Read | `Read candidate`; использовать через agent adapter |
| `Period.GetSet / IPeriodSetGetFunc` | Доступные периоды списания | Нет | Не требуется | Read | `Read candidate` |
| `Project.GetLastSet / ILastProjectSetGetFunc` | Последние проекты пользователя | Необязательный `top` | Entra Object ID | Read | `Read candidate`; ограничить `top` на сервере |
| `Project.GetSet / IProjectSetGetFunc` | Общий набор активных проектов с пользовательской историей | Нет | Entra Object ID | Read | `Read candidate`; дополнительно проверить видимость четырёх типов проектов |
| `Project.SearchSet / IProjectSetSearchFunc` | Поиск project/incident/opportunity/lead | Строка поиска, необязательный `top` | CRM System User ID как `CallerObjectId` | Read | `Read candidate`; ограничить длину строки и `top` |
| `Timesheet.GetSet / ITimesheetSetGetFunc` | Списания пользователя за диапазон дат | `dateFrom`, `dateTo` | Entra Object ID | Read | `Read candidate`; ограничить диапазон дат и размер результата |
| `Tag.GetSet / ITagSetGetFunc` | Хэштеги пользователя по выбранному проекту | Project ID | Entra Object ID | Read | `Read candidate`; Project ID должен происходить из разрешённого результата поиска/выбора |
| `Subscription.GetSet / ISubscriptionSetGetFunc` | Настройки уведомлений пользователя | Нет | CRM System User ID | Read | `Read candidate`, но не нужен для первого timesheet-сценария |
| `Timesheet.Modify / ITimesheetCreateFunc` | Создание списания | Дата, проект, длительность, комментарий | CRM System User ID как `CallerObjectId` | Write | `Write blocked` |
| `Timesheet.Modify / ITimesheetUpdateFunc` | Изменение списания | Timesheet ID и изменяемые поля | CRM System User ID | Write | `Write blocked`; update-запрос требует дополнительного аудита impersonation/ownership |
| `Timesheet.Delete / ITimesheetDeleteFunc` | Удаление списания | Timesheet ID | CRM System User ID как `CallerObjectId` | Write | `Write blocked` |
| `Profile.Update / IProfileUpdateFunc` | Изменение языка профиля | Language code | CRM System User ID | Write | `Write blocked`; низкий приоритет |
| `Notification.Subscribe / INotificationSubscribeFunc` | Настройка ежедневных/еженедельных уведомлений | Тип и параметры уведомления | CRM System User ID | Write | `Write blocked`; низкий приоритет |
| `Notification.Subscribe / INotificationUnsubscribeFunc` | Отключение уведомлений | Тип уведомления | CRM System User ID | Write | `Write blocked`; низкий приоритет |
| `User.SignIn / IUserSignInFunc` | Создание Telegram/CRM-привязки по подписанному Mini App `initData` | Telegram `initData` | Интерактивный Entra user | Write | `System only`; никогда не вызывать моделью |
| `User.SignOut / IUserSignOutFunc` | Отзыв Telegram-привязки | Нет | Интерактивный Entra user | Write | `System only`; отдельный явный account flow, не обычный tool |

## Первая read-only группа

Первый безопасный набор native plugins предлагается ограничить следующими возможностями:

1. `GetProfile` — получить язык и отображаемое имя пользователя.
2. `GetPeriods` — получить допустимые периоды.
3. `GetRecentProjects` — показать последние проекты пользователя.
4. `SearchProjects` — найти проекты по тексту с серверным пределом результата.
5. `GetTimesheets` — показать списания пользователя за ограниченный диапазон.
6. `GetProjectTags` — получить подсказки тегов для уже выбранного проекта.

`Project.GetSet` можно добавить после проверки фактической видимости данных для Project, Incident, Opportunity и Lead. `Subscription.GetSet` безопаснее операций записи, но не требуется для первого пользовательского сценария и увеличивает поверхность tools.

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
