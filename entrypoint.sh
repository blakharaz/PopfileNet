#!/bin/bash
set -e

echo "Starting PopfileNet Backend on port 5000..."
dotnet /app/backend/PopfileNet.Backend.dll --urls "http://0.0.0.0:5000" &
BACKEND_PID=$!

echo "Starting PopfileNet UI on port 5001..."
dotnet /app/ui/PopfileNet.Ui.dll --urls "http://0.0.0.0:5001" &
UI_PID=$!

trap "kill $BACKEND_PID $UI_PID" SIGTERM SIGINT

wait
