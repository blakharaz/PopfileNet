#!/bin/bash
set -e

echo "Starting PopfileNet Backend on port 8000..."
dotnet /app/backend/PopfileNet.Backend.dll --urls "http://0.0.0.0:8000" &

echo "Starting PopfileNet UI on port 8001..."
dotnet /app/ui/PopfileNet.Ui.dll --urls "http://0.0.0.0:8001" &
UI_PID=$!

trap "kill $BACKEND_PID $UI_PID" SIGTERM SIGINT

wait
