# Backend Enterprise Template

Template backend **.NET 10** dùng lại được cho nhiều dự án, xây dựng theo
**Clean Architecture**, **Domain-Driven Design (DDD)**, **Repository + Unit of
Work**, dùng **EF Core** cho phía ghi và **Dapper** cho phía đọc, chạy trên
**PostgreSQL / TimescaleDB**.

Mục tiêu: làm điểm khởi đầu cho các hệ ERP, SCADA, MES, CRM, HRM, Manufacturing,
Warehouse. Bạn thay aggregate mẫu `User` bằng domain của mình, phần kiến trúc
bên dưới giữ nguyên.

> **Tài liệu chi tiết API / Service / Repository / DTO (tiếng Việt):**
> [`docs/Tong_Quan_API_Service_DTO.md`](docs/Tong_Quan_API_Service_DTO.md)

## 1. Cấu trúc solution

```
Backend.sln
docs/
└── Tong_Quan_API_Service_DTO.md   # Catalog API + interface + DTO tiếng Việt
src/
├── Api/                        # ASP.NET Core Web API - chỉ là tầng trình bày
│   ├── Controllers/            # IAM: Auth, Users, Roles, Audit, Reports
│   │   └── Scada/              # GET SCADA metadata + history (Timescale)
│   ├── Middlewares/            # CorrelationId, ExceptionHandling, Performance
│   ├── Filters/                # ValidationFilter (FluentValidation)
│   ├── Extensions/             # AddApiServices() (CORS), AddJwtAuthentication()
│   ├── Swagger/                # Cấu hình Swashbuckle + JWT security scheme
│   ├── Configuration/          # Section appsettings có kiểu (CorsSettings...)
│   ├── Program.cs
│   └── appsettings*.json
│
├── Application/                # Use case / điều phối. Không EF Core, không ASP.NET Core.
│   ├── Common/                 # Abstraction hạ tầng mà Application cần:
│   │                           #   ICurrentUserService, IDateTimeProvider,
│   │                           #   IFileStorageService, ICacheService,
│   │                           #   INotificationService, IBackgroundJobService
│   ├── Interfaces/
│   │   ├── Repositories/       # IGenericRepository<T>, ISoftDeleteRepository<T>,
│   │   │                       # IUserRepository, IRoleRepository
│   │   ├── Services/           # IUserService, IAuthService, IRoleService, ITokenService,
│   │   │                       # IAuditService, IReportService, IImportExportService
│   │   │   └── Scada/          # Query interfaces: Station, Plc, Tag, History...
│   │   ├── Dapper/             # IDapperContext, IDapperRepository, IUserReportQueries
│   │   ├── UnitOfWork/         # IUnitOfWork
│   │   └── Industrial/         # Hợp đồng realtime/ingest tương lai (chưa implement)
│   ├── Services/               # UserService, AuthService, RoleService
│   ├── DTOs/                   # Users, Auth, Roles, Reports, Audit, Scada, History, Industrial
│   ├── Mapping/                # Extension method Entity <-> DTO (viết tay)
│   ├── Validators/             # FluentValidation
│   └── DependencyInjection.cs
│
├── Domain/                     # Lõi nghiệp vụ - chỉ phụ thuộc Shared
│   ├── Entities/               # IAM: User, Role, Permission, RefreshToken, AuditLog
│   │   ├── Scada/              # Station, Plc, Device, Tag, HistoryProfile...
│   │   └── History/            # History1s/1m/30m, AlarmHistory, EventLog...
│   ├── Enums/                  # UserStatus, AuditAction
│   ├── ValueObjects/           # Email, FullName
│   ├── Events/                 # UserCreatedEvent, UserLockedOutEvent...
│   ├── Specifications/         # ISpecification<T>, BaseSpecification<T>
│   ├── Common/                 # BaseEntity, AggregateRoot, AuditableEntity,
│   │                           # ISoftDelete, IDomainEvent, ValueObject, DomainException
│   └── Interfaces/             # IPasswordHasher (hợp đồng domain service)
│
├── Infrastructure/             # Toàn bộ chi tiết kỹ thuật/framework
│   ├── Common/                 # CurrentUser, DateTime, FileStorage, Cache,
│   │                           # Notifications, BackgroundJobs, ConnectionFactory
│   ├── Scada/                  # ScadaMetadataQueryService, HistoryQueryService
│   ├── Persistence/
│   │   ├── Context/            # ApplicationDbContext (+ design-time factory)
│   │   ├── Configurations/     # IAM + Scada/ + History/ (snake_case, schema)
│   │   ├── Repository/         # Generic / SoftDelete / User / Role
│   │   ├── UnitOfWork/
│   │   ├── Auditing/
│   │   └── Migrations/
│   ├── Dapper/
│   ├── Identity/
│   ├── Reporting/              # ReportService, ExcelImportExportService (ClosedXML)
│   ├── Logging/
│   └── DependencyInjection.cs
│
└── Shared/                     # Constants, Extensions, Results, Responses,
                                # Pagination, Exceptions, Helpers, Models
```

### Schema PostgreSQL

| Schema | Nội dung |
|--------|----------|
| `app` | IAM: Users, Roles, Permissions, RefreshTokens, AuditLogs (PascalCase, Guid) |
| `scada` | Metadata SCADA: station, plc, device, tag… (snake_case, `long`) |
| `history` | Timescale hypertables: history_1s/1m/30m, alarm_history, event_log… |

Hai hệ user **khác nhau**: `app.Users` (JWT/IAM) ≠ `scada.users` (`ScadaUser` vận hành).

### `scada.users` — cấu trúc bảng (chuẩn hóa Phase 0)

Luồng đăng nhập đang chạy dùng `scada.users` (`ScadaUser`). Phase 0 mở rộng bảng
theo hướng **additive, không phá dữ liệu** để làm nền cho các phase
Authentication/Security tiếp theo (đổi mật khẩu lần đầu, hết hạn mật khẩu, khóa
đăng nhập). Phase 0 **chỉ** chuẩn hóa data model — chưa cài đặt logic.

