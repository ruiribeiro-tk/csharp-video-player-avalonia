#!/bin/bash

cd /app/src

set -e

echo "==> Restoring dependencies..."
dotnet restore

echo "==> Building application..."
dotnet build -c Release

echo "==> Cleaning publish directory..."
rm -rf /app/publish

echo "==> Publishing application for Windows x64..."
dotnet publish -c Release -r win-x64 --self-contained true -o /app/bin

echo "==> Build complete!"
