#!/usr/bin/env sh
set -eu
COURSE_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPO_ROOT=$(cd "$COURSE_DIR/.." && pwd)
echo "Démarrage du cours local hors ligne..."
exec dotnet run --project "$COURSE_DIR/server/CourseServer.csproj" -- "$REPO_ROOT"
