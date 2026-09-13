# Tag screen mapping (Excel → DB → Realtime / History)

## Analysis (before change)

| Layer | Existing | Missing | Change |
| ----- | -------- | ------- | ------ |
| Entity | `Station`, `Plc`, `Device`, `Tag`, `TagHistoryConfig`, `History1s/30s/1m/30m` | Screen mapping | Add `TagScreenMapping` |
| DB | `scada.tag` (unique `code`) | `scada.tag_screen_mapping` | Migration |
| Redis | Concurrent sessions + `ICacheService` (memory) | Last-value realtime store | `IRealtimeDataStore` Fake / Redis |
| SignalR | None | Hub + groups | `ScadaRealtimeHub` |
| API | Station schematic/device-monitor via **catalog aliases** | Snapshot by screen | `GET /api/v1/screens/{screen}/snapshot` |
| History | `GET /api/v1/history/1s\|1m\|30m` | Filter by screen mapping | `GET /api/v1/screens/{screen}/history` |
| FE | (not changed — BE only) | Subscribe hub | Contract documented |

Excel is **not** a runtime DB. Seed writes mapping into Postgres.

## Excel mapping rules (actual workbook)

Realtime columns mix `"Có"` and display labels. Counts in the file:

| Screen | Rule | Count |
| ------ | ---- | ----- |
| Nguyên lý | nonempty | 48 |
| Công nghệ | nonempty | 51 |
| Chi tiết bơm | nonempty | 232 |
| Lỗi | `Có` | 50 |
| Trend | nonempty label | 163 |
| Báo cáo | nonempty label | 173 |

`MappingLabel` stores the cell text.

## APIs

| Method | Route | Query |
| ------ | ----- | ----- |
| GET | `/api/v1/screens/{screen}/snapshot` | `stationId` required; `deviceId` **required** for `chi-tiet-bom` |
| GET | `/api/v1/screens/{screen}/history` | `stationId`, `from`, `to`, optional `deviceId`, `interval=1s\|30s\|1m\|30m` |

`{screen}`: `nguyen-ly` \| `cong-nghe` \| `chi-tiet-bom` \| `loi` \| `trend` \| `bao-cao`

Snapshot: tags from `tag_screen_mapping` + current value from `IRealtimeDataStore` (Fake by default).

History: same tag set, samples from Timescale `history.history_*` — **not Redis**.

Existing report APIs (`stations/{id}/reports/...`) unchanged.

## SignalR

- Hub: `/hubs/scada`
- Client methods: `Subscribe(stationId, screen, deviceId?)`, `Unsubscribe(...)`
- Event: `TagChanged` → `RealtimeChangedMessage` (`tagId`, `deviceId`, `stationId`, `tagCode`, `value`, `dataType`, `timestamp`)
- Groups: `station:{id}:screen:{slug}` and `station:{id}:screen:{slug}:device:{deviceId}`

FE flow: GET snapshot → Subscribe.

## Redis switch

```json
"Realtime": { "Provider": "Fake" }
```

`"Provider": "Redis"` uses `RedisRealtimeDataStore` (keys `{RedisKeyPrefix}:{tagId}`). No FE/business change.

## Seed

On API startup (Development/configured): import `data/Dinh_Nghia_Tag_Thuy_Loi_Ha_Noi.xlsx`.

Match: Station (TBAB → `TB01` / Trạm Ấp Bắc) + PLC + Device + Tag code. Insert missing devices/tags. Upsert mappings. Idempotent.
