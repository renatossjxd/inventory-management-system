#!/usr/bin/env bash
set -euo pipefail

api_url="${INTEGRATION_API_URL:-http://localhost:8080}"
admin_email="${INTEGRATION_ADMIN_EMAIL:-integration.admin@inventory.local}"
admin_password="${INTEGRATION_ADMIN_PASSWORD:-Integration_2026!}"
run_id="$(date +%s)"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

request() {
  local method="$1" path="$2" body="${3:-}" output="$4"
  local args=(-sS -o "$output" -w '%{http_code}' -X "$method" "$api_url$path")
  if [[ -n "${token:-}" ]]; then args+=(-H "Authorization: Bearer $token"); fi
  if [[ -n "$body" ]]; then args+=(-H 'Content-Type: application/json' --data "$body"); fi
  curl "${args[@]}"
}

assert_status() {
  if [[ "$1" != "$2" ]]; then
    echo "Se esperaba HTTP $2 y se recibió $1."
    cat "$3"
    exit 1
  fi
}

echo "Esperando que la API esté disponible..."
for attempt in {1..60}; do
  if curl -fsS "$api_url/health" >/dev/null 2>&1; then break; fi
  if [[ "$attempt" == 60 ]]; then echo "La API no respondió a tiempo."; exit 1; fi
  sleep 2
done

curl -fsS "$api_url/" -o "$work_dir/index.html"
python -c 'import sys; text=open(sys.argv[1],encoding="utf-8").read(); assert "Renato Inventory" in text and "login-form" in text' "$work_dir/index.html"

register_body="$(python -c 'import json,sys; print(json.dumps({"email":sys.argv[1],"displayName":"Integration Admin","password":sys.argv[2]}))' "$admin_email" "$admin_password")"
status="$(request POST /api/auth/register "$register_body" "$work_dir/auth.json")"
if [[ "$status" != 201 ]]; then
  login_body="$(python -c 'import json,sys; print(json.dumps({"email":sys.argv[1],"password":sys.argv[2]}))' "$admin_email" "$admin_password")"
  status="$(request POST /api/auth/login "$login_body" "$work_dir/auth.json")"
  assert_status "$status" 200 "$work_dir/auth.json"
fi
token="$(python -c 'import json,sys; print(json.load(open(sys.argv[1]))["accessToken"])' "$work_dir/auth.json")"

category_body="$(python -c 'import json,sys; print(json.dumps({"name":sys.argv[1],"description":"Prueba automatizada"}))' "Integración $run_id")"
status="$(request POST /api/categories "$category_body" "$work_dir/category.json")"
assert_status "$status" 201 "$work_dir/category.json"
category_id="$(python -c 'import json,sys; print(json.load(open(sys.argv[1]))["id"])' "$work_dir/category.json")"

supplier_body="$(python -c 'import json,sys; print(json.dumps({"name":sys.argv[1],"email":"integration@example.com","phone":"+56 9 1111 2222"}))' "Proveedor integración $run_id")"
status="$(request POST /api/suppliers "$supplier_body" "$work_dir/supplier.json")"
assert_status "$status" 201 "$work_dir/supplier.json"
supplier_id="$(python -c 'import json,sys; print(json.load(open(sys.argv[1]))["id"])' "$work_dir/supplier.json")"

product_body="$(python -c 'import json,sys; print(json.dumps({"sku":sys.argv[1],"name":"Producto de integración","price":14990,"minimumStock":2,"description":"Creado por CI","categoryId":sys.argv[2],"supplierId":sys.argv[3]}))' "INT-$run_id" "$category_id" "$supplier_id")"
status="$(request POST /api/products "$product_body" "$work_dir/product.json")"
assert_status "$status" 201 "$work_dir/product.json"
product_id="$(python -c 'import json,sys; print(json.load(open(sys.argv[1]))["id"])' "$work_dir/product.json")"

status="$(request POST "/api/products/$product_id/stock-movements" \
  '{"quantity":5,"reason":"Entrada de prueba automatizada"}' "$work_dir/movement.json")"
assert_status "$status" 200 "$work_dir/movement.json"
python -c 'import json,sys; assert json.load(open(sys.argv[1]))["currentStock"] == 5' "$work_dir/movement.json"

printf '\x89PNG\r\n\x1a\n\x00\x00\x00\x0dIHDR' > "$work_dir/product.png"
image_file="$work_dir/product.png"
if command -v cygpath >/dev/null 2>&1; then image_file="$(cygpath -w "$image_file")"; fi
status="$(curl -sS -o "$work_dir/image.json" -w '%{http_code}' -X POST \
  -H "Authorization: Bearer $token" -F "file=@$image_file;type=image/png" \
  "$api_url/api/products/$product_id/image")"
assert_status "$status" 200 "$work_dir/image.json"
image_url="$(python -c 'import json,sys; print(json.load(open(sys.argv[1]))["imageUrl"])' "$work_dir/image.json")"
curl -fsS -o "$work_dir/downloaded-image.png" "$image_url"

status="$(request GET "/api/products?page=1&pageSize=10&search=INT-$run_id" '' "$work_dir/products.json")"
assert_status "$status" 200 "$work_dir/products.json"
python -c 'import json,sys; data=json.load(open(sys.argv[1])); assert data["totalCount"] == 1 and data["items"][0]["id"] == sys.argv[2]' "$work_dir/products.json" "$product_id"

status="$(request GET /api/dashboard '' "$work_dir/dashboard.json")"
assert_status "$status" 200 "$work_dir/dashboard.json"
python -c 'import json,sys; data=json.load(open(sys.argv[1])); assert data["productCount"] >= 1 and data["totalStockUnits"] >= 5 and data["inventoryValue"] >= 74950' "$work_dir/dashboard.json"

status="$(request GET "/api/reports/inventory.csv?search=INT-$run_id" '' "$work_dir/inventory.csv")"
assert_status "$status" 200 "$work_dir/inventory.csv"
python -c 'import sys; text=open(sys.argv[1],encoding="utf-8-sig").read(); assert "SKU;Producto;Categoría" in text and sys.argv[2] in text' "$work_dir/inventory.csv" "INT-$run_id"

status="$(request POST "/api/products/$product_id/stock-movements" \
  '{"quantity":-4,"reason":"Salida que activa alerta de stock"}' "$work_dir/low-stock-movement.json")"
assert_status "$status" 200 "$work_dir/low-stock-movement.json"
status="$(request GET "/api/notifications?unreadOnly=true" '' "$work_dir/notifications.json")"
assert_status "$status" 200 "$work_dir/notifications.json"
notification_id="$(python -c 'import json,sys; items=json.load(open(sys.argv[1])); item=next(x for x in items if x["productId"] == sys.argv[2]); assert not item["isRead"]; print(item["id"])' "$work_dir/notifications.json" "$product_id")"
status="$(request POST "/api/notifications/$notification_id/read" '' "$work_dir/notification-read.json")"
assert_status "$status" 204 "$work_dir/notification-read.json"

status="$(request GET "/api/audit-logs?pageSize=100&method=POST" '' "$work_dir/audit-logs.json")"
assert_status "$status" 200 "$work_dir/audit-logs.json"
python -c 'import json,sys; data=json.load(open(sys.argv[1])); assert data["totalCount"] >= 1 and any(x["path"].endswith("/stock-movements") and x["statusCode"] == 200 for x in data["items"])' "$work_dir/audit-logs.json"

echo "Prueba de integración completada: autenticación, SQL Server, inventario, alertas, auditoría, dashboard, reportes y Blob Storage correctos."
