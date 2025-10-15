#!/bin/bash

# Watch script for automatic rebuild on file changes
# This script monitors src/ directory and rebuilds the project when changes are detected

echo "=================================="
echo "Development Watch Mode"
echo "=================================="
echo "Monitoring: src/ directory"
echo "Watching: *.cs, *.axaml, *.csproj files"
echo "Press Ctrl+C to stop"
echo "=================================="
echo ""

# Initial build
echo "[$(date '+%H:%M:%S')] Initial build..."
make build
echo ""

# Watch for changes using inotifywait (more efficient) or fallback to polling
if command -v inotifywait >/dev/null 2>&1; then
    echo "Using inotifywait for file monitoring"
    echo ""

    while true; do
        # Wait for file changes in src/
        inotifywait -r -e modify,create,delete \
            --include '\.(cs|axaml|csproj)$' \
            src/ 2>/dev/null

        echo ""
        echo "[$(date '+%H:%M:%S')] Change detected! Rebuilding..."
        make build
        echo ""
        echo "Watching for changes... (Ctrl+C to stop)"
    done
else
    # Fallback to polling if inotifywait is not available
    echo "Note: Install inotify-tools for better performance (sudo apt-get install inotify-tools)"
    echo "Using polling mode (checks every 2 seconds)"
    echo ""

    # Store initial checksums
    LAST_CHECKSUM=$(find src/ -type f \( -name "*.cs" -o -name "*.axaml" -o -name "*.csproj" \) -exec md5sum {} \; 2>/dev/null | sort | md5sum)

    while true; do
        sleep 2

        # Calculate current checksums
        CURRENT_CHECKSUM=$(find src/ -type f \( -name "*.cs" -o -name "*.axaml" -o -name "*.csproj" \) -exec md5sum {} \; 2>/dev/null | sort | md5sum)

        if [ "$CURRENT_CHECKSUM" != "$LAST_CHECKSUM" ]; then
            echo ""
            echo "[$(date '+%H:%M:%S')] Change detected! Rebuilding..."
            make build
            echo ""
            echo "Watching for changes... (Ctrl+C to stop)"
            LAST_CHECKSUM=$CURRENT_CHECKSUM
        fi
    done
fi
