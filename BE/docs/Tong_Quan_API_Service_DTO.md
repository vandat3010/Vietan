# Tổng quan API · Service · Repository · DTO

Tài liệu mô tả toàn bộ endpoint, interface, implementation và các trường DTO của solution **Backend (TLN_API)**.

> **API Design chi tiết Stations + Báo cáo:** xem [`API_Design_Stations_Reports.md`](./API_Design_Stations_Reports.md).

> **Auth hiện đang tắt** (`TAM-TAT-LOGIN`): JWT / `[Authorize]` bị comment trong `Program.cs` và hầu hết controller. Bật lại bằng cách bỏ comment các dòng đánh dấu `TAM-TAT-LOGIN`.

Response API mặc định bọc trong `ApiResponse<T>` (trừ export Excel và `/health`).

Query danh sách kế thừa `PaginationRequest` luôn có thêm:

| Trường | Kiểu | Ý nghĩa |
|--------|------|---------|
| `pageNumber` | int | Số trang (bắt đầu từ 1) |
| `pageSize` | int | Số bản ghi mỗi trang |
| `keyword` | string? | Từ khóa tìm tự do |
| `filters` | dictionary? | Lọc field=value (repo quyết định field nào hợp lệ) |
| `sortBy` | string? | Tên cột sắp xếp |
| `sortDirection` | enum | Ascending / Descending |
| `sorts` | list? | Sắp xếp nhiều cấp |

---

# 1. API Endpoints

## 1.1 Xác thực — `api/v1/Auth`

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| POST | `/api/v1/Auth/register` | Đăng ký tài khoản và trả token ngay |
| POST | `/api/v1/Auth/login` | Đăng nhập, cấp access + refresh token |
| POST | `/api/v1/Auth/refresh-token` | Đổi refresh token lấy cặp token mới |
| POST | `/api/v1/Auth/revoke-token` | Thu hồi một refresh token (logout thiết bị) |

**Body:**

- `RegisterDto`: `email`, `firstName`, `lastName`, `password`
- `LoginDto`: `email`, `password`
- `RefreshTokenRequestDto` / `RevokeTokenDto`: `refreshToken`

**Trả về:** `AuthResultDto` — `accessToken`, `refreshToken`, `accessTokenExpiresAtUtc`, `userId`, `email`, `roles`

---

## 1.2 User IAM — `api/v1/Users` (schema `app`)

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| GET | `/api/v1/Users` | Danh sách user phân trang / lọc |
| GET | `/api/v1/Users/export` | Xuất Excel theo cùng bộ lọc |
| GET | `/api/v1/Users/{id}` | Chi tiết user |
| POST | `/api/v1/Users` | Tạo user |
| PUT | `/api/v1/Users/{id}` | Cập nhật hồ sơ |
| DELETE | `/api/v1/Users/{id}` | Xóa mềm |
| POST | `/api/v1/Users/{id}/change-password` | Đổi mật khẩu |
| PUT | `/api/v1/Users/{id}/roles` | Gán lại role |
| POST | `/api/v1/Users/{id}/deactivate` | Vô hiệu hóa |
| POST | `/api/v1/Users/{id}/activate` | Kích hoạt lại |

**Query thêm (`UserSearchQuery`):** `status`, `roleId`, `createdFromUtc`, `createdToUtc`, `includeDeleted`

---

## 1.3 Role — `api/v1/Roles`

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| GET | `/api/v1/Roles` | Liệt kê mọi role |
| GET | `/api/v1/Roles/{id}` | Chi tiết role |
| POST | `/api/v1/Roles` | Tạo role (`name`, `description`, `permissionIds`) |

---

## 1.4 Audit & Report

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| GET | `/api/v1/audit-logs` | Nhật ký audit (chỉ đọc) |
| GET | `/api/v1/Reports/users/summary` | Tổng hợp số user (Dapper) |
| GET | `/api/v1/Reports/users/top-active?top=10` | Top user hoạt động |
| GET | `/health` | Health check (PostgreSQL) |

---

