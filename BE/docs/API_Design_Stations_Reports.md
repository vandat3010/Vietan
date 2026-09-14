# API Design — SCADA Trạm bơm (Stations & Reports)

Tài liệu thiết kế + mô tả API cho FE/BE.  
Base URL (dev): `http://localhost:5140`  
Prefix: `/api/v1`  
Controller chính: `StationsController` → `/api/v1/stations`

---

## 0. NHỚ — History 1s khi data lớn

> **Quyết định kiến trúc (team):**  
> Hiện `history.history_1s` / báo cáo vẫn có thể dùng **EF** khi volume nhỏ (dev/demo).  
> **Khi dữ liệu 1s quá nhiều** (báo cáo, pivot, scan theo khoảng thời gian lớn):  
> **không dùng EF nữa** → chuyển sang **Dapper + Stored Procedure** (read-side).  
> Giữ EF cho metadata SCADA (station/plc/device/tag) và write-side.  
> Pattern sẵn trong solution: `IDapperRepository` / `IReportService` / `IUserReportQueries`.

Checklist khi migrate:
1. Viết SP (Postgres function) nhận `station_id`, `from`, `to`, `device_id`, phân trang.
2. Gọi qua Dapper → map thẳng DTO báo cáo (không `DbSet<History1s>`).
3. Giữ contract API FE không đổi (cùng path/query/response).

---

## 1. Nguyên tắc thiết kế (API Design)

### 1.1 Envelope response
Mọi API JSON bọc trong `ApiResponse<T>`:

```json
{
  "success": true,
  "message": "...",
  "data": { },
  "errors": null,
  "traceId": null,
  "timestampUtc": "..."
}
```

| HTTP | Khi nào |
|------|---------|
| 200 | Thành công |
| 400 | Query không hợp lệ (khoảng thời gian…) |
| 404 | Không tìm thấy station / resource |
| 500 | Lỗi không mong đợi |

### 1.2 Phân trang
Query kế thừa `PaginationRequest`:

| Param | Mặc định | Ghi chú |
|-------|----------|---------|
| `pageNumber` | 1 | Bắt đầu từ 1 |
| `pageSize` | 20 | Max 100 |

`data` kiểu `PaginationResult<T>`:

