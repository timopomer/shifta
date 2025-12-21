#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PYTHON_PROJECT="$ROOT_DIR/scheduling-backend"
OUTPUT_DIR="$ROOT_DIR/backend/Backend.Api/Clients/Generated"
OUTPUT_FILE="$OUTPUT_DIR/OptimizerApiClient.cs"
TEMP_SPEC=$(mktemp --suffix=.json)

cleanup() {
    rm -f "$TEMP_SPEC"
}
trap cleanup EXIT

echo "=== Generating C# client from Python OpenAPI spec ==="

# Step 1: Build the Python container and export OpenAPI schema
echo "Step 1: Exporting OpenAPI schema from Python Docker container..."

docker build -t scheduling-openapi -f "$PYTHON_PROJECT/Dockerfile" --target base "$PYTHON_PROJECT" -q

docker run --rm scheduling-openapi python -c "
import json
from scheduling.api.app import app
print(json.dumps(app.openapi()))
" > "$TEMP_SPEC"

echo "  Exported to temp file: $TEMP_SPEC"

# Step 2: Ensure NSwag is installed
echo "Step 2: Checking NSwag installation..."
if ! dotnet tool list -g | grep -qi "nswag.consolecore"; then
    echo "  Installing NSwag CLI tool..."
    dotnet tool install --global NSwag.ConsoleCore
fi

# Step 3: Generate C# client
echo "Step 3: Generating C# client with NSwag..."
mkdir -p "$OUTPUT_DIR"

nswag openapi2csclient \
    /input:"$TEMP_SPEC" \
    /namespace:Backend.Api.Clients.Generated \
    /className:OptimizerApiClient \
    /output:"$OUTPUT_FILE" \
    /GenerateClientInterfaces:true \
    /GenerateExceptionClasses:true \
    /UseBaseUrl:false \
    /InjectHttpClient:true \
    /GenerateResponseClasses:false \
    /WrapResponses:false \
    /JsonLibrary:SystemTextJson \
    /ArrayType:System.Collections.Generic.List \
    /DateType:System.DateTime \
    /DateTimeType:System.DateTime \
    /OperationGenerationMode:SingleClientFromOperationId

# Step 4: Post-process the generated file to fix NSwag Collection vs List issue
echo "Step 4: Post-processing generated client..."
sed -i 's/new System\.Collections\.ObjectModel\.Collection/new System.Collections.Generic.List/g' "$OUTPUT_FILE"

echo ""
echo "=== Client generation complete! ==="
echo "Generated file: $OUTPUT_FILE"
