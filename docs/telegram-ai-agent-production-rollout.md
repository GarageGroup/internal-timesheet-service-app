# Telegram AI Agent: production rollout runbook

Последнее обновление: 30.09.2026.

## Назначение документа

Этот runbook перечисляет изменения вне кода, необходимые для переноса Telegram AI Agent в production.
Он дополняется по мере развития решения. Значения test-среды приведены только как ориентиры; test GUID,
resource IDs, URL, сертификаты и credentials нельзя копировать в production.

## 1. Обязательные решения до начала

- согласовать владельца и бюджет Foundry deployment;
- определить требования data residency для CRM-текста;
- выбрать `GlobalStandard`, `DataZoneStandard` либо regional deployment;
- подтвердить production-модель и квоту;
- определить production-наименования App Registration, APIM API, Foundry resource/project и таблицы;
- подтвердить сроки хранения истории диалога;
- назначить исполнителя с правами создавать App Registration и app role assignments;
- назначить исполнителя с `Microsoft.Authorization/roleAssignments/write` на нужных Azure scopes;
- подтвердить, что production API продолжает требовать backend client certificate;
- подготовить план отката и окно наблюдения.

## 2. Инвентаризация production

До изменений заполнить таблицу:

| Параметр | Production-значение |
|---|---|
| Subscription ID API/бота | `<prod-subscription-id>` |
| Resource Group API/бота | `<prod-resource-group>` |
| Function App Telegram-бота | `<prod-bot-function-app>` |
| App Service Timesheet API | `<prod-timesheet-api-app>` |
| Managed Identity API | `<prod-api-managed-identity>` |
| Storage Account | `<prod-storage-account>` |
| APIM subscription/RG/service | `<prod-apim-location>` |
| Backend certificate entity | `<prod-certificate-entity>` |
| Entra tenant ID | `<prod-tenant-id>` |
| Foundry resource/project | `<prod-foundry-resource>` / `<prod-foundry-project>` |
| Foundry model/deployment | `<prod-model>` / `<prod-deployment>` |
| Telegram Bot ID | `<prod-telegram-bot-id>` |

Проверить, что bot Function App и API не используют одну и ту же identity для входящего agent-вызова.

## 3. Managed Identity Telegram-бота

1. Включить system-assigned Managed Identity у production Function App Telegram-бота.
2. Сохранить её:
   - client/application ID;
   - principal/object ID.
3. Не удалять существующую user-assigned identity, если она нужна старому коду.
4. В клиенте agent API явно использовать system-assigned identity. Если задан `AZURE_CLIENT_ID`,
   убедиться, что он не заставляет бот получать agent token от общей identity.

Причина отдельной identity: API должен однозначно распознавать Telegram-бот по `appid`/`azp`.

Проверка: получить app-only token для production audience и убедиться, что его client claim соответствует
system-assigned identity бота.

## 4. App Registration agent API

Создать отдельную production App Registration, например `api-timesheet-agent-prod`.

Настроить:

- single-tenant приложение;
- Application (client) ID — сохранить как production audience basis;
- Identifier URI: `api://<application-client-id>`;
- application role:
  - display name/value: `Timesheet.Agent.Invoke`;
  - allowed member types: `Applications`;
  - enabled: `true`;
  - новый production role ID.

Не создавать client secret для бота: он использует Managed Identity.

Назначить app role `Timesheet.Agent.Invoke` service principal system-assigned Managed Identity бота.
Это Entra app role assignment, а не Azure RBAC role assignment.

Проверка токена:

- `aud` равен production Identifier URI;
- `roles` содержит `Timesheet.Agent.Invoke`;
- `appid` или `azp` соответствует client ID production MI бота;
- `tid` соответствует production tenant;
- issuer является tenant-specific v1 или v2.

## 5. Конфигурация Timesheet API: входящая авторизация

Добавить в production App Service:

| App Setting | Значение |
|---|---|
| `Agent__Enabled` | сначала `false` |
| `Agent__Authentication__TenantId` | production Entra tenant ID |
| `Agent__Authentication__Audience` | `api://<production-app-client-id>` |
| `Agent__Authentication__RequiredRole` | `Timesheet.Agent.Invoke` |
| `Agent__Authentication__Clients__0__ClientId` | client ID system-assigned MI бота |
| `Agent__Authentication__Clients__0__BotId` | production Telegram Bot ID |

