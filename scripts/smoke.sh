#!/usr/bin/env bash
# BotPulse - Smoke Tests Post-Deploy
# Usage: bash scripts/smoke.sh [base_url]
set -euo pipefail

BASE_URL="${1:-http://localhost}"
ADMIN_USER="${ADMIN_USER:-admin}"
ADMIN_PASS="${ADMIN_PASS:-Admin@BotPulse2024!}"
PASS=0; FAIL=0

test_endpoint() {
    local name="$1"; local cmd="$2"
    printf "Testing: %-40s" "$name ..."
    if eval "$cmd" > /dev/null 2>&1; then
        echo " OK"; PASS=$((PASS+1))
    else
        echo " FAIL"; FAIL=$((FAIL+1))
    fi
}

echo ""
echo "================================================="
echo "  BotPulse Smoke Tests — $BASE_URL"
echo "================================================="
echo ""

test_endpoint "GET /health/live"  "curl -sf $BASE_URL/health/live"
test_endpoint "GET /health/ready" "curl -sf $BASE_URL/health/ready"

TOKEN=$(curl -sf -X POST "$BASE_URL/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"userName\":\"$ADMIN_USER\",\"password\":\"$ADMIN_PASS\"}" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])" 2>/dev/null || echo "")

if [[ -n "$TOKEN" ]]; then
    echo "  Token obtained ✓"
    H="Authorization: Bearer $TOKEN"
    test_endpoint "GET /api/v1/auth/me"   "curl -sf -H '$H' $BASE_URL/api/v1/auth/me"
    test_endpoint "GET /api/v1/robots"    "curl -sf -H '$H' $BASE_URL/api/v1/robots"
    test_endpoint "GET /api/v1/jobs"      "curl -sf -H '$H' $BASE_URL/api/v1/jobs"
    test_endpoint "GET /api/v1/machines"  "curl -sf -H '$H' $BASE_URL/api/v1/machines"
else
    echo "  Login FAILED — skipping authenticated tests"
    FAIL=$((FAIL+1))
fi

echo ""
echo "================================================="
echo "  Results: $PASS passed, $FAIL failed"
echo "================================================="
[[ $FAIL -eq 0 ]]
