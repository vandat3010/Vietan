-- BE 1.3a / T1a.1 — Force the DEFAULT admin account to change password on first login.
-- Idempotent and TARGETED: only the default account is touched, never a blanket update.
-- Run: psql -h localhost -U postgres -d <db> -f scripts/seed_must_change_password.sql
--
-- Adjust the username below to match your deployment's default account.
-- (Do NOT change existing users' flag in bulk — that is out of scope for 1.3a.)

UPDATE scada.users
SET must_change_password = TRUE,
    updated_at = now()
WHERE username = 'admin'
  AND must_change_password = FALSE;
