# Перенос логики из БД в код

Правила изменений лежат в `Geonicle.Data/Rules/`, один класс на правило. Новое правило — новый класс (наследник `ChangeRule<TEntity>`), регистрировать нигде не нужно: `ChangeRuleRunner` находит классы сам.

- `State` — когда срабатывает (`Added`, `Modified`, `Deleted`).
- `Phase` — `BeforeSave` (до записи, сущности уходят в тот же запрос) или `AfterSave` (после первой записи, в той же транзакции; нужны ключи, созданные БД).
- `ReplacesTrigger` — имя триггера БД, который заменяет правило. Пока триггер есть в базе, правило не выполняется (иначе действие сделается дважды), после удаления триггера включается само. Наличие триггера проверяется по `sys.triggers` один раз за время работы приложения.

Не переносим: метки `DateCreated`/`DateModified` (остаются в БД, чтобы не писать время клиентских компьютеров) и тяжёлые расчётные процедуры (пока остаются в БД).

DDL (удаление триггеров и процедур) выполняет пользователь и обновляет скрипты в `gdb/`.

## Триггеры

| Триггер | Таблица | Что делает | Статус | Правило |
|---|---|---|---|---|
| AfterInsertHoles | Boreholes | замер 0/0/0 в Survey | перенесено, ждёт удаления триггера в БД | `BoreholeCreatedRule` |
| AfterInsertHoles | Boreholes | запись `ChangedRecords('Survey', BoreholeId)` | перенесено, ждёт удаления триггера в БД | `BoreholeSurveyChangedRecordRule` |
| AfterInsertHoles | Boreholes | строка в LineHoles (LineId = 0) | не переносится (решение пользователя) | — |
| AfterUpdateHoles | Boreholes | `DateModified` | остаётся в БД | — |
| AfterUpdateHoles | Boreholes | `ChangedRecords` при смене X, Y, Z, Depth | не начато | — |
| остальные AfterInsert*/AfterUpdate*/AfterDelete*/InsteadOf* | разные | см. `gdb/GDB52.sql` | не начато (метки времени остаются в БД) | — |

## Процедуры

Не начато. Расчётные (`Reserves*`, `CalcBlock*`, `CalcComposites`) остаются в БД. Остальные (импорт `Import.*`, `Create*`, `Delete*`) — по очереди, в папке `Geonicle.Data/Operations/`.
