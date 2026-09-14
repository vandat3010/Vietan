# Configuration sources (AppSettings / Options / Secrets)

## Source of truth

| Group | Examples | Source | Editable via API |
|-------|----------|--------|------------------|
| Runtime DB | `session.idleTimeoutMinutes` | `app.app_settings` | Yes (Admin) — also `PUT /session-policy` |
| Security options | `Password:*`, `Login:*` | `appsettings` / ENV → `IOptions` | No (deploy config) |
| Infrastructure secrets | JWT signing key, Postgres, Redis | ENV / User Secrets | **Never** in AppSettings DB |
| FE local | password policy UI | localStorage | FE only — not BE SoT |

## APIs

- `GET /api/v1/app-settings/catalog` — definitions (no secrets)
- `PUT /api/v1/app-settings/by-key/{key}` — allowlisted editable keys only
- Arbitrary keys → 400 ValidationError

## Cache

No dedicated AppSettings Redis cache (low volume, Admin reads). Session policy reads EF each time; invalidate is implicit on upsert. Do not treat Redis as config SoT.
