# Database migrations

Schema changes are applied with SQL scripts in this folder (the repo does not use EF Core migrations yet).

## Apply migration

From repo root, using `sqlcmd`:

```powershell
sqlcmd -S "(local)" -d cinema_db -U sa -P "YOUR_PASSWORD" -i "scripts/migrations/20260630_pricing_rules_int_ids.up.sql" -C
```

Or open the `.up.sql` file in SSMS / Azure Data Studio and execute it against your database.

## Rollback

```powershell
sqlcmd -S "(local)" -d cinema_db -U sa -P "YOUR_PASSWORD" -i "scripts/migrations/20260630_pricing_rules_int_ids.down.sql" -C
```

## 20260630 — PricingRule int IDs

| Before | After |
|--------|-------|
| `room_type` nvarchar(20) | `room_type_id` int (1=Standard, 2=VIP, 3=IMAX, 4=4DX) |
| `time_slot` nvarchar(20) | `time_slot_id` int (1=Normal, 2=Peak) |

Legacy string values are converted automatically before old columns are dropped. The script also removes legacy check constraints `CK_PR_ROOM_TYPE` / `CK_PR_TIME_SLOT` and adds int-based constraints.

**Note:** Run with `sqlcmd` (includes `GO` batch separators). SSMS works as well.

After migration, verify:

```sql
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'PRICING_RULES'
ORDER BY ORDINAL_POSITION;
```

Expected columns include `room_type_id` and `time_slot_id` (int), not `room_type` / `time_slot`.
