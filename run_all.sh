#!/bin/bash

#!/bin/bash

# Exit on any error
set -e

# log file for running the whole application
LOG_FILE="run_all.log" > "$LOG_FILE"


# Function to clean up background processes
cleanup() {
    echo "Error or exit detected. Cleaning up..." >> "../$LOG_FILE"
    [[ -n "$BACKEND_PID" ]] && kill $BACKEND_PID 2>/dev/null
    }

# Register cleanup on script exit or interruption
trap cleanup EXIT

echo "Starting multi-directory command execution..." >> "$LOG_FILE"


echo "Running webapi servers for Bob and Alice..." >> "$LOG_FILE"
cd Networking.WebApi
dotnet build > /dev/null 2>&1
dotnet run --no-build &
BACKEND_PID=$!

# wait for server to start
sleep 10

echo "Running main conversation between Alice and Bob..." >> "../$LOG_FILE"
cd ../Networking.Console
dotnet build > /dev/null 2>&1
dotnet run --no-build

sleep 10

echo "Cleaning up..." >> "../$LOG_FILE" 2>&1
kill $BACKEND_PID

echo "All tasks completed successfully." >> "../$LOG_FILE"