Использовать массив `Clients`, а не GUID как сегмент dictionary key: App Service может отклонить имя
настройки с GUID-сегментом.

До завершения остальных шагов `Agent__Enabled` должен оставаться `false`.

## 6. Foundry resource, project и deployment

1. Создать отдельные production Foundry resource и project либо использовать одобренный корпоративный
   resource с изоляцией и понятным владельцем.
2. Выбрать регион, deployment type и модель после проверки:
   - доступности модели;
   - subscription quota именно для модели/региона/type;
   - требований data residency;
   - ожидаемых TPM/RPM и бюджета.
3. Создать deployment с production-именем.
4. Для начального пилота не резервировать Provisioned capacity без отдельного обоснования.
5. Не использовать API key.

Test reference: `gpt-5-mini` `2025-08-07`, `GlobalStandard`, 10K TPM, `West Europe`. Это не
автоматическая production-рекомендация: `GlobalStandard` допускает глобальную обработку inference.

Назначить Managed Identity production API роль `Cognitive Services User` на scope production Foundry
resource. Роль должна получить identity API, а не бот.

Добавить в production API:

| App Setting | Значение |
|---|---|
| `Agent__Foundry__ProjectEndpoint` | `https://<resource>.services.ai.azure.com/api/projects/<project>` |
| `Agent__Foundry__ModelId` | точное имя deployment |
| `Agent__Foundry__TokenScope` | `https://ai.azure.com/.default` |

Если API использует user-assigned MI, `AZURE_CLIENT_ID` должен содержать её client ID.

## 7. Azure Table для истории диалога

В одобренном production Storage Account создать таблицу:

```text
TimesheetAgentConversation
TimesheetAgentAction
```

Приложение намеренно не создаёт таблицу автоматически.

Назначить Managed Identity production API роль `Storage Table Data Contributor` на минимально
допустимом scope. Предпочтителен scope конкретного Storage Account; при наличии поддерживаемой
table-level RBAC и организационного стандарта можно сузить scope дополнительно.

Не передавать приложению storage account key или connection string.

Добавить в production API:

| App Setting | Значение |
|---|---|
| `Agent__Storage__TableServiceEndpoint` | `https://<storage>.table.core.windows.net/` |
| `Agent__Storage__ConversationTableName` | `TimesheetAgentConversation` |
| `Agent__Storage__ActionTableName` | `TimesheetAgentAction` |
| `Agent__WritePreparation__Enabled` | сначала `false`; включать только после проверки таблицы и preview flow |
| `Agent__WritePreparation__ApprovalTtlMinutes` | `10` либо согласованное значение |
| `Agent__Message__MaxHistoryMessageCount` | согласованный лимит, test default `20` |
| `Agent__Message__MaxTextLength` | согласованный лимит, test default `2000` |
| `Agent__Message__TimeZoneId` | production business timezone |

Согласовать lifecycle/retention и процедуру удаления истории и завершённых action. История диалога и
текущее состояние Action Table не заменяют согласованный неизменяемый аудит write-операций.

## 8. APIM agent API

Создать отдельный production APIM API для server-to-server agent traffic. Не ослаблять существующую
Mini App policy.

Рекомендуемая конфигурация:

| Параметр | Значение |
|---|---|
| API ID | `<prod-timesheet-agent-api>` |
| Public path | `timesheet-agent` либо production-стандарт |
| Backend | production Timesheet API App Service |
| Subscription required | `false`, если защита обеспечивается MI JWT и принятым APIM perimeter |
| Operation | `POST /internal/agent/messages` |

API-level policy должна:

- направлять запрос в production backend;
- предъявлять production client certificate entity, требуемую App Service;
- сохранять исходный `Authorization: Bearer` header бота;
- не подменять пользовательские/Telegram identifiers;
- применять стандартные ограничения размера, timeout и журналирование без payload/secrets.

Для callback подтверждения создать вторую operation в том же защищённом agent API:

| Параметр | Значение |
|---|---|
| Method | `POST` |
| URL template | `/internal/agent/actions/{actionId}/decision` |
| Authentication/policy | та же app-only policy и backend certificate, что у message operation |

