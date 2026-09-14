# Concurrent Session Limit (Redis)

## Responsibility split

| Layer | Responsibility |
|-------|----------------|
| **FE** | Idle timeout 10 phút cho **Normal User**; Admin không idle timeout; gọi `POST /api/v1/auth/logout` khi hết idle |
| **BE** | Login / JWT / concurrent limit / Admin reserved slot / License / acquire & release Redis session |

BE **không** đếm idle, **không** có heartbeat/activity API, **không** lưu `lastActivity`.

## Behavior

- Default: `MaxConcurrentUsers=10`, `ReservedAdminSlots=1` → max **9 normal** + **1 Admin/SuperAdmin**.
- Normal users cannot use the reserved Admin slot.
- Admin login always succeeds up to reserved admin capacity; if admin slots are full, the **oldest admin session** is replaced.
- License rows in `app.system_licenses` can raise `MaxConcurrentUsers` when enabled and within validity.
- Admin CRUD: `GET/POST/PUT/DELETE /api/v1/licenses` (key **masked** on read). Status: `GET /api/v1/licenses/concurrent-users`.
- Redis required; if Redis is down, login fails (fail-safe).

## Config

```json
"ConnectionStrings": { "Redis": "localhost:6379" },
"ConcurrentSession": {
  "DefaultMaxConcurrentUsers": 10,
  "ReservedAdminSlots": 1,
  "SessionTtlSeconds": 0,
  "RedisKeyPrefix": "tln:concurrent"
}
```

`SessionTtlSeconds: 0` → dùng `Jwt:RefreshTokenExpirationDays` (dọn session khi browser crash, **không** thay idle 10 phút của FE).

## Local Redis

```powershell
winget install -e --id taizod1024.redis-windows-fork --source winget
pwsh ./BE/scripts/ensure-redis.ps1
```

## APIs

| Method | Path | Notes |
|--------|------|--------|
| POST | `/api/v1/auth/login` | Acquire Redis slot **before** JWT |
| POST | `/api/v1/auth/logout` | FE idle / manual → release Redis session |
| POST | `/api/v1/auth/refresh` | Extend Redis TTL (không phải idle heartbeat) |
| GET | `/api/v1/auth/concurrent-users` | Active counts + limits |
| GET | `/api/v1/licenses/concurrent-users` | Same |

Login response includes `sessionId` (BE-generated). Logout: gửi `refreshToken` (và optional `sessionId` hint); BE ưu tiên SessionId từ refresh token / JWT claim `sid`.

## Redis keys

- `{prefix}:admin` / `{prefix}:normal` — sorted sets (score = unix expire)
- `{prefix}:session:{sessionId}` — hash metadata

Acquire / extend / release dùng **Lua** (atomic).
