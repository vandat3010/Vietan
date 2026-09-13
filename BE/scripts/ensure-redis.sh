#!/usr/bin/env bash
# Ensures local Redis is running for WEB_TLN (Docker container tln-redis).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE="${ROOT}/docker-compose.yml"

if [[ ! -f "$COMPOSE" ]]; then
  echo "docker-compose.yml not found at $COMPOSE" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is not installed. Install Docker Desktop / Engine, then re-run." >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker engine is not running. Start Docker, then re-run." >&2
  exit 1
fi

echo "==> WEB_TLN ensure Redis"
docker compose -f "$COMPOSE" up -d redis

deadline=$((SECONDS + 30))
while (( SECONDS < deadline )); do
  status="$(docker inspect -f '{{.State.Health.Status}}' tln-redis 2>/dev/null || true)"
  if [[ "$status" == "healthy" ]]; then
    echo "Redis is ready at localhost:6379 (container: tln-redis)"
    exit 0
  fi
  sleep 0.5
done

echo "Redis started but not healthy within 30s. Check: docker logs tln-redis" >&2
exit 1