Не добавлять decision route в Mini App API и не ослаблять общую policy. До готовности Telegram-кнопок и smoke-теста оставлять `Agent__WritePreparation__Enabled=false`.

Проверить:

- запрос через APIM с корректным MI token проходит;
- без токена API возвращает 401;
- с валидным токеном без app role — 403;
- токен неизвестного client ID — 403;
- прямой backend-вызов без client certificate отклоняется;
- Mini App API продолжает работать по старой policy.

## 9. Конфигурация Telegram-бота

Добавить в production Function App:

| App Setting | Значение |
|---|---|
| `AgentApi__BaseAddress` | base URL production agent API в APIM, с завершающим `/` |
| `AgentApi__Audience` | production Identifier URI App Registration |

Проверить, что webhook production-бота продолжает указывать на production ingress и защищён текущим
секретом/function key. Не выводить token, webhook secret или function key в deployment logs.

Развернуть код штатным CI/CD. Прямой ZIP deployment использовать только как явно документированную
аварийную/тестовую процедуру.

После deployment дождаться завершения recycle и старта Durable Task worker до отправки сообщений.
Быстрые последовательные ZIP deployments могут временно оставить Durable control message невидимым;
не очищать очереди для исправления этого состояния.

## 10. Остальные настройки agent-модуля

Проверить и при необходимости явно задать:

| App Setting | Назначение |
|---|---|
| `Agent__Tools__Timesheet__MaxDateRangeInDays` | максимальный диапазон чтения timesheet |
| `Agent__Tools__Project__DefaultTop` | стандартное число проектов |
| `Agent__Tools__Project__MaxTop` | верхний лимит проектов |
| `Agent__Tools__Project__MaxSearchTextLength` | лимит поискового текста |
| `Agent__Tools__Tag__MaxTags` | максимальное число тегов |
| `Agent__Voice__Enabled` | отдельное включение голосового ввода; при первом deployment оставить `false` |
| `Agent__Voice__Endpoint` | Azure OpenAI endpoint вида `https://<resource>.openai.azure.com/` |
| `Agent__Voice__DeploymentName` | имя отдельного audio-to-text deployment |
| `Agent__Voice__ModelId` | идентификатор модели, проверенный с Semantic Kernel connector |
| `Agent__Voice__MaxFileSizeBytes` | максимальный размер аудио до отправки модели |
| `AgentVoice__MaxFileSizeBytes` (bot Function App) | такой же или меньший предел потокового скачивания Telegram voice |

Значения должны соответствовать production-нагрузке и бизнес-ограничениям, а не автоматически
копироваться из test.

## 11. Порядок развёртывания

1. Создать MI/App Registration/app role и назначить роль боту.
2. Создать Foundry resource/project/chat deployment и отдельный audio-to-text deployment; проверить quota обоих.
3. Создать Azure Table и назначить API две Azure RBAC-роли.
4. Добавить API settings с `Agent__Enabled=false`.
5. Создать APIM API, operations и policy.
6. Добавить bot settings.
7. Развернуть API.
8. Проверить health и отсутствие регрессии Mini App.
9. Установить для Telegram Function App поддерживаемый `.NET 10 isolated` runtime stack и развернуть `net10.0` Telegram-бота. Изменение stack и artifact выполнять согласованно, с заранее подготовленным откатом на предыдущие stack и artifact.
10. Проверить 401/403 на agent message и decision routes; отдельный диагностический profile route не создавать.
11. Установить `Agent__Enabled=true` и перезапустить API.
12. Выполнить read-only end-to-end тест обычным Telegram-сообщением.
13. Включить `Agent__WritePreparation__Enabled=true` только после проверки Action Table и decision route.
14. Выполнить create/update/delete Cancel и Confirm smoke matrix на контролируемых пилотных данных.
15. Наблюдать логи, latency, 429, `Indeterminate`, токены и стоимость.
16. После отдельного voice smoke test установить `Agent__Voice__Enabled=true`; проверить read-only голос и голосовой write preview + Cancel до первого Confirm.

## 12. Acceptance checklist