| Nhóm | Cột | Kiểu C# | Cột PostgreSQL | Ghi chú |
|------|-----|---------|----------------|---------|
| Gốc | `Username`,`PasswordHash`,`FullName`,`Role`,`IsActive`,`LastLoginAt` | — | `username`,`password_hash`,`full_name`,`role`,`is_active`,`last_login_at` | ERD rename: `display_name`→`full_name`, `is_enable`→`is_active` (migration `AlignErdbNamingAndAlarmLookups`) |
| Hồ sơ | `Email` | `string?` | `email varchar(256)` | nullable |
| Hồ sơ | `Unit` | `string?` | `unit varchar(100)` | nullable |
| Hồ sơ | `Level` | `int?` | `level integer` | số, **không** enum/không business rule ở Phase 0 |
| Hồ sơ | `Department` | `string?` | `department varchar(150)` | nullable |
| Hồ sơ | `Position` | `string?` | `position varchar(150)` | nullable |
| Hồ sơ | `Description` | `string?` | `description varchar(500)` | nullable |
| Hồ sơ | `CreatedBy`/`UpdatedBy` | `string?` | `created_by`/`updated_by varchar(100)` | nullable |
| Bảo mật | `MustChangePassword` | `bool` | `must_change_password boolean` | default `false` (backfill) |
| Bảo mật | `PasswordUpdatedAt` | `DateTimeOffset?` | `password_updated_at timestamptz` | nullable |
| Bảo mật | `FailedLoginCount` | `int` | `failed_login_count integer` | default `0` (backfill) |
| Bảo mật | `LockoutUntil` | `DateTimeOffset?` | `lockout_until timestamptz` | nullable |

- Migration: `Phase0_NormalizeScadaUser` — chỉ `AddColumn`, có default cho 2 cột
  non-null; `Down()` chỉ rollback đúng các cột Phase 0. Không DROP TABLE / không
  đổi cột cũ / không xóa data.
- Entity: `Domain/Entities/Scada/ScadaUser.cs`; mapping:
  `Infrastructure/Persistence/Configurations/Scada/ScadaUserConfiguration.cs`.
- DTO auth (`Application/DTOs/Auth/ScadaAuthDtos.cs`): `AuthTokenResponse` và
  `CurrentUserResponse` trả thêm field **hồ sơ**. **Không bao giờ** trả
  `PasswordHash`/`Password`/`FailedLoginCount`/`LockoutUntil` ra client.
- Mapping cập nhật tại `AuthenticationService.GetCurrentUserAsync` và
  `IssueTokensAsync`.

### BE 1.3a — Bắt đổi mật khẩu lần đầu (`must_change_password`)

Khi `scada.users.must_change_password = true`, user **login được bình thường**
(không phải lỗi) nhưng bị chặn mọi business API cho tới khi đổi mật khẩu.

- **T1a.1 Seed**: `scripts/seed_must_change_password.sql` — set cờ cho **user mặc
  định** (`username = 'admin'`), idempotent, **không** blanket update user khác.
- **T1a.2 Login**: `AuthTokenResponse.MustChangePassword` + `CurrentUserResponse.MustChangePassword`
  lấy từ DB (`AuthenticationService.IssueTokensAsync` / `GetCurrentUserAsync`).
- **T1a.3 Chặn API**: `Api/Middlewares/MustChangePasswordMiddleware` chạy **sau
  Authentication**, trước business endpoint. Whitelist bằng **metadata**
  (`[AllowWhenPasswordChangeRequired]` trên `me`/`logout`/`change-password` +
  các endpoint `[AllowAnonymous]`) — **không** match theo chuỗi URL nên không thể
  bypass bằng path chứa "password". Bị chặn → `403` + `errorCode = PASSWORD_CHANGE_REQUIRED`
  (`Shared/Constants/AuthErrorCodes.cs`), theo đúng `ErrorResponse` chung.
- **T1a.4 ChangePassword**: `ChangePasswordAsync` set `PasswordHash` +
  `MustChangePassword = false` + `PasswordUpdatedAt = now` trong **cùng một
  SaveChanges** (atomic). `ResetPasswordAsync` và `RegisterAsync` cũng cập nhật
  `PasswordUpdatedAt`.

> Middleware là **no-op khi JWT đang tắt** (`TAM-TAT-LOGIN`): không có principal
> nên không chặn gì. Khi bật lại auth, middleware nằm ngay sau `UseAuthentication()`
> và hoạt động đầy đủ. Trade-off truy vấn: mỗi request đã-đăng-nhập, không thuộc
> whitelist sẽ query 1 cột `must_change_password` (Option A — đơn giản, chính xác;
> chưa cache/claim để tránh token stale).

### BE 1.4a,b,c — Giới hạn đăng nhập sai + khóa tài khoản (Redis + PostgreSQL)

Chặn brute-force theo tầng: **RateLimiter** (chống flood) → **Redis counter**
(đếm sai theo `username + IP`) → **PostgreSQL `lockout_until`** (khóa persistent).

- **Config** (`appsettings` mục `Login` → `Application/Options/LoginSecurityOptions`,
  validate `ValidateOnStart`): `MaxFailed=5`, `WindowMinutes=5`, `LockMinutes=15`,
  `RedisKeyPrefix="tln:auth:login-failed"`, `RateLimit*`. Config sai (≤0) → app
  **không khởi động** (không âm thầm tắt bảo vệ).
- **Redis counter** (`Infrastructure/Identity/RedisLoginAttemptService`, dùng lại
  `IConnectionMultiplexer` — **không** tạo connection mới): key
  `{prefix}:{username}:{ip}`; **atomic** Lua `INCR` + `EXPIRE`-khi-lần-đầu →
  **fixed window** (TTL không reset mỗi lần sai). Redis lỗi → trả
  `LoginAttemptOutcome.Unavailable` (**fail-closed**, không bao giờ coi là "0 lần sai").
- **Login flow** (`AuthenticationService.LoginAsync`): (1) check `lockout_until`
  **trước khi** verify mật khẩu (mật khẩu đúng cũng không bypass được khóa); (2)
  sai → `INCR` counter, tới ngưỡng thì set `lockout_until` **monotonic** (không rút
  ngắn khóa đang có) + persist Postgres, trả `423 Locked`; chưa tới ngưỡng trả `401`
  + số lần còn lại; (3) đúng → `DEL` counter + reset `failed_login_count`/`lockout_until`.
  Verify **decoy hash** khi user không tồn tại/disabled để giảm rò rỉ timing (chống
  username enumeration). Response giống hệt nhau cho user tồn tại/không tồn tại.
- **Client IP**: `RemoteIpAddress` (không tin `X-Forwarded-For`/`X-Real-IP` vì chưa
  cấu hình trusted proxy) — reuse `AuthController.ClientIp()`.
- **Error / HTTP**: `423 Locked` + `Retry-After` (thô = `LockMinutes`, không lộ thời
  gian còn lại chính xác); `429` khi vượt RateLimiter; giữ nguyên `ApiResponse`.
- **RateLimiter** (`ApiServiceExtensions.AddLoginRateLimiter`, policy `auth-login`):
  fixed-window theo IP, **chỉ áp cho endpoint `/auth/login`** — **không** áp global,
  không đụng SignalR / realtime / PLC / monitoring.
- **`failed_login_count` vs Redis**: Redis = bộ đếm **tạm** trong cửa sổ (live);
  `scada.users.failed_login_count` = metadata **persistent** (chỉ ghi khi đạt ngưỡng),
  không ghi Postgres mỗi lần sai.