## 1.5 SCADA Metadata — schema `scada` (chỉ GET)

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| GET | `/api/v1/stations` | Danh sách trạm (lọc `isActive`, `keyword`) |
| GET | `/api/v1/stations/{id}` | Chi tiết trạm |
| GET | `/api/v1/stations/{stationId}/electrical` | Thông số điện theo `stationId` (optional `deviceId`, `deviceType`) |
| GET | `/api/v1/stations/{stationId}/schematic` | Sơ đồ nguyên lý: MCCB I/V, thẻ bơm, status (không gồm electrical) |
| GET | `/api/v1/stations/{stationId}/device-cards` | Thiết bị theo trạm: join `station→plc→device→tag→history_1s` theo Id |
| GET | `/api/v1/stations/{stationId}/device-monitor` | Màn Devices: field UI card bơm (không dump plc/tag) |
| GET | `/api/v1/stations/{stationId}/reports/devices` | Dropdown báo cáo: chỉ `{ id, name }` (`0`=Mức nước) |
| GET | `/api/v1/stations/{stationId}/reports/water-levels` | Báo cáo mức nước (`history_30s`), phân trang trong ngày |
| GET | `/api/v1/stations/{stationId}/reports/pump-temperatures` | Báo cáo nhiệt độ bơm (`history_30s`), phân trang trong ngày |
| GET | `/api/v1/plcs` | Danh sách PLC (`stationId`, `isEnable`) |
| GET | `/api/v1/plcs/{id}` | Chi tiết PLC |
| GET | `/api/v1/devices` | Danh sách thiết bị (`plcId`, `deviceType`) |
| GET | `/api/v1/devices/{id}` | Chi tiết thiết bị |
| GET | `/api/v1/tags` | Danh sách tag (`plcId`, `deviceId`, `enableRealtime`…) |
| GET | `/api/v1/tags/{id}` | Chi tiết tag |
| GET | `/api/v1/history-profiles` | Tất cả profile ghi lịch sử |
| GET | `/api/v1/history-profiles/{id}` | Chi tiết profile |
| GET | `/api/v1/tag-history-configs` | Cấu hình tag ↔ profile |
| GET | `/api/v1/tag-history-configs/{id}` | Chi tiết cấu hình |
| GET | `/api/v1/communication-configs` | Cấu hình giao thức (S7, Modbus…) |
| GET | `/api/v1/communication-configs/{id}` | Chi tiết |
| GET | `/api/v1/mqtt-configs` | Cấu hình MQTT (**không trả password**) |
| GET | `/api/v1/mqtt-configs/{id}` | Chi tiết |
| GET | `/api/v1/scada-users` | User vận hành SCADA (**không trả passwordHash**) |
| GET | `/api/v1/scada-users/{id}` | Chi tiết |
| GET | `/api/v1/app-settings` | Tất cả setting ứng dụng |
| GET | `/api/v1/app-settings/{id}` | Theo Id |
| GET | `/api/v1/app-settings/by-key/{settingKey}` | Theo khóa |

---

## 1.6 History / Timescale — schema `history` (chỉ GET)

> **NHỚ:** `history_1s` volume lớn → **Dapper + Stored Procedure**, không EF.  
> Chi tiết: [`API_Design_Stations_Reports.md`](./API_Design_Stations_Reports.md) §0.

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| GET | `/api/v1/history/1s` | Mẫu 1 giây — **bắt buộc** `from` + `to` |
| GET | `/api/v1/history/1m` | Mẫu 1 phút — bắt buộc khoảng thời gian |
| GET | `/api/v1/history/30m` | Mẫu 30 phút — bắt buộc khoảng thời gian |
| GET | `/api/v1/alarm-histories` | Lịch sử alarm |
| GET | `/api/v1/alarm-histories/{id}` | Chi tiết alarm |
| GET | `/api/v1/event-logs` | Nhật ký sự kiện hệ thống |
| GET | `/api/v1/event-logs/{id}` | Chi tiết |
| GET | `/api/v1/user-activity-logs` | Nhật ký thao tác user SCADA |
| GET | `/api/v1/user-activity-logs/{id}` | Chi tiết |

---

# 2. Service Interfaces & Implementation

## 2.1 Application Services (business IAM)

| Interface | Implementation | Vai trò |
|-----------|----------------|---------|
| `IAuthService` | `AuthService` | Đăng ký/đăng nhập, refresh/revoke/logout, reset mật khẩu, validate token |
| `IUserService` | `UserService` | CRUD user, export Excel, đổi MK, gán role, activate/deactivate |
| `IRoleService` | `RoleService` | Đọc/tạo role |

### Phương thức chính

**`IAuthService`:** `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`, `RevokeTokenAsync`, `LogoutAsync`, `ResetPasswordAsync`, `ValidateTokenAsync`

