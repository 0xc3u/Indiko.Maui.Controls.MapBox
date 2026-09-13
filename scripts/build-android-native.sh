#!/usr/bin/env bash
# Builds the IndikoMapboxKit Android facade AAR and exports the com.mapbox.*
# runtime dependencies into the .NET binding project's Artifacts folder.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"

# Gradle 8.13 does not run on JDK 25 — use JDK 21 if the default is too new.
if [[ -z "${JAVA_HOME:-}" || "$("${JAVA_HOME:-/usr}/bin/java" -version 2>&1 | head -1 | grep -oE '"[0-9]+' | tr -d '"')" -gt 24 ]]; then
    for candidate in /Library/Java/JavaVirtualMachines/microsoft-21.jdk/Contents/Home; do
        if [[ -d "$candidate" ]]; then
            export JAVA_HOME="$candidate"
            break
        fi
    done
fi
echo "Using JAVA_HOME=${JAVA_HOME:-<default>}"

if [[ -z "${ANDROID_HOME:-}" && -d "$HOME/Library/Android/sdk" ]]; then
    export ANDROID_HOME="$HOME/Library/Android/sdk"
fi
echo "Using ANDROID_HOME=${ANDROID_HOME:-<unset>}"

cd "$ROOT/native/android"
./gradlew --no-daemon :indikomapboxkit:assembleRelease :indikomapboxkit:exportMapboxDeps

DEST="$ROOT/src/Indiko.Maui.Controls.MapBox.Bindings.Android/Artifacts"
mkdir -p "$DEST/deps"
rm -f "$DEST/deps/"*.aar "$DEST/deps/"*.jar 2>/dev/null || true

cp indikomapboxkit/build/outputs/aar/indikomapboxkit-release.aar "$DEST/"
cp indikomapboxkit/build/exported-deps/* "$DEST/deps/"

echo ""
echo "Exported artifacts:"
ls -la "$DEST" "$DEST/deps"
