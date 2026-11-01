# Перенос логики из БД в код

Триггеры БД переносятся в триггеры библиотеки [EntityFrameworkCore.Triggered](https://www.nuget.org/packages/autoguru.entityframeworkcore.triggered) (форк AutoGuru 5.0.0, net10 / EF Core 10). Они лежат в `Geonicle.Data/Triggers/`, один класс на триггер. Новый триггер — новый класс, реализующий `IBeforeSaveTrigger<T>` (до записи, изменения уходят в тот же запрос) и/или `IAfterSaveTrigger<T>` (после записи, известны ключи, созданные БД); регистрировать нигде не нужно: `AddGeonicleData` находит классы сам.

- В `ITriggerContext<T>` есть `ChangeType` (`Added`, `Modified`, `Deleted`), `Entity` и `UnmodifiedEntity` (значения до правки).
- Триггер получает зависимости через конструктор; контекст в нём — тот же, что сохраняет (сервис экрана создаёт контекст в отдельной области DI, `EditableDataService`).
- Изменения, сделанные в триггере «после сохранения», библиотека не записывает: триггер сам вызывает `await db.SaveChangesAsync(...)` (образец: `BoreholeSurveyChangedRecordTrigger`).
- `EditableDataService.SaveAsync` оборачивает сохранение в транзакцию, поэтому запись и записи триггеров «после сохранения» фиксируются вместе. При прямом `db.SaveChangesAsync` без транзакции эта атомарность не гарантируется.
- Триггер выполняется всегда; триггер, который он заменяет, пользователь удаляет в БД сам (в комментарии класса указано, какой).

Не переносим: метки `DateCreated`/`DateModified` (остаются в БД, чтобы не писать время клиентских компьютеров) и тяжёлые расчётные процедуры (пока остаются в БД).

DDL (удаление триггеров и процедур) выполняет пользователь и обновляет скрипты в `gdb/`.

## Триггеры

| Триггер БД | Таблица | Что делает | Статус | Триггер в коде |
|---|---|---|---|---|
| AfterInsertHoles | Boreholes | замер 0/0/0 в Survey | перенесено (триггер в БД удалён пользователем); замер добавляется сразу при создании строки в гриде (`BoreholesService`), правило страхует при сохранении | `BoreholeCreatedTrigger` |
| AfterInsertHoles | Boreholes | запись `ChangedRecords('Survey', BoreholeId)` | перенесено (триггер в БД удалён пользователем) | `BoreholeSurveyChangedRecordTrigger` |
| AfterInsertHoles | Boreholes | строка в LineHoles (LineId = 0) | не переносится (решение пользователя) | — |
| AfterUpdateHoles | Boreholes | `DateModified` | остаётся в БД | — |
| AfterUpdateHoles | Boreholes | `ChangedRecords` при смене X, Y, Z, Depth | не начато | — |
| остальные AfterInsert*/AfterUpdate*/AfterDelete*/InsteadOf* | разные | см. `gdb/GDB52.sql` | не начато (метки времени остаются в БД) | — |

## Процедуры

Не начато. Расчётные (`Reserves*`, `CalcBlock*`, `CalcComposites`) остаются в БД. Остальные (импорт `Import.*`, `Create*`, `Delete*`) — по очереди, в папке `Geonicle.Data/Operations/`.