> **Refresh token & lockout**: refresh-token flow hiện **chưa** check `lockout_until`
> (token cấp trước khi khóa vẫn refresh được access token mới), nhưng middleware
> nghiệp vụ + JWT ngắn hạn vẫn ràng buộc; đây là *finding* đã ghi nhận, chưa revoke
> theo scope hiện tại.
> **Multi-instance**: `InMemoryBackgroundJobService`/RateLimiter là **process-local**;
> Redis counter + lockout Postgres là **shared** nên đếm/khóa vẫn đúng khi scale ngang.

### BE 2.1a,b — Idle timeout + Auto logout (config + VERIFY, không rewrite)

Idle timeout là **client-side detection + server-side authoritative logout**. BE
**không** track activity phía server (không `LastActivityAt`, không BackgroundService
scan, không refresh Redis TTL mỗi request) — theo đúng phân định FE/BE.

- **Config** (`scada.app_settings`, seed idempotent `scripts/seed_session_idle_timeout.sql`,
  `ON CONFLICT (setting_key) DO NOTHING` — không duplicate/overwrite):
  `session.idleTimeoutMinutes = 10` (`data_type=int`).
- **API cho FE** (đã có sẵn, **không** tạo mới): `GET /api/v1/app-settings/by-key/{settingKey}`
  → `AppSettingsController.GetByKey` → `GetByKeyAsync` (match **case-sensitive**, trả
  `AppSettingDto {key,value,...}`, `404` nếu thiếu). FE gọi được sau login (endpoint
  không giới hạn Admin).
- **Validation** (`Application/Common/SessionIdleTimeoutPolicy`): value không hợp lệ
  (`0`/âm/null/không phải int/overflow) → **không** biến âm thầm thành 0 hay vô hạn,
  mà fallback `DefaultMinutes=10` + báo invalid. Idle timer thực thi ở FE.
- **Logout đã VERIFY đúng — không sửa** (`AuthenticationService.LogoutAsync`):
  revoke refresh token (`RevokedAt=now`, persist) → refresh sau logout bị chặn vì
  `ScadaRefreshToken.IsActive => RevokedAt is null && ExpiresAt > now`; release Redis
  session (`RedisConcurrentSessionService.ReleaseAsync`, Lua `ZREM`+`DEL` → **idempotent**,
  giải phóng concurrent slot). Logout `[AllowAnonymous]`, lấy refresh token từ body →
  **logout được cả khi access token đã hết hạn** (đúng cho FE idle lâu).
- **Admin vs Normal**: idle timeout là business rule của FE (Admin không áp); BE
  logout hoạt động giống nhau cho mọi role.

> **Findings (không sửa theo scope)**: (1) `RefreshAsync` chưa check `LockoutUntil`
> (finding từ BE 1.4). (2) `ReleaseAsync` nuốt lỗi Redis (log rồi bỏ qua) → nếu release
> thất bại, session lingers tới hết TTL, nhưng refresh token đã revoke nên **không** tạo
> bypass. Không thêm distributed transaction.

### BE 3.1a — System Audit Log (Login / Export / API-System Error)

Ghi vết "ai làm gì, khi nào, từ đâu, trên API nào, kết quả ra sao" cho **3 nhóm sự
kiện**: Đăng nhập (success/failed/locked), Xuất dữ liệu (export), và Lỗi API/hệ thống.
Tách bạch với **application log** (Serilog — kỹ thuật) và với **entity-change trail**
(`app.AuditLog` cũ, dạng Guid, hiện chưa dùng).

- **Bảng riêng** `scada.system_audit_logs` (entity `SystemAuditLog : ScadaEntity` —
  `long Id`, `timestamptz`, snake_case). Cột chính: `action`, `event_type`, `status`,
  `user_id`, `user_name`, `description`, `module`, `endpoint`, `http_method`,
  `http_status_code`, `ip_address`, `user_agent`, `entity_type`, `entity_id`,
  `correlation_id`, `additional_data (jsonb)`, `created_at`. Index: `created_at`,
  `event_type`, `action`, `status`, `user_name`, `correlation_id`.
- **Enum ổn định (mở rộng được)**: `AuditEventType {Authentication, DataExport,
  ApiError, SystemError}`, `AuditStatus {Success, Failed}`; `Action` là chuỗi trong
  `AuditActionNames` (lưu enum dạng string → thêm member không cần đổi schema).
- **Writer fail-safe** (`SystemAuditService`, singleton): ghi trên **DbContext scope
  độc lập** (`IServiceScopeFactory`) nên audit của error path vẫn ghi được dù transaction
  của request đã hỏng; **không bao giờ throw** — lỗi audit chỉ log ra Serilog, không phá
  business flow (§15). Tự enrich `ip/userAgent/endpoint/method/correlationId/actor` từ
  `HttpContext` + `ICurrentUserService`; **actor không lấy từ request body**.
- **Ownership (không double-log)**: Authentication → ghi tại `AuthenticationService.LoginAsync`;
  DataExport → ghi tại endpoint export (`UsersController.Export`); API/System error → ghi
  **một chỗ duy nhất** tại `ExceptionHandlingMiddleware`.
- **Chỉ audit lỗi đáng ghi** (`AuditErrorPolicy`, §4): `401/403` → `ApiError`,
  `5xx` → `SystemError`. Không audit `400/404/409/429` (noise). 5xx chỉ lưu **tên
  exception**, không lưu message thô/stack trace vào DB.
- **Không lộ bí mật** (`AuditSanitizer`): redact key nhạy cảm trong `additional_data`
  (password/token/jwt/authorization/secret/connectionString/cookie…), truncate text.
  Login failed dùng description generic → **không lộ username tồn tại hay không**.
- **CorrelationId**: dùng lại `X-Correlation-Id` (từ `CorrelationIdMiddleware`) → nối
  audit với application log + response header.
- **Query API (Admin)**: `GET /api/v1/system-audit-logs` + `GET .../{id}`
  (`SystemAuditLogsController` → `ISystemAuditLogQueryService`). Lọc `fromUtc/toUtc/
  userName/eventType/action/status/httpStatusCode`, phân trang, **sort whitelist** (mặc
  định `timestamp` DESC — chống SQL injection qua sortBy), `AsNoTracking`, DTO không lộ
  secret. `[Authorize(Roles=Admin,SuperAdmin)]` đang comment theo `TAM-TAT-LOGIN`.
- **Async/performance**: ghi **đồng bộ** cho các sự kiện tần suất thấp (login/export/error)
  để đảm bảo **durability** (không mất sự kiện bảo mật); không fire-and-forget. Có thể
  nâng cấp lên bounded background queue nếu throughput tăng.
- **Migration**: `AddSystemAuditLog` (đã apply dev DB). **UI** (list/filter/detail) thuộc
  FE — ngoài scope "chỉnh BE".

