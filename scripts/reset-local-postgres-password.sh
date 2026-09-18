#!/usr/bin/env bash
set -euo pipefail

# Repairs an existing Aspire PostgreSQL data volume whose postgres password
# was created by an older/generated Aspire configuration.
#
# This deliberately does NOT require the old password. It temporarily adds
# a localhost trust rule to pg_hba.conf, changes the password, then restores
# the original authentication configuration.

container="$(docker ps --format '{{.ID}}\t{{.Names}}' | awk 'tolower($0) ~ /postgres/ { print $1; exit }')"

if [[ -z "${container}" ]]; then
  echo "No running PostgreSQL container found."
  echo "Start Aspire first with: aspire run"
  exit 1
fi

echo "Using PostgreSQL container ${container}..."

pgdata="$(docker exec "${container}" sh -c 'printf "%s" "${PGDATA:-/var/lib/postgresql/data}"')"
hba="${pgdata}/pg_hba.conf"

if ! docker exec "${container}" sh -c "test -f '${hba}'"; then
  echo "Could not find PostgreSQL pg_hba.conf at ${hba}."
  echo "Refusing to modify the database."
  exit 1
fi

backup="${hba}.diaperscout-backup"
trust_rule="local all postgres trust"

echo "Temporarily allowing local postgres authentication..."

docker exec "${container}" sh -c \
  "cp '${hba}' '${backup}' && printf '%s\n' '${trust_rule}' | cat - '${backup}' > '${hba}'"

cleanup() {
  echo "Restoring PostgreSQL authentication configuration..."
  docker exec "${container}" sh -c \
    "if test -f '${backup}'; then cp '${backup}' '${hba}'; rm -f '${backup}'; fi"

  # PostgreSQL accepts SIGHUP to reload pg_hba.conf.
  docker exec "${container}" sh -c \
    "kill -HUP \$(head -n 1 '${pgdata}/postmaster.pid')" >/dev/null 2>&1 || true
}
trap cleanup EXIT

# Reload the temporary trust rule.
docker exec "${container}" sh -c \
  "kill -HUP \$(head -n 1 '${pgdata}/postmaster.pid')"

echo "Changing postgres password to the shared DiaperScout development password..."

docker exec "${container}" psql \
  -U postgres \
  -d postgres \
  -c "ALTER USER postgres PASSWORD 'postgres';"

echo "Password updated successfully."
echo "The original pg_hba.conf will now be restored."

# cleanup runs automatically here.