```json
{
  "items": [ ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

FE footer: `Tổng: {totalCount} bản ghi` · `Trang {pageNumber}/{totalPages}`.

### 1.3 Domain join (Id)
```
scada.station → scada.plc → scada.device → scada.tag → history.* (tag_id)
```
Không dump metadata thừa — response theo **field màn hình UI**.

### 1.4 Thời gian báo cáo
- Lọc **trong 1 ngày**: `reportDate` + `startTime` + `endTime` (giờ VN +07).
- Mặc định: hôm nay, `00:00:00` → `23:59:59`.
- DB lưu/query **UTC**; FE hiển thị local.

### 1.5 Dropdown / Combobox
Chỉ trả `{ id, name }` — không trả kind/key/code thừa.

### 1.6 Controller style
Action thin: `=> this.ToActionResult(await service..., message)`.  
Lỗi nghiệp vụ / exception → `Result` → `ApiResponse` (không throw lên FE).

### 1.7 Auth
Hiện **JWT đang bật**. Mọi endpoint SCADA yêu cầu Bearer (trừ Auth anonymous).

---

## 2. Bản đồ màn hình ↔ API

| Màn FE | API |
|--------|-----|
| Chi tiết / header trạm | `GET /stations/{id}` |
| Devices (card bơm realtime) | `GET /stations/{id}/device-monitor` |
| Thông số điện (màn riêng) | `GET /stations/{id}/electrical` |
| Sơ đồ nguyên lý | `GET /stations/{id}/schematic` |
| Báo cáo lịch sử — dropdown Thiết bị | `GET /stations/{id}/reports/devices` |
| Báo cáo — Mức nước | `GET /stations/{id}/reports/water-levels` |
| Báo cáo — Nhiệt độ bơm | `GET /stations/{id}/reports/pump-temperatures` |

**Không dùng** `alarm_history` cho màn mức nước / nhiệt độ (alarm là màn lỗi riêng: `/api/v1/alarm-histories`).

---

## 3. Chi tiết API — Stations

### 3.1 Danh sách trạm
```
GET /api/v1/stations?pageNumber=1&pageSize=20&isActive=true&keyword=
```
**Response `data.items[]`:** `id`, `code`, `name`, `isActive`

### 3.2 Chi tiết trạm
```
GET /api/v1/stations/{id}
```
**Response:** `id`, `code`, `name`, `address`, `latitude`, `longitude`, `description`, `isActive`

### 3.3 Device-monitor (màn Devices)
```
GET /api/v1/stations/{stationId}/device-monitor?deviceType=Pump&deviceId=&isEnable=
```
Nguồn realtime: `history.history_1s` (latest).  
Station chi tiết: gọi riêng `GET /stations/{id}`.

**Response `data.items[]` (đủ field card, không thừa):**

| Field | UI |
|-------|-----|
| `deviceId`, `name`, `ratedPowerKw` | Header "Bơm N - 160kW" |
| `status` | running / stopped / error / … |
| `windingTempActual` `{a,b,c}` | Nhiệt cuộn thực tế |
| `windingTempAllowed` `{a,b,c}` | Nhiệt cuộn cho phép |
| `bearingTop` / `bearingBottom` `{actual,allowed}` | Ổ bi |
| `waterLevel` `{river,basin}` | Mực nước |
| `runtime` `{instantH,totalH}` | Thời gian chạy |
| `electrical` | 8 thông số điện (object phẳng) |

**JSON mẫu:**
```json
{
  "success": true,
  "message": "Danh sách bơm màn Devices (đủ thông số UI).",
  "data": {
    "items": [{
      "deviceId": 1,
      "name": "Bơm 1",
      "ratedPowerKw": 160,
      "status": "running",
      "windingTempActual": { "a": 31, "b": 32, "c": 33 },
      "windingTempAllowed": { "a": 80, "b": 80, "c": 80 },
      "bearingTop": { "actual": 45.2, "allowed": 80 },
      "bearingBottom": { "actual": 43.7, "allowed": 70 },
      "waterLevel": { "river": 2.15, "basin": 1.42 },
      "runtime": { "instantH": 3.33, "totalH": 348.83 },
      "electrical": {
        "voltageRs": 381.2, "voltageSt": 382.7, "voltageTr": 380.5,
        "currentA": 48.5, "powerFactor": 0.92, "frequencyHz": 50.01,
        "powerKw": 95.4, "energyKwh": 5715.2
      }
    }]
  }
}
```

### 3.4 Electrical / Schematic / Device-cards
| Path | Mục đích |
|------|----------|
| `.../electrical` | Chỉ thông số điện theo trạm |
| `.../schematic` | Sơ đồ nguyên lý (MCCB, status…) |
| `.../device-cards` | Debug/full: plc + device + toàn bộ tag + history |

---

## 4. Chi tiết API — Báo cáo lịch sử (history_30s)

Nguồn dữ liệu: **`history.history_30s`** (`time`, `tag_id`, `value`) + catalog `scada.tag`.  
Sắp xếp: **`time DESC`**. Có phân trang.

### 4.1 Dropdown Thiết bị
```
GET /api/v1/stations/{stationId}/reports/devices
```

**Response `data`:** mảng `{ id, name }`

| id | name | FE gọi tiếp |
|----|------|-------------|
| `0` | Mức nước | `/reports/water-levels` |
| `>0` | Nhiệt độ Bơm N | `/reports/pump-temperatures?deviceId={id}` |

```json
{
  "success": true,
  "message": "Danh sách thiết bị báo cáo.",
  "data": [
    { "id": 0, "name": "Mức nước" },
    { "id": 1, "name": "Nhiệt độ Bơm 1" },
    { "id": 31, "name": "Nhiệt độ Bơm 2" }
  ]
}
```

### 4.2 Báo cáo mức nước
```
GET /api/v1/stations/{stationId}/reports/water-levels
  ?reportDate=2026-06-01
  &startTime=00:00:00
  &endTime=23:59:59
  &deviceId=          (optional)
  &pageNumber=1
  &pageSize=20