- [ ] Production GUID/URL не совпадают с test.
- [ ] У бота отдельная system-assigned MI.
- [ ] Function App бота использует поддерживаемый `.NET 10 isolated` runtime, а deployed artifact собран для `net10.0` на Azure Functions Worker SDK 2.x.
- [ ] App role выдана только разрешённым workload identities.
- [ ] API проверяет tenant, audience, issuer, role и client allowlist.
- [ ] Telegram `BotId` выводится из доверенного client mapping.
- [ ] Telegram/CRM binding разрешается сервером.
- [ ] API key и storage key отсутствуют.
- [ ] API MI имеет только `Cognitive Services User` и `Storage Table Data Contributor` на нужных scopes.
- [ ] Исходящие Telegram Bot API URL редактируются или не записываются в telemetry; bot token отсутствует в traces, requests и exceptions.
- [ ] После проверки telemetry создан новый production bot token, а использовавшиеся при проверках токены отозваны.
- [ ] APIM предъявляет backend certificate и сохраняет Bearer token.
- [ ] Mini App policy не ослаблена.
- [ ] Foundry deployment доступен и имеет квоту.
- [ ] Audio-to-text deployment доступен, API MI имеет к нему доступ, а Voice feature включается независимо от текста.
- [ ] Исходное аудио не сохраняется в Conversation/Action Table и не попадает в telemetry.
- [ ] Effective APIM `forward-request` timeout покрывает cold-start цепочку voice transcription и agent/tools; проверен первый запрос после restart.
- [ ] Data residency согласована.
- [ ] Table создана и доступна API MI.
- [ ] Запрос без токена = 401.
- [ ] Неправильная роль/client = 403.
- [ ] Read-only Telegram question возвращает корректный ответ.
- [ ] Create/update/delete показывают preview и до Confirm не изменяют Dataverse.
- [ ] Cancel каждой write-операции не изменяет Dataverse и удаляет клавиатуру.
- [ ] Confirm каждой write-операции выполняет сохранённый payload только один раз.
- [ ] Voice read-only, preview, Cancel и Confirm пройдены сквозным Telegram smoke test.
- [ ] Настроен операторский разбор `Executing`/`Indeterminate` без автоматического повторения CRM write.
- [ ] Бизнес-правило timezone согласовано с Dataverse validation.
- [ ] Ошибки не раскрывают credentials, CRM payload или stack trace.
- [ ] Настроены Application Insights alerts и cost monitoring.
- [ ] Записаны фактические production resource IDs и ответственные.

## 13. Откат

Быстрый функциональный откат:

1. установить `Agent__Enabled=false`;
2. при необходимости удалить/disable APIM agent operations;
3. развернуть предыдущую версию бота;
4. Mini App flow оставить включённым;
5. не удалять таблицу до решения о сохранении истории и аудита.

Полный инфраструктурный откат выполнять только после подтверждения отсутствия зависимостей:

- снять app role assignment с MI бота;
- снять Foundry/Storage RBAC с MI API;
- удалить deployment/project/resource, если они выделенные и данные/стоимость учтены;
- удалить пустую таблицу после retention review;
- удалить App Registration последней, после проверки отсутствия consumers.

Каждое production-изменение записывать в отдельный change record: дата, исполнитель, исходное и новое
состояние, resource ID, причина, проверка, rollback и связанный deployment/commit.

## 14. Перед включением write-операций

В test реализованы и сквозно проверены create/update/delete preview, Confirm/Cancel, owner validation,
TTL, ETag-переходы и запрет повторного исполнения. Для production остаётся:

- согласовать бизнес-правило timezone между агентом и Dataverse;
- определить операторскую reconciliation-процедуру для `Executing`/`Indeterminate`;
- согласовать retention и неизменяемый аудит команды, подтверждения и результата;
- подтвердить лимиты часов, диапазона дат и доступных проектов;
- решить, нужна ли очистка комментария через расширение `Timesheet.Update`;
- провести production security review и выполнить полную Cancel/Confirm smoke matrix.

## Связанные документы

- [обзор для руководителя](telegram-ai-agent-management-overview.md);
- [план реализации](telegram-ai-agent-implementation-plan.md);
- [журнал прогресса](telegram-ai-agent-progress.md);
- [журнал Azure](telegram-ai-agent-azure-change-log.md);
- [каталог операций](telegram-ai-agent-operation-catalog.md).