### T4 — Security audit events + Input validation/sanitization

Mở rộng hệ audit 3.1a (reuse `SystemAuditService`/`SystemAuditLog`/`SystemAuditLogsController`,
**không** tạo service/controller/enum-int mới; **không** hồi sinh `app.AuditLog` cũ vì thiếu
field EventType/Status/Endpoint…).

- **T4.2 Action constants** (`AuditActionNames`, dạng string → không reorder enum persisted):
  thêm `LoginFailed`, `AccountLocked`, `PasswordChanged`, `ConfigurationChanged`.
- **T4.1 Security events** (ghi tại `AuthenticationService`):
  - `Login`/Success; `LoginFailed`/Failed (mọi lần sai, description generic
    `Invalid username or password` — không account enumeration).
  - `AccountLocked`/Failed **đúng 1 lần cho mỗi lockout event** (ghi tại đúng nơi account
    bị khóa thật; lần đăng nhập sau khi đã khóa → `LoginFailed`, không lặp AccountLocked;
    username không tồn tại → `LoginFailed`, không giả lock).
  - `PasswordChanged`/Success tại `ChangePasswordAsync` + `ResetPasswordAsync` (actor lấy từ
    context/reset-token record). **Không bao giờ** ghi old/new password, hash, salt, token.
  - `ConfigurationChanged`: đã có constant + cơ chế sanitize sẵn (`AuditSanitizer` mask
    JwtSecret/ConnectionString/ApiKey…), **nhưng app-settings hiện READ-ONLY** (không có
    endpoint ghi) → chưa có call site để wire; sẽ gắn khi có endpoint sửa config (không tạo
    endpoint mới trong scope T4).
- **T4.3 Query API** (`GET /api/v1/system-audit-logs`, đã có từ 3.1a): thêm filter `userId`;
  đã có `fromUtc/toUtc/userName/eventType/action/status/httpStatusCode`, phân trang
  (PageSize clamp `MaxPageSize`), **sort whitelist** (mặc định `timestamp` DESC), DTO không
  lộ secret, `[Authorize(Roles=Admin,SuperAdmin)]` (đang comment `TAM-TAT-LOGIN`).
- **T4.4 Validation/Sanitization**:
  - Login: `Username` NotEmpty + MaxLength(100); `Password` NotEmpty + **MaxLength(128)**
    (chống DoS hash Argon2) — **không** trim/không complexity trên login (giữ nguyên byte).
  - ChangePassword: `NewPassword` complexity + Min/Max length + **≠ CurrentPassword**
    (message không chứa password).
  - Audit search (`SystemAuditLogQueryValidator`, chạy qua `ValidationFilter` cho `[FromQuery]`):
    `FromUtc ≤ ToUtc` (§14) → 400; `EventType/Status` là enum (400 nếu sai); `SortBy` whitelist;
    bound length `userName/action`.
  - SQLi: toàn bộ query bằng EF LINQ parameterized + sort whitelist (không nối chuỗi SQL).
  - XSS: `additional_data` được redact secret + là plain-text lưu trữ; **output-encoding là
    trách nhiệm FE** (không render raw HTML).
- **Verify (live, dev DB)**: 4×`LoginFailed` → lần 5 `AccountLocked` (1 lần) → lần 6 `LoginFailed`;
  không có password trong DB. Password-change audit: code-verified (auth đang tắt nên chưa
  live-test được).

### T5 — Free-text validation + HTML sanitization helper

- **Phân loại**: Identifier (`Username`) whitelist `A-Za-z0-9._-`; Display name
  (`DisplayName`/`FirstName`/`LastName`/`Role.Name`) Unicode + max length + **không**
  control char; Free-text (`Description`/`Reason`/`Keyword`) Unicode, cho phép `\n\r\t`,
  max = cột DB (`ValidationConstants`). **Không** field API nào là rich-text HTML.
- **Validation** tại FluentValidation (`ValidationFilter` → 400), độ dài ≤ cột EF
  (`username` 100, `display_name` 150, `description` 500, …). Không `.Trim()` password.
- **SanitizeHtml** (`StringExtensions`, HtmlSanitizer 9.2.1039, allowlist
  `p/br/strong/em/b/i/ul/ol/li`) — **có sẵn nhưng không gắn vào field plain-text**
  (tránh biến `x < 10` thành HTML). XSS của Description/Name: **output encoding ở FE**.
- **FE map**: `parseDescription` parse KMZ thành React text (encode sẵn); chặn
  `javascript:` / `data:` (trừ `data:image` raster) khi gán `img src`.
- **SQL**: không `FromSqlRaw`; sort audit whitelist / `ApplySorting` dùng `GetProperty`
  (không nối SQL).

### Token architecture (Access / Refresh / Reset / Session)

Tách 4 khái niệm — **không** dùng JWT `exp` làm idle timeout (idle = FE 10 phút → `POST /logout`).

| Token | Lifetime | Storage | One-time |
|---|---|---|---|
| Access JWT | 15 phút | Client only | N/A (hết hạn tự nhiên, không blacklist) |
| Refresh | 7 ngày | SHA-256 hash trong `scada.refresh_tokens` | Rotate: `ExecuteUpdate` atomic; reuse token đã revoke → revoke **cả family** + audit `RefreshTokenReuse` |
| Password reset | 30 phút | SHA-256, `UsedAt` | Có |
| Session Redis | TTL ≈ refresh | `tln:concurrent` | Logout/`ReleaseAsync`; Admin không chiếm slot normal (tối đa 9) |

- **SigningKey**: không còn placeholder trong `appsettings.json` (prod phải set `Jwt__SigningKey`). `JwtSettings.Validate` + `ValidateOnStart` reject key ngắn/`CHANGE_ME`/`secret`. Dev key chỉ trong `appsettings.Development.json`.
- **Logout** ghi audit `Logout`. Change/reset password vẫn `RevokeAllRefreshTokensAndSessionsAsync`.
- **Claims JWT**: `sub/jti/iat/username/role/sid` — không password, không permission (SCADA role-only). `TAM-TAT-LOGIN` vẫn tắt `UseAuthentication` cho đến khi deploy.

## 2. Trách nhiệm từng tầng (quy tắc phụ thuộc)

```
Api  ─────────────►  Application  ─────────────►  Domain  ─────────────►  Shared
 │                         ▲                          ▲                      ▲
 └────────► Infrastructure ┘                          │                      │
                  └──────────────────────────────────┴──────────────────────┘
```

* **Shared** không tham chiếu project nào. Đây là điều kiện để Domain dùng được
  nó mà không phá quy tắc phụ thuộc.
* **Domain** chỉ phụ thuộc **Shared**. Không EF Core, không Dapper, không
  Infrastructure. Toàn bộ quy tắc nghiệp vụ nằm ở đây.
