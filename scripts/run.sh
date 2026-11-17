#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
if [[ -f .env ]]; then
  set -a
  # Values in .env are simple KEY=VALUE pairs (no shell commands!).
  source .env
  set +a
fi
case "${1:-}" in
  1) p=01-DirectResponses ;;
  2) p=02-ManualToolCalling ;;
  3) p=03-FoundryManagedAgent ;;
  4) p=04-AgentFrameworkWorkflow ;;
  5) p=05-McpClientAgent ;;
  *) echo "Usage: bash scripts/run.sh {1|2|3|4|5}"; exit 2 ;;
esac
dotnet run --project "$p"