**`IUserService`:** `GetByIdAsync`, `GetAllAsync`, `ExportExcelAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `ChangePasswordAsync`, `AssignRolesAsync`, `DeactivateAsync`, `ActivateAsync`

**`IRoleService`:** `GetAllAsync`, `GetByIdAsync`, `CreateAsync`

---

## 2.2 Infrastructure Services (kỹ thuật / đọc SCADA)

| Interface | Implementation | Vai trò |
|-----------|----------------|---------|
| `ITokenService` | `JwtTokenService` | Sinh / validate JWT + refresh token |
| `IAuditService` | `AuditService` | Ghi & đọc audit log |
| `IReportService` | `ReportService` | Chạy SQL/SP (Dapper), xuất Excel/PDF |
| `IImportExportService` | `ExcelImportExportService` | Import / validate / export Excel |
| `ICurrentUserService` | `CurrentUserService` | User hiện tại từ HTTP claims |
| `IDateTimeProvider` | `DateTimeProvider` | Đồng hồ hệ thống (tránh `DateTime.Now`) |
| `IFileStorageService` | `LocalFileStorageService` | Upload/download file |
| `ICacheService` | `MemoryCacheService` | Cache (có thể thay Redis) |
| `INotificationService` | `LoggingNotificationService` | Gửi thông báo (mặc định ghi log) |
| `IBackgroundJobService` | `InMemoryBackgroundJobService` | Job nền in-process |

### SCADA Query (một class — nhiều interface)

| Class | Implements |
|-------|------------|
| `ScadaMetadataQueryService` | `IStationQueryService`, `IPlcQueryService`, `IDeviceQueryService`, `ITagQueryService`, `IHistoryProfileQueryService`, `ITagHistoryConfigQueryService`, `ICommunicationConfigQueryService`, `IMqttConfigQueryService`, `IScadaUserQueryService`, `IAppSettingQueryService` |
| `HistoryQueryService` | `IHistorySampleQueryService`, `IAlarmHistoryQueryService`, `IScadaEventLogQueryService`, `IUserActivityLogQueryService` |

Mỗi query interface đều có dạng: **GetPaged/GetAll** + **GetById**; `IAppSettingQueryService` thêm `GetByKeyAsync`.

---

## 2.3 Industrial (chưa có implementation)

Contract tương lai trong `Application/Interfaces/Industrial` — **chưa đăng ký DI, chưa có controller**:

- `IDeviceService` — trạng thái thiết bị / heartbeat  
- `ITagService` — giá trị realtime & lịch sử tag  
- `IAlarmService` — tạo / acknowledge / clear alarm  
- `IEventLogService` — ghi / đọc event công nghiệp  

---

# 3. Repository · UnitOfWork · Dapper

## 3.1 Repository

| Interface | Implementation | Vai trò |
|-----------|----------------|---------|
| `IGenericRepository<T>` | `GenericRepository<T>` | CRUD + đọc cơ bản EF (`T : BaseEntity<Guid>`) |
| `ISoftDeleteRepository<T>` | `SoftDeleteRepository<T>` | Soft delete / restore / lấy bản đã xóa |
| `IUserRepository` | `UserRepository` | User + tìm theo email, refresh token, `SearchAsync` |
| `IRoleRepository` | `RoleRepository` | Role + `GetByNameAsync`, `GetByIdsAsync` |

### `IGenericRepository<T>` — method chính

**Đọc:** `GetByIdAsync`, `GetAllAsync`, `FindAsync`, `FirstOrDefaultAsync`, `ListAsync(spec)`, `GetPagedAsync`, `CountAsync`, `AnyAsync`, `ExistsAsync`

**Ghi (chỉ stage — lưu qua UoW):** `CreateAsync`, `CreateRangeAsync`, `Update`, `Delete`, `DeleteRange`, `DeleteAsync`

### `ISoftDeleteRepository<T>` thêm: `SoftDeleteAsync`, `RestoreAsync`, `GetDeletedAsync`

### `IUserRepository` thêm: `GetByEmailAsync`, `GetWithRolesAsync`, `GetByRefreshTokenAsync`, `EmailExistsAsync`, `SearchAsync`

---

## 3.2 `IUnitOfWork` → `UnitOfWork`

Điều phối nhiều repository trong **một transaction**:

- Property: `Users`, `Roles`
- `SaveChangesAsync`
- `BeginTransactionAsync` / `CommitTransactionAsync` / `RollbackTransactionAsync`
- `ExecuteInTransactionAsync(...)` — bọc commit/rollback an toàn

---

## 3.3 Dapper (chỉ đọc)

| Interface | Implementation | Vai trò |
|-----------|----------------|---------|
| `IDapperContext` | `DapperContext` | Tạo `IDbConnection` ngắn hạn |
| `IDapperRepository` | `DapperRepository` | `QueryAsync`, `ExecuteAsync`, `ExecuteStoredProcedureAsync`… |
| `IUserReportQueries` | `UserReportQueries` | Báo cáo summary / top-active users |
| `ISqlConnectionFactory` | `SqlConnectionFactory` | Factory kết nối PostgreSQL thô |

> SCADA GET **không** dùng repository riêng — query service gọi thẳng `ApplicationDbContext` (`AsNoTracking`).

---

# 4. Giải thích DTO & các trường

## 4.1 Auth / User / Role

### `UserDto`
| Trường | Kiểu | Ý nghĩa |
|--------|------|---------|
| `id` | Guid | Khóa user IAM |
| `email` | string | Email đăng nhập |
| `firstName` / `lastName` | string | Họ tên |
| `phoneNumber` | string? | SĐT |
| `status` | string | Trạng thái (Active, Locked…) |
| `lastLoginAtUtc` | DateTime? | Đăng nhập gần nhất |
| `createdDate` | DateTime | Ngày tạo |
| `roles` | list string | Tên các role |

### `CreateUserDto` / `UpdateUserDto` / `ChangePasswordDto` / `AssignRolesDto`
- Tạo: `email`, `firstName`, `lastName`, `password`, `phoneNumber?`, `roleIds`
- Cập nhật: `firstName`, `lastName`, `phoneNumber?`
- Đổi MK: `currentPassword`, `newPassword`
- Gán role: `roleIds`

### `AuthResultDto`
| Trường | Ý nghĩa |
|--------|---------|
| `accessToken` | JWT ngắn hạn |
| `refreshToken` | Token dài hạn (rotation) |
| `accessTokenExpiresAtUtc` | Thời điểm hết hạn access |
| `userId`, `email`, `roles` | Thông tin phiên |

### `RoleDto`
| Trường | Ý nghĩa |
|--------|---------|
| `id`, `name`, `description` | Định danh & mô tả |
| `isSystemRole` | Role hệ thống (không xóa tùy tiện) |
| `permissions` | Danh sách quyền (string) |

### `AuditLogDto`
| Trường | Ý nghĩa |
|--------|---------|
| `entityName`, `entityId` | Đối tượng bị tác động |
| `action` | Created / Updated / Deleted / Login… |
| `userId`, `userName` | Ai thực hiện |
| `oldValues`, `newValues` | JSON trước/sau |
| `ipAddress`, `correlationId` | Truy vết |
| `createdDate` | Thời điểm ghi |

### Báo cáo
- `UserSummaryReportDto`: `totalUsers`, `activeUsers`, `inactiveUsers`, `lockedUsers`, `newUsersLast30Days`
- `UserActivityReportDto`: `userId`, `email`, `fullName`, `lastLoginAtUtc`, `loginCountLast30Days`

---

## 4.2 SCADA Metadata

### `StationDto` — Trạm (list)
| Trường | Ý nghĩa |
|--------|---------|
| `id`, `code`, `name` | Khóa / mã / tên trạm |
| `isActive` | Trạm đang giám sát? |

### `StationDetailDto` — Chi tiết trạm theo id
| Trường | Ý nghĩa |
|--------|---------|
| `id`, `code`, `name` | Khóa / mã / tên trạm |
| `address` | Địa chỉ |
| `latitude`, `longitude` | Tọa độ |
| `isActive` | Trạm đang giám sát? |

### `DeviceElectricalDto` / `StationElectricalDto` — Thông số điện (GET `/api/v1/stations/{stationId}/electrical`)
| Trường | Ý nghĩa |
|--------|---------|
| `station.id/code/name` | Trạm — input bắt buộc `stationId` |
| `items[].equipment` | Thiết bị thuộc trạm (`scada.device` qua `plc.station_id`) |
| `items[].parameters[].key` | Key FE: `voltageRs` … `energyKwh` |
| `items[].parameters[].tagId/code/value/unit/timestamp` | Tag + latest `history_1s` |

### `StationSchematicDto` — Sơ đồ nguyên lý (GET `/api/v1/stations/{stationId}/schematic`)
| Trường | Ý nghĩa |
|--------|---------|
| `layout.mba/msb/mdbs` | Nhãn SVG tĩnh (MBA/MSB/MDB) |
| `pumps[].i1..v3` | Ô MCCB |
| `pumps[].currentA/runtimeH` | Thẻ bơm |
| `pumps[].motorStatus/kdmStatus/lockStatus` | Màu M / KĐM / khoá |

### `StationDeviceCardsDto` — Thiết bị theo trạm (GET `/api/v1/stations/{stationId}/device-cards`)
Join theo Id (không flatten UI):

| Khối | Bảng | Join |
|------|------|------|
| `station` | `scada.station` | `station.id = @stationId` |
| `items[].plc` | `scada.plc` | `plc.station_id = station.id` (qua `device.plc_id`) |
| `items[].device` | `scada.device` | `device.plc_id = plc.id` |
| `items[].tags[]` | `scada.tag` | `tag.device_id = device.id` |
| `items[].tags[].value/timestamp` | `history.history_1s` | `history_1s.tag_id = tag.id` (latest) |

Query lọc: `deviceId`, `deviceType`, `isEnable` (cột trên `scada.device`).

### `StationDeviceMonitorDto` — Danh sách bơm + thông số (GET `/api/v1/stations/{stationId}/device-monitor`)

Chi tiết map từng field ↔ cột DB: xem `BE/docs/MAP_Device_Monitor_API.txt`.

Station chi tiết: `GET /api/v1/stations/{id}`. Response **chỉ field card UI**.

| Field | UI |
|-------|-----|
| `name`, `ratedPowerKw` | Header "Bơm N - 160kW" |
| `status` | Thanh trạng thái |
| `windingTempActual/Allowed` | Nhiệt cuộn A/B/C |
| `bearingTop/Bottom` | Ổ bi thực tế / cho phép |
| `waterLevel` | Sông / Bể xả |
| `runtime` | Tức thời / Tổng |
| `electrical.*` | 8 thông số điện |

Chi tiết: `BE/docs/MAP_Device_Monitor_API.txt`.


### `PlcDto` — PLC
| Trường | Ý nghĩa |
|--------|---------|
| `stationId`, `stationCode` | Trạm cha |
| `code`, `name`, `plcType` | Mã, tên, loại (S7-1200…) |
| `ipAddress`, `rack`, `slot`, `port` | Thông số kết nối |
| `pollingInterval` | Chu kỳ đọc (ms) |
| `reconnectInterval` | Chu kỳ reconnect (ms) |
| `timeout` | Timeout (ms) |
| `maxConnection` | Số kết nối song song tối đa |
| `isEnable` | Bật/tắt PLC |

### `DeviceDto` — Thiết bị
| Trường | Ý nghĩa |
|--------|---------|
| `plcId`, `plcCode` | PLC quản lý |
| `code`, `name`, `displayName` | Mã / tên nội bộ / tên HMI |
| `deviceType` | Loại (Pump, PowerMeter, Level…) |
| `isEnable` | Cho phép hoạt động |

### `TagDto` — Điểm dữ liệu PLC
| Trường | Ý nghĩa |
|--------|---------|
| `id` (**TagId**) | Định danh toàn hệ thống (Redis/History dùng Id này) |
| `plcId`, `deviceId` | PLC & thiết bị |
| `code` | Mã tag duy nhất |
| `tagName` | Tên tag trong PLC (cột DB `tag`) |
| `displayName` | Tên hiển thị |
| `address` | Địa chỉ DB/Byte/Bit |
| `dataType` | Bool / Int / Real… |
| `unit` | Đơn vị (°C, A, m…) |
| `scale`, `offsetValue` | Hệ số quy đổi |
| `readOnly`, `writeEnable` | Quyền đọc/ghi |
| `enableRealtime` | Publish Redis? |
| `enableAlarm` | Sinh alarm? |

### `HistoryProfileDto`
| Trường | Ý nghĩa |
|--------|---------|
| `code` | HP_1S, HP_1M, HP_30M… |
| `intervalSecond` | Chu kỳ ghi (giây) |
| `retentionDay` | Số ngày giữ dữ liệu |
| `compressionDay` | Sau bao nhiêu ngày nén (Timescale) |
| `isEnable` | Bật profile |

### `TagHistoryConfigDto`
| Trường | Ý nghĩa |
|--------|---------|
| `tagId`, `historyProfileId` | Liên kết tag ↔ profile |
| `tagCode`, `historyProfileCode` | Mã hiển thị |
| `deadband` | Ngưỡng thay đổi mới ghi |
| `priority` | Độ ưu tiên |
| `isEnable` | Bật cấu hình |

### `CommunicationConfigDto`
| Trường | Ý nghĩa |
|--------|---------|
| `code`, `name` | Mã / tên |
| `protocol` | S7, MQTT, ModbusTCP, OPCUA… |
| `isEnable` | Bật |

### `MqttConfigDto`
| Trường | Ý nghĩa |
|--------|---------|
| `broker`, `port` | Địa chỉ broker |
| `username`, `clientId` | Tài khoản / ClientId (không trả password) |
| `topicPublish`, `topicSubscribe` | Topic |
| `keepAlive`, `qos`, `retain`, `useTls` | Tham số MQTT |
| `isEnable` | Bật |

### `ScadaUserDto` (khác User IAM)
| Trường | Ý nghĩa |
|--------|---------|
| `username`, `displayName` | Đăng nhập / tên hiển thị |
| `role` | Vai trò dạng chuỗi |
| `isEnable` | Đang hoạt động |
| `lastLoginAt` | Đăng nhập gần nhất |

### `AppSettingDto`
| Trường | Ý nghĩa |
|--------|---------|
| `settingKey` | Khóa cấu hình |
| `settingValue` | Giá trị |
| `dataType` | Kiểu giá trị (string, int…) |
| `isEnable` | Bật |

---

## 4.3 History (Timescale)

### `HistorySampleDto`
| Trường | Ý nghĩa |
|--------|---------|
| `time` | Mốc thời gian mẫu |
| `tagId` | Tag (không join FK trong DB) |
| `value` | Giá trị `double` |

### `HistorySampleQuery`
| Trường | Ý nghĩa |
|--------|---------|
| `from`, `to` | **Bắt buộc** — khoảng thời gian |
| `tagId` / `tagIds` | Lọc một hoặc nhiều tag |

### `AlarmHistoryDto`
| Trường | Ý nghĩa |
|--------|---------|
| `stationId`, `plcId`, `deviceId`, `tagId` | Tham chiếu metadata (nullable) |
| `deviceName`, `tagName` | Snapshot tên lúc alarm |
| `description`, `troubleshootingGuide` | Mô tả / hướng xử lý |
| `type` | Loại alarm |
| `isAcknowledged` | Đã xác nhận? |
| `durationSeconds` | Thời gian tồn tại |
| `startTime`, `endTime` | Bắt đầu / kết thúc |
| `createdAt` | Thời điểm ghi |

### `EventLogDto`
| Trường | Ý nghĩa |
|--------|---------|
| `time`, `level`, `module` | Thời điểm / mức / module (API, PLC, MQTT…) |
| `message`, `exception` | Nội dung / stack |
| `machine` | Máy chủ ghi log |

### `UserActivityLogDto`
| Trường | Ý nghĩa |
|--------|---------|
| `createdAt` | Thời điểm |
| `userName` | Ai thao tác |
| `actionType` | Loại hành động |
| `description` | Chi tiết |
| `ipAddress` | IP |

---

# 5. Sơ đồ phụ thuộc nhanh

```
Controller
   │
   ├─ IAuthService / IUserService / IRoleService     → Application Services
   │         └─ IUnitOfWork (IUserRepository, IRoleRepository) → EF Core (schema app)
   │
   ├─ IStationQueryService … IAppSettingQueryService → ScadaMetadataQueryService → EF (schema scada)
   │
   ├─ IHistorySampleQueryService …                   → HistoryQueryService → EF (schema history)
   │
   └─ IUserReportQueries                             → Dapper (đọc báo cáo)
```

---

# 6. Lưu ý quan trọng

1. **Hai hệ user:** `app.Users` (IAM / JWT) ≠ `scada.users` (`ScadaUser` vận hành SCADA).
2. **SCADA API hiện chỉ GET** — chưa có POST/PUT/DELETE metadata.
3. **History bắt buộc `from` + `to`** — tránh quét cả hypertable.
4. Có **hai** bộ DTO Industrial (Guid, tương lai) khác bộ History/SCADA đang dùng API (`long`).
5. Connection Development: xem `appsettings.Development.json` (`postgres` / `123456` / DB `postgres`).