```

**Cột FE ↔ Response ↔ Tag**

| Cột FE | Field | Tag code |
|--------|-------|----------|
| Thời gian | `time` | `history_30s.time` |
| Mức nước sông | `riverLevel` | `LEVEL_RIVER` |
| Mức xả 1…10 | `discharge1`…`discharge10` | `LEVEL_DISCHARGE_1`…`_10` |

```json
{
  "success": true,
  "data": {
    "items": [{
      "time": "2026-05-31T17:00:00+00:00",
      "riverLevel": 2.15,
      "discharge1": 1.40,
      "discharge2": 1.41,
      "discharge3": null,
      "discharge4": null,
      "discharge5": null,
      "discharge6": null,
      "discharge7": null,
      "discharge8": null,
      "discharge9": null,
      "discharge10": null
    }],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 20,
    "totalPages": 1
  }
}
```

### 4.3 Báo cáo nhiệt độ bơm
```
GET /api/v1/stations/{stationId}/reports/pump-temperatures
  ?reportDate=2026-06-01
  &startTime=00:00:00
  &endTime=23:59:59
  &deviceId=1          (null = tất cả bơm Pump)
  &pageNumber=1
  &pageSize=20
```

**Cột FE ↔ Response ↔ Tag**

| Cột FE | Field | Tag |
|--------|-------|-----|
| Thời gian | `time` | history_30s |
| Bơm | `pump` (+ `deviceId`) | `device.name` |
| Nhiệt độ A/B/C | `tempA/B/C` | `TEMP_COIL_A/B/C` |
| Nhiệt độ bi dưới | `bearingBottom` | `BEARING_BOTTOM` |
| Nhiệt độ bi trên | `bearingTop` | `BEARING_TOP` |

```json
{
  "success": true,
  "data": {
    "items": [{
      "time": "2026-05-31T17:04:30+00:00",
      "deviceId": 1,
      "pump": "Bơm 1",
      "tempA": 32,
      "tempB": 33,
      "tempC": 34,
      "bearingBottom": 44,
      "bearingTop": 46
    }],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 10,
    "totalPages": 1
  }
}
```

### 4.4 Luồng FE đề xuất (màn báo cáo)
```
1. Load dropdown  → GET .../reports/devices
2. User chọn id:
   - id == 0  → GET .../reports/water-levels?...
   - id  > 0  → GET .../reports/pump-temperatures?deviceId={id}&...
3. Đổi trang     → cùng URL + pageNumber
4. Làm mới       → reset reportDate/start/end + pageNumber=1
5. Xuất Excel    → `GET .../reports/table/export` (Operator/Admin), cap 10_000 rows
```

---

## 5. Bảng DB liên quan

| Schema.table | Dùng cho |
|--------------|----------|
| `scada.station` | Path `stationId`, chi tiết trạm |
| `scada.plc` | Join device theo trạm |
| `scada.device` | Bơm / lọc deviceId, dropdown |
| `scada.tag` | Map code → cột báo cáo / monitor |
| `history.history_1s` | Device-monitor (giá trị mới nhất) |
| `history.history_30s` | Báo cáo mức nước + nhiệt độ (chu kỳ 30s) |

Cột tối thiểu history: `time`, `tag_id`, `value`.

---

## 6. Danh mục endpoint Stations (tóm tắt)

| Method | Path | Phân trang | Ghi chú |
|--------|------|------------|---------|
| GET | `/stations` | Có | List |
| GET | `/stations/{id}` | Không | Detail |
| GET | `/stations/{id}/electrical` | Không | |
| GET | `/stations/{id}/schematic` | Không | |
| GET | `/stations/{id}/device-cards` | Không | Full tags |
| GET | `/stations/{id}/device-monitor` | Không | Màn Devices |
| GET | `/stations/{id}/reports/devices` | Không | Dropdown id+name |
| GET | `/stations/{id}/reports/water-levels` | Có | history_30s |
| GET | `/stations/{id}/reports/pump-temperatures` | Có | history_30s |

---

## 7. File liên quan trong repo

| File | Nội dung |
|------|----------|
| `BE/docs/API_Design_Stations_Reports.md` | Tài liệu này |
| `BE/docs/MAP_Device_Monitor_API.txt` | Map field màn Devices |
| `BE/docs/MAP_Station_History_Reports.txt` | Map báo cáo lịch sử |
| `BE/docs/MAP_WaterLevel_Report_API.txt` | Map mức nước |
| `BE/docs/Tong_Quan_API_Service_DTO.md` | Tổng quan toàn solution |
| `BE/scripts/seed_history_30s_water_level.sql` | Seed mức nước |
| `BE/scripts/seed_history_30s_pump_temp.sql` | Seed nhiệt độ |

---

*Cập nhật: theo code `StationsController` hiện tại.*
