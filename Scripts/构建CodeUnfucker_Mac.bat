#!/bin/bash
echo "Building CodeUnfucker for macOS/Linux..."
cd "$(dirname "$0")/../CodeUnfucker"
dotnet build
