#!/usr/bin/env bash
# BotPulse - UiPath Tenant Connection Test (bash)
# Run from repository root: bash scripts/test-uipath-tenant.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="${ROOT_DIR}/.env"

echo ""
echo "=================================================="
echo "  BotPulse - UiPath Tenant Connection Test"
echo "=================================================="
echo ""

# --- Load .env file ---
if [[ ! -f "$ENV_FILE" ]]; then
    echo "ERROR: .env file not found at $ENV_FILE"
    echo "Copy .env.example to .env and fill in your credentials."
    exit 1
fi

while IFS= read -r line; do
    # Skip blank lines and comments
    if [[ "$line" =~ ^[[:space:]]*$ || "$line" =~ ^[[:space:]]*# ]]; then
        continue
    fi
    if [[ "$line" =~ ^([^=]+)=(.*)$ ]]; then
        export "${BASH_REMATCH[1]}"="${BASH_REMATCH[2]}"
    fi
done < "$ENV_FILE"

BASE_URL="${UiPath__BaseUrl:-}"
TENANT="${UiPath__Tenant:-}"
CLIENT_ID="${UiPath__ClientId:-}"
CLIENT_SECRET="${UiPath__ClientSecret:-}"

if [[ -z "$BASE_URL" || -z "$CLIENT_ID" || -z "$CLIENT_SECRET" ]]; then
    echo "ERROR: Missing UiPath credentials in .env"
    echo "Required: UiPath__BaseUrl, UiPath__Tenant, UiPath__ClientId, UiPath__ClientSecret"
    exit 1
fi

echo "Tenant:   $TENANT"
echo "Base URL: $BASE_URL"
echo "ClientId: $CLIENT_ID"
echo ""

PASS=0
FAIL=0
declare -a RESULT_NAMES=()
declare -a RESULT_STATUS=()
declare -a RESULT_DETAILS=()

# --- Helper: test one OData endpoint ---
test_endpoint() {
    local name="$1"
    local url="$2"
    local token="$3"

    printf "Testing: %-42s" "$name ..."

    local response http_code body count

    response=$(curl -s -w "\n%{http_code}" \
        -H "Authorization: Bearer $token" \
        -H "X-UIPATH-TenantName: $TENANT" \
        "$url")

    http_code=$(echo "$response" | tail -n 1)
    body=$(echo "$response" | head -n -1)

    if [[ "$http_code" == "200" ]]; then
        count=$(echo "$body" \
            | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d.get('value',[])))" 2>/dev/null \
            || echo "?")
        echo " ✅ OK ($count items)"
        PASS=$((PASS + 1))
        RESULT_NAMES+=("$name")
        RESULT_STATUS+=("PASS")
        RESULT_DETAILS+=("$count items")
    else
        echo " ❌ FAIL (HTTP $http_code)"
        FAIL=$((FAIL + 1))
        RESULT_NAMES+=("$name")
        RESULT_STATUS+=("FAIL")
        RESULT_DETAILS+=("HTTP $http_code")
    fi
}

# ---------------------------------------------------------------------------
# Step 1: Get OAuth2 token
# ---------------------------------------------------------------------------
echo "Step 1: OAuth2 Authentication"

TOKEN_URL="$BASE_URL/identity_/connect/token"
TOKEN_RESPONSE=$(curl -s -X POST "$TOKEN_URL" \
    --data-urlencode "grant_type=client_credentials" \
    --data-urlencode "client_id=$CLIENT_ID" \
    --data-urlencode "client_secret=$CLIENT_SECRET" \
    -H "Content-Type: application/x-www-form-urlencoded")

TOKEN=$(echo "$TOKEN_RESPONSE" \
    | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])" 2>/dev/null \
    || echo "")

if [[ -z "$TOKEN" ]]; then
    echo "❌ FAIL: Could not obtain OAuth2 token"
    echo "Response: $TOKEN_RESPONSE"
    exit 1
fi

EXPIRES_IN=$(echo "$TOKEN_RESPONSE" \
    | python3 -c "import sys,json; print(json.load(sys.stdin).get('expires_in','?'))" 2>/dev/null \
    || echo "?")

echo "  ✅ Token obtained (expires in ${EXPIRES_IN}s)"
PASS=$((PASS + 1))
RESULT_NAMES+=("OAuth2 Token")
RESULT_STATUS+=("PASS")
RESULT_DETAILS+=("expires_in=${EXPIRES_IN}s")
echo ""

# ---------------------------------------------------------------------------
# Step 2: Orchestrator Endpoints
# ---------------------------------------------------------------------------
echo "Step 2: Orchestrator Endpoints"

ORC="$BASE_URL/$TENANT/orchestrator_"

test_endpoint "GET odata/Robots"            "$ORC/odata/Robots?\$top=10"           "$TOKEN"
test_endpoint "GET odata/Jobs (top 5)"      "$ORC/odata/Jobs?\$top=5"              "$TOKEN"
test_endpoint "GET odata/QueueDefinitions"  "$ORC/odata/QueueDefinitions?\$top=10" "$TOKEN"
test_endpoint "GET odata/Releases"          "$ORC/odata/Releases?\$top=10"         "$TOKEN"
test_endpoint "GET odata/Machines"          "$ORC/odata/Machines?\$top=10"         "$TOKEN"
test_endpoint "GET odata/Assets"            "$ORC/odata/Assets?\$top=10"           "$TOKEN"
test_endpoint "GET odata/RobotLogs (top 5)" "$ORC/odata/RobotLogs?\$top=5"         "$TOKEN"
test_endpoint "GET odata/Folders"           "$ORC/odata/Folders?\$top=10"          "$TOKEN"

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
echo ""
echo "=================================================="
echo "  RESULTS SUMMARY"
echo "=================================================="

total=${#RESULT_NAMES[@]}
for ((i = 0; i < total; i++)); do
    status="${RESULT_STATUS[$i]}"
    name="${RESULT_NAMES[$i]}"
    detail="${RESULT_DETAILS[$i]}"
    if [[ "$status" == "PASS" ]]; then
        echo "  ✅ $name  ($detail)"
    else
        echo "  ❌ $name  -> $detail"
    fi
done

echo ""
echo "  Passed: $PASS / $total"

if [[ $FAIL -gt 0 ]]; then
    echo ""
    echo "  HINT: 403 errors = missing OAuth2 scope in UiPath External App."
    echo "  Scope reference:"
    echo "    odata/Robots           -> OR.Robots.Read"
    echo "    odata/Jobs             -> OR.Jobs.Read"
    echo "    odata/QueueDefinitions -> OR.Queues.Read"
    echo "    odata/Releases         -> OR.Execution.Read"
    echo "    odata/Machines         -> OR.Machines.Read"
    echo "    odata/Assets           -> OR.Assets.Read"
    echo "    odata/RobotLogs        -> OR.Robots.Read / OR.Monitoring.Read"
    echo "    odata/Folders          -> OR.Folders.Read"
    echo ""
    exit 1
fi

echo ""
exit 0