* **Application** phụ thuộc **Domain** + **Shared**. Nó *khai báo interface* cho
  mọi thứ cần từ bên ngoài (repository, token, current user, cache, file
  storage, Dapper query) nhưng không bao giờ tự implement.
* **Infrastructure** phụ thuộc **Application** + **Domain** + **Shared** và
  implement mọi interface mà Application đã khai báo.
* **Api** phụ thuộc tất cả, nhưng chỉ để *lắp ráp* (`Program.cs`) và expose HTTP.
  Không chứa nghiệp vụ.

Đây chính là lý do kiến trúc thay thế được: đổi PostgreSQL sang SQL Server, đổi
REST sang gRPC, đổi JWT sang IdP bên ngoài — chỉ **Infrastructure**/**Api** đổi,
**Domain** và **Application** giữ nguyên.

## 3. Vì sao vừa EF Core vừa Dapper? (CQRS-lite)

| | EF Core | Dapper |
|---|---|---|
| Dùng cho | Create, Update, Delete, Migration, Transaction, Change Tracking | Dashboard, báo cáo, join phức tạp, stored procedure, query hiệu năng cao |
| Sở hữu | `ApplicationDbContext` (context EF **duy nhất**) | `DapperContext` (tạo `NpgsqlConnection` thô) |
| Repository | `IGenericRepository<T>` / `IUserRepository` | `IDapperRepository` + interface chuyên biệt như `IUserReportQueries` |
| Transaction | Có (qua `IUnitOfWork`) | Không — query ngắn, chỉ đọc, mỗi lần một connection |
| Mô hình dữ liệu | Entity giàu hành vi (`User`, `Role`...) | DTO phẳng (`UserSummaryReportDto`...) |

**Vì sao không dùng EF Core cho tất cả?**

1. **Change tracking rất đắt** với query dashboard join nhiều bảng trả về dữ
   liệu phẳng — Dapper map thẳng vào DTO, không tốn overhead đó.
2. **LINQ translator của EF Core không sinh được SQL tối ưu thủ công theo
   index** hay stored procedure — Dapper cho phép dán nguyên câu SQL mà DBA đã
   tối ưu.
3. **Tách ra để loại bỏ cám dỗ sửa dữ liệu ngoài domain model.** Mọi thao tác
   ghi BẮT BUỘC đi qua aggregate root + `IUnitOfWork`, nên các invariant
   (`User.RecordLoginFailure`, `Role.RevokePermission`...) không thể bị đi
   đường tắt. `IDapperRepository` cố ý không có helper Insert/Update/Delete —
   chỉ có `QueryAsync`/`QuerySingleAsync`/`ExecuteAsync` cho đọc và stored
   procedure.

## 4. Luồng transaction

Một request ghi điển hình (`POST /api/v1/users`):

```
Controller
  -> IUserService.CreateAsync(dto)                 [Application]
       -> User.Create(...)                         [Domain: validate + raise UserCreatedEvent]
       -> unitOfWork.Users.CreateAsync(user)       [chỉ stage insert, chưa có SQL]
       -> unitOfWork.SaveChangesAsync()            [Infrastructure]
            -> ApplicationDbContext.SaveChangesAsync (override):
                 1. ApplyAuditInformation()          - đóng dấu CreatedDate/CreatedBy
                 2. ConvertHardDeletesToSoftDeletes() - Delete() thành IsDeleted = true
                 3. base.SaveChangesAsync()           - MỘT transaction cho mọi thay đổi
                 4. DispatchDomainEventsAsync(...)    - chỉ chạy sau khi commit thành công
```

Mọi thay đổi stage trên `ApplicationDbContext` (scope theo request, inject một
lần vào `IUnitOfWork` và mọi repository của request đó) được commit **nguyên
khối** trong một lần `SaveChangesAsync`.

Khi một nghiệp vụ phải gọi `SaveChangesAsync` nhiều lần (ví dụ tạo Order + tạo
OrderDetail + trừ tồn kho), dùng `ExecuteInTransactionAsync` thay vì tự gọi
begin/commit/rollback — quên rollback trong khối catch là cách làm rò rỉ
transaction và row lock suốt phần còn lại của request:

```csharp
await unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    await unitOfWork.Orders.CreateAsync(order, ct);
    await unitOfWork.SaveChangesAsync(ct);

    inventory.Reserve(order.Lines);
    await unitOfWork.SaveChangesAsync(ct);

    await auditService.CreateAuditLogAsync(new CreateAuditLogDto
    {
        EntityName = nameof(Order),
        EntityId = order.Id.ToString(),
        Action = AuditAction.Created
    }, ct);
}, cancellationToken);
```

Nếu đã có transaction bên ngoài thì lời gọi lồng nhau sẽ *join* vào transaction
đó chứ không tạo transaction con: EF Core không hỗ trợ nested transaction, và
commit ngầm ở bên trong sẽ phá vỡ đảm bảo all-or-nothing mà caller đang trông
đợi.

## 5. Luồng repository

```
Controller → IUserService → IUnitOfWork.Users (IUserRepository)
                                   │
                                   ▼
                 SoftDeleteRepository<User> : GenericRepository<User>
                                   │
                                   ▼
                        ApplicationDbContext (DbSet<User>)
```

* Controller không bao giờ thấy `IUnitOfWork` hay `DbContext`, chỉ thấy
  `IUserService`/`IAuthService`/`IRoleService`.
* Service không viết LINQ thô trên `DbSet` — gọi method của repository, hoặc
  dựng `ISpecification<T>` khi query đủ phức tạp/dùng lại nhiều lần.
* Repository không chứa quy tắc nghiệp vụ, chỉ chứa hình dạng truy cập dữ liệu
  (include, filter, order). Quy tắc nghiệp vụ nằm trên entity.

### Các method dùng lại được

`IGenericRepository<T>` (ràng buộc `T : BaseEntity<Guid>`):

| Nhóm | Method |
|---|---|
| Đọc | `GetByIdAsync`, `GetAllAsync`, `FindAsync(predicate)`, `FirstOrDefaultAsync`, `ListAsync(spec)`, `GetPagedAsync` |
| Đếm/kiểm tra | `CountAsync` (3 overload), `AnyAsync`, `ExistsAsync(id)` |
| Ghi | `CreateAsync`, `CreateRangeAsync`, `Update`, `Delete`, `DeleteRange`, `DeleteAsync(id)` |

`ISoftDeleteRepository<T>` (ràng buộc thêm `ISoftDelete`) bổ sung
`SoftDeleteAsync`, `RestoreAsync`, `GetDeletedAsync`.

Hai điểm thiết kế cố ý:

* **Chỉ có một `FindAsync(predicate)`**, không có thêm `FilterAsync` /
  `GetByConditionAsync`: cả ba đều biên dịch ra cùng một `Where()`, ba cái tên
  cho một hành vi là cách repository mục ruỗng theo thời gian.
* **`Update`/`Delete` là đồng bộ.** Đánh dấu một entity đang được track là
  thao tác thuần trong bộ nhớ; trả về `Task` sẽ ám chỉ có I/O không hề tồn tại.
  Riêng `DeleteAsync(id)` là async thật vì nó phải query DB để tìm bản ghi.
* **Soft delete tách thành interface riêng** thay vì gắn vào mọi repository, để
  "restore bản ghi này" là **lỗi biên dịch** với entity xoá cứng, chứ không phải
  lỗi lúc chạy.

`SearchAsync` không nằm ở repository generic vì repository generic không thể
biết cột nào của `T` là tìm kiếm được. Nó nằm ở repository cụ thể —
`IUserRepository.SearchAsync(UserSearchQuery)` lọc theo keyword (email, họ,
tên), status, role, khoảng CreatedDate, và toàn bộ được đẩy xuống SQL.

## 6. Phân trang, sắp xếp, lọc

`PaginationRequest` (bind trực tiếp từ query string):

| Thuộc tính | Ý nghĩa |
|---|---|
| `PageNumber`, `PageSize` | `PageSize` bị chặn trần bởi `ApplicationConstants.MaxPageSize` để client không thể xin cả bảng |
| `Keyword` | Từ khoá tự do; repository quyết định match cột nào |
| `Filters` | Dictionary "field → value" cho lọc bằng nhau |
| `SortBy` + `SortDirection` | Sắp xếp một cấp |
| `Sorts` | Sắp xếp nhiều cấp ("Status ASC, CreatedDate DESC") |

`PaginationResult<T>` trả về `Items`, `PageNumber`, `PageSize`, `TotalCount`,
`TotalPages`, `HasNextPage`, `HasPreviousPage`, cùng `Map()` để chuyển entity
sang DTO mà vẫn giữ nguyên metadata phân trang.

`QueryableExtensions.ApplySorting` sinh `OrderBy` cho field đầu tiên và `ThenBy`
cho các field sau, tất cả đều dịch được sang SQL. Field không tồn tại thì bị bỏ
qua chứ không ném exception, để một client cũ không làm chết endpoint danh sách.

Ví dụ: `GET /api/v1/users?keyword=nguyen&status=Active&sortBy=CreatedDate&sortDirection=Descending&pageSize=50`

## 7. Xác thực & phân quyền

* **JWT access token** (mặc định 15 phút) ký HMAC-SHA256, mang claim `role` và
  `permission` (xem `JwtTokenService`).
* **Refresh token** là chuỗi ngẫu nhiên, lưu phía server trong aggregate `User`
  (entity con `RefreshToken`) nên thu hồi/xoay vòng được.
  `AuthService.RefreshTokenAsync` xoay vòng: mỗi lần dùng sẽ vô hiệu token cũ và
  cấp token mới.
* **Phân quyền theo role**: `[Authorize(Policy = PolicyNames.RequireAdmin)]`.
* **Phân quyền theo permission**: `[Authorize(Policy = Permissions.Users.Create)]`
  — mỗi hằng `Permissions.*` là một policy dựa trên claim, đăng ký trong
  `AuthenticationServiceExtensions.AddJwtAuthentication`.
* **Khoá tài khoản** (5 lần sai → khoá 15 phút) được enforce hoàn toàn bên
  trong aggregate `User` (`RecordLoginFailure`/`IsLockedOut`), không nằm ở auth
  service, nên không code path nào đi vòng qua được.
* `LogoutAsync` thu hồi *toàn bộ* refresh token của user. Access token đã cấp
  vẫn sống đến khi hết hạn — đó là bản chất của JWT stateless, và cũng là lý do
  vòng đời access token phải ngắn.
* `ResetPasswordAsync` là reset kiểu quản trị (đặt mật khẩu mới cho tài khoản
  khác rồi huỷ hết session của nó). Luồng "quên mật khẩu" tự phục vụ cần token
  gửi qua email dùng một lần — phần token store và email sender được cố ý để
  trống làm điểm mở rộng thay vì làm dở dang.

## 8. Audit

Hệ thống có hai lớp audit, phục vụ hai câu hỏi khác nhau:

1. **Cột audit tự động**: `CreatedDate`, `CreatedBy`, `ModifiedDate`,
   `ModifiedBy` do `ApplicationDbContext.SaveChangesAsync` tự đóng dấu cho mọi
   entity. Trả lời "ai chạm vào bản ghi này gần nhất".
2. **`IAuditService` + bảng `AuditLogs`**: trả lời "chính xác cái gì đã đổi,
   trước đó trông thế nào", và ghi cả những sự kiện không gắn với bản ghi nào
   (đăng nhập, export...).

`AuditLog` kế thừa `BaseEntity` chứ không phải `AuditableEntity`: một nhật ký
audit mà tự nó sửa hoặc xoá mềm được thì vô giá trị.

Entry được stage trên **cùng DbContext** với thay đổi nghiệp vụ, nên nó commit
hoặc rollback cùng nhau — một dòng audit sống sót sau transaction thất bại là
một lời nói dối.

Đọc qua `GET /api/v1/audit-logs` (chỉ Admin). Không có endpoint ghi: audit entry
do service tạo trong transaction, không phải do client gọi API.

## 9. Mối quan tâm xuyên suốt (cross-cutting)

* **Xử lý exception tập trung** (`ExceptionHandlingMiddleware`) map cây
  `Backend.Shared.Exceptions` sang status code tương ứng: `NotFoundException`
  → 404, `ValidationException` → 400, `UnauthorizedException` → 401,
  `ForbiddenException` → 403, `BusinessException` (bao gồm `DomainException`)
  → 400, còn lại → 500. Body luôn là `ErrorResponse` thống nhất.
* **Validation** (`ValidationFilter`) chạy validator FluentValidation tương ứng
  cho mọi tham số action trước khi vào controller.
* **Correlation Id** (`CorrelationIdMiddleware`) đọc/sinh `X-Correlation-Id`,
  trả lại trong response, và đẩy vào `LogContext` của Serilog để mọi dòng log
  của một request đều truy vết được.
* **Performance** (`PerformanceMiddleware`) ghi cảnh báo cho request chậm hơn
  1 giây.
* **Serilog** ghi ra Console + file xoay vòng theo ngày (`Logs/log-.txt`), có
  enrich `CorrelationId`, thread id và tên môi trường.
* **Kiểm tra DI lúc khởi động**: ở môi trường Development, `Program.cs` bật
  `ValidateOnBuild` + `ValidateScopes` để lỗi thiếu đăng ký hoặc singleton ôm
  scoped service (ví dụ ôm `DbContext`) nổ ngay lúc start, thay vì nổ ở request
  đầu tiên chạm vào đúng nhánh hỏng.

## 10. Result Pattern, Exception và API Response

* `Result` / `Result<T>` (`Shared.Results`) — dùng **bên trong** Application để
  các thất bại *dự kiến được* (validation, không tìm thấy, trùng dữ liệu) không
  phải ném exception làm luồng điều khiển.
* `Backend.Shared.Exceptions` — dùng cho tình huống thật sự ngoại lệ, hoặc cho
  thất bại phát sinh sâu trong Domain nơi việc trả `Result` ngược lên từng tầng
  là không thực tế. Mọi exception đều có `Message`, `ErrorCode`,
  `AdditionalData`. `DomainException` kế thừa `BusinessException` nên Domain
  vẫn chỉ cần phụ thuộc `Shared`.
* `ApiResponse<T>` (`Shared.Responses`) — vỏ bọc mà mọi controller trả về:
  `{ success, message, data, errors, traceId, timestampUtc }`.
* `PaginationResult<T>` (`Shared.Pagination`) — kết quả cho endpoint danh sách.
* `ErrorResponse` (`Shared.Responses`) — chỉ do `ExceptionHandlingMiddleware`
  sinh ra.

## 11. Service dùng chung

Interface nằm ở Application, implementation nằm ở Infrastructure — đổi
implementation chỉ là đổi một dòng đăng ký DI.

| Interface | Mặc định | Thay thế được bằng |
|---|---|---|
| `ICacheService` | `MemoryCacheService` | Redis / IDistributedCache |
| `INotificationService` | `LoggingNotificationService` | SignalR, SMTP, FCM |
| `IBackgroundJobService` | `InMemoryBackgroundJobService` + hosted processor | Hangfire, Quartz |
| `IFileStorageService` | `LocalFileStorageService` | Azure Blob, MinIO/S3 |
| `IReportService` | `ReportService` (Dapper + ClosedXML) | Engine báo cáo khác |
| `IImportExportService` | `ExcelImportExportService` (ClosedXML) | — |
| `ISqlConnectionFactory` | `SqlConnectionFactory` (Npgsql) | Provider khác |

Vài lưu ý quan trọng khi dùng:

* **Background job nhận `Func<IServiceProvider, CancellationToken, Task>`**, không
  phải closure. Job chạy sau khi request scope đã dispose, nên nó phải tự resolve
  service scoped (DbContext, repository) từ provider được truyền vào. Capture
  sẵn vào closure chính là nguồn gốc kinh điển của lỗi "Cannot access a disposed
  context".
* **Job queue in-process sẽ mất job khi restart.** Module nào cần đảm bảo không
  mất job thì chuyển sang Hangfire.
* **`ImportExcelAsync` không ghi database.** Nó parse, validate, gom lỗi theo
  từng dòng vào `ImportResult.Errors` (một ô sai không làm hỏng cả file) rồi trả
  về; việc lưu là của caller qua `IUnitOfWork`.
* **`ExportPdfAsync` ném `NotSupportedException`.** Template cố ý không kèm PDF
  engine (license, dung lượng). Đăng ký một implementation `IReportService` có
  PDF (QuestPDF/iText) khi thực sự cần.

### 11.1. Export Excel với header và mapping tự khai báo

`ExportExcelAsync(rows, sheetName)` lấy header từ tên property, hợp cho file kỹ
thuật cần import lại. Với file gửi người dùng cuối, dùng overload nhận
`ExcelColumns<T>` (`Shared/Models/ExcelColumn.cs`) để tự quyết định tiêu đề, thứ
tự cột, cách lấy giá trị và định dạng ngày/số:

```csharp
var columns = new ExcelColumns<UserDto>
{
    // { Tiêu đề, hàm lấy giá trị, [định dạng], [độ rộng] }
    { "Email",              user => user.Email },
    { "Họ tên",             user => $"{user.FirstName} {user.LastName}".Trim() },
    { "Vai trò",            user => string.Join(", ", user.Roles) },
    { "Ngày tạo",           user => user.CreatedDate, ApplicationConstants.ExcelDateTimeFormat },
    { "Doanh thu",          user => user.Revenue, "#,##0.00", width: 18 }
};

byte[] file = await importExportService.ExportExcelAsync(rows, columns, "Users", ct);
```

Dòng tiêu đề được in đậm, đóng băng và bật auto-filter; cột không khai báo
`width` sẽ tự canh theo nội dung (chỉ với file dưới 1.000 dòng, vì auto-fit trên
file lớn tốn hơn phần dễ đọc mà nó mang lại). Giá trị `null` để trống ô.

Overload thứ ba ghi thẳng vào một `Stream` thay vì trả `byte[]`, dùng khi lưu ra
đĩa hoặc đẩy lên `IFileStorageService` mà không muốn giữ cả file trong RAM hai
lần:

```csharp
await using var stream = File.Create(path);
await importExportService.ExportExcelAsync(rows, columns, stream, "Users", ct);
```

Ví dụ hoàn chỉnh (service dựng cột + controller trả file) ở
`UserService.ExportExcelAsync` và `GET /api/v1/users/export`. Service đọc kết
quả theo từng trang và dừng ở `ApplicationConstants.MaxExportRows` (50.000
dòng); vượt ngưỡng đó nên đẩy sang background job rồi gửi link tải thay vì bắt
request HTTP chờ.

## 12. SCADA trên PostgreSQL + TimescaleDB

### Đã có sẵn (read API)

- **Entity** trong `Domain/Entities/Scada` và `Domain/Entities/History` (Id `long`,
  `DateTimeOffset`, map snake_case).
- **EF configuration** theo schema `scada` / `history`.
- **Query service** trong `Infrastructure/Scada`:
  - `ScadaMetadataQueryService` — Station, Plc, Device, Tag, config…
  - `HistoryQueryService` — history_1s/1m/30m, alarm, event, activity
- **Controller GET** dưới `Api/Controllers/Scada/`.

History sample **bắt buộc** query `from` + `to` (tránh quét cả hypertable).

Migration `AddScadaEntities` là **baseline no-op** nếu bảng `scada`/`history` đã
tạo sẵn ngoài EF; migration IAM vẫn tạo schema `app.*`.

### Hợp đồng Industrial (chưa implement)

`Application/Interfaces/Industrial` (`IDeviceService`, `ITagService`,
`IAlarmService`, `IEventLogService`) vẫn chỉ là contract realtime/ingest tương
lai — **chưa đăng ký DI**, khác với GET metadata/history ở trên.

## 13. Database & connection string

Trỏ `ConnectionStrings:DefaultConnection` rồi chạy:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

**Development** (`appsettings.Development.json`) đè connection string của
`appsettings.json`. Ví dụ hiện tại:

```text
Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=...
```

Ở `Development`, `Program.cs` tự gọi `MigrateAsync()` lúc khởi động. Production
nên chạy migration qua CI/CD, không tự migrate trong process API.

## 13b. Redis (concurrent login)

API cần Redis tại `localhost:6379` (`ConnectionStrings:Redis`).

**Cách khuyến nghị trên Windows — cài native, không cần Docker:**

```powershell
winget install -e --id taizod1024.redis-windows-fork --source winget
pwsh ./BE/scripts/ensure-redis.ps1
```

Script `ensure-redis.ps1` sẽ:
1. Dùng Redis đang chạy nếu đã có
2. Start `redis-server` native nếu đã cài
3. Tự `winget install` Redis Windows nếu chưa có
4. Chỉ fallback Docker nếu native không được

Chạy API kèm Redis:

```powershell
pwsh ./BE/scripts/start-api.ps1
```

Docker vẫn tùy chọn (`docker compose up -d`). Chi tiết: `docs/Concurrent_Session_Limit.md`.

```bash
dotnet ef migrations add <Ten> --project src/Infrastructure --startup-project src/Api -o Persistence/Migrations
```

## 14. CORS (Frontend)

Trong **Development**:

- Cho phép mọi origin `localhost` / `127.0.0.1` / `[::1]` (mọi port).
- `AllowCredentials` + expose `Token-Expired`, `Content-Disposition`, `X-Correlation-Id`.
- `UseCors` chạy **trước** exception middleware (lỗi 500 vẫn có header CORS).
- Tắt `UseHttpsRedirection` ở Development (tránh preflight OPTIONS bị 307).

FE nên gọi `http://localhost:5140` (profile `http` trong `launchSettings.json`).

Production: cấu hình `Cors:AllowedOrigins` trong appsettings / biến môi trường.

## 15. Khả năng mở rộng (không đụng tầng Application)

Vì Application chỉ phụ thuộc interface do chính nó khai báo, mọi hạng mục dưới
đây thêm được hoàn toàn trong `Infrastructure` (cộng vài dòng trong
`Infrastructure/DependencyInjection.cs`):

| Hạng mục | Cắm vào đâu |
|---|---|
| **Redis** | Implement `ICacheService` trong `Infrastructure/Common/Caching` |
| **SignalR** | Implement `INotificationService` + hub trong `Infrastructure` |
| **Kafka / RabbitMQ** | Thay `DispatchDomainEventsAsync` (hiện là no-op) trong `ApplicationDbContext` bằng publisher thật |
| **Hangfire / Quartz** | Implement `IBackgroundJobService` |
| **MinIO / S3 / Azure Blob** | Implement `IFileStorageService` |
| **PDF** | Implement `IReportService` có `ExportPdfAsync` |
| **SCADA write / ingest** | Implement Industrial contracts + command services |

## 16. Endpoint

> Chi tiết từng DTO/field/service: [`docs/Tong_Quan_API_Service_DTO.md`](docs/Tong_Quan_API_Service_DTO.md)

### IAM / hệ thống

| Method | Route | Quyền (khi bật auth) | Mô tả |
|---|---|---|---|
| POST | `/api/v1/auth/register` | Anonymous | Đăng ký + tự đăng nhập |
| POST | `/api/v1/auth/login` | Anonymous | Đăng nhập |
| POST | `/api/v1/auth/refresh-token` | Anonymous | Xoay vòng refresh token |
| POST | `/api/v1/auth/revoke-token` | Đã đăng nhập | Thu hồi refresh token |
| GET | `/api/v1/users` | `Users.View` | Danh sách user |
| GET | `/api/v1/users/export` | `Users.View` | Xuất Excel |
| GET/POST/PUT/DELETE | `/api/v1/users/...` | theo policy | CRUD + role + activate |
| GET/POST | `/api/v1/roles` | Roles.* | Role |
| GET | `/api/v1/audit-logs` | Admin | Audit trail |
| GET | `/api/v1/reports/users/summary` | `Reports.View` | Dashboard Dapper |
| GET | `/health` | Anonymous | Health (Postgres) |

### SCADA metadata (GET)

| Method | Route | Mô tả |
|---|---|---|
| GET | `/api/v1/stations` · `/api/v1/stations/{id}` | Trạm |
| GET | `/api/v1/plcs` · `/api/v1/plcs/{id}` | PLC |
| GET | `/api/v1/devices` · `/api/v1/devices/{id}` | Thiết bị |
| GET | `/api/v1/tags` · `/api/v1/tags/{id}` | Tag |
| GET | `/api/v1/history-profiles` | Profile ghi history |
| GET | `/api/v1/tag-history-configs` | Tag ↔ profile |
| GET | `/api/v1/communication-configs` | Giao thức |
| GET | `/api/v1/mqtt-configs` | MQTT |
| GET | `/api/v1/scada-users` | User SCADA |
| GET | `/api/v1/app-settings` · `.../by-key/{key}` | Setting |

### History Timescale (GET)

| Method | Route | Mô tả |
|---|---|---|
| GET | `/api/v1/history/1s` · `/1m` · `/30m` | Mẫu theo chu kỳ (`from`+`to` bắt buộc) |
| GET | `/api/v1/alarm-histories` | Lịch sử alarm |
| GET | `/api/v1/event-logs` | Event hệ thống |
| GET | `/api/v1/user-activity-logs` | Activity user SCADA |

> **Dev:** JWT đang tạm tắt (`TAM-TAT-LOGIN` trong `Program.cs` / Controllers).
> Search chuỗi đó để bật lại auth.

## 17. Nguyên tắc đã được áp dụng sẵn

- Aggregate root là lối vào **duy nhất** để thay đổi dữ liệu (entity không có
  public setter) — tránh anemic domain model.
- Value Object (`Email`, `FullName`) làm cho trạng thái sai không biểu diễn được.
- Soft delete và cột audit IAM là tự động
  (`ApplicationDbContext.SaveChangesAsync`), không phải việc của từng service.
- Entity SCADA dùng `ScadaEntity` (`long` + `CreatedAt`/`UpdatedAt`) — tách khỏi
  `BaseEntity<Guid>` của IAM.
- Mỗi tầng có đúng một `DependencyInjection.cs`; `Program.cs` chỉ gọi
  `AddApplication()` / `AddInfrastructure()` / `AddApiServices()`.
- Interface repository ở Application, implementation ở Infrastructure — nên
  Application/Domain unit test được bằng fake in-memory, không cần database.
- Refresh token xoay vòng mỗi lần dùng và thu hồi được, giảm rủi ro replay.
- Không có magic string: role, permission, cache key, giới hạn độ dài field đều
  là hằng trong `Shared/Constants`.
- Mọi cấu hình (JWT signing key, connection string, CORS origin) đến từ
  `appsettings`/biến môi trường — **không** hard-code. Nhớ thay signing key mẫu
  trước khi deploy.
