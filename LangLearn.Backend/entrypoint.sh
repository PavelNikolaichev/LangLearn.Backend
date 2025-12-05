#!/bin/sh
set -e

# If a Docker secret file is provided at /run/secrets/jwt_key, export it to Jwt__Key
if [ -f /run/secrets/jwt_key ]; then
  export Jwt__Key="$(cat /run/secrets/jwt_key)"
fi

# Debug: show whether Jwt__Key is set (do not print the value in real prod)
if [ -z "${Jwt__Key:-}" ]; then
  echo "Warning: Jwt__Key environment variable is not set. The app will fail to start if Jwt:Key is required."
else
  echo "Jwt__Key is set."
fi

# Find the published DLL under /app (handles nested publish folders)
# Use find to locate the first occurrence of LangLearn.Backend.dll
APP_DLL="$(find /app -maxdepth 4 -type f -name 'LangLearn.Backend.dll' -print -quit || true)"
if [ -z "$APP_DLL" ]; then
  echo "ERROR: LangLearn.Backend.dll not found under /app. Listing /app for debugging:" >&2
  ls -la /app || true
  echo "You may be mounting a host directory onto /app which hides the image contents; avoid mounting the repo root to /app." >&2
  echo "If you want to persist the DB, mount a single file to /app/LangLearn.db or mount a separate folder and set ConnectionStrings__DefaultConnection." >&2
  exit 1
fi

echo "Starting application from: $APP_DLL"

# Exec the app (replace the shell with dotnet process)
exec dotnet "$APP_DLL" "$@"
