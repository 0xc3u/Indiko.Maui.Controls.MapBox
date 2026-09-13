#!/usr/bin/env bash
# Builds the IndikoMapboxKit iOS facade as an XCFramework (device + simulator)
# and copies it — together with the dynamic Mapbox binaries resolved via SPM —
# into the .NET binding project's Artifacts folder.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJ="$ROOT/native/ios/IndikoMapboxKit"
DERIVED="$PROJ/DerivedData"
DEST="$ROOT/src/Indiko.Maui.Controls.MapBox.Bindings.iOS/Artifacts"

cd "$PROJ"
xcodegen generate

rm -rf archives out

# MapboxMaps is statically linked into the facade, so its SPM resource bundle
# must live inside the facade framework — the runtime bundle accessor looks it
# up via Bundle(for:) relative to the framework that contains the code.
# Both archives share ArchiveIntermediates, so each bundle must be copied
# immediately after its own archive step before the next one wipes it.
INTERMEDIATE="$DERIVED/Build/Intermediates.noindex/ArchiveIntermediates/IndikoMapboxKit/IntermediateBuildFilesPath/UninstalledProducts"

xcodebuild archive \
    -project IndikoMapboxKit.xcodeproj \
    -scheme IndikoMapboxKit \
    -destination "generic/platform=iOS" \
    -archivePath archives/ios.xcarchive \
    -derivedDataPath "$DERIVED" \
    SKIP_INSTALL=NO CODE_SIGNING_ALLOWED=NO | tail -5

cp -R "$INTERMEDIATE/iphoneos/MapboxMaps_MapboxMaps.bundle" \
    archives/ios.xcarchive/Products/Library/Frameworks/IndikoMapboxKit.framework/

xcodebuild archive \
    -project IndikoMapboxKit.xcodeproj \
    -scheme IndikoMapboxKit \
    -destination "generic/platform=iOS Simulator" \
    -archivePath archives/iossim.xcarchive \
    -derivedDataPath "$DERIVED" \
    SKIP_INSTALL=NO CODE_SIGNING_ALLOWED=NO | tail -5

cp -R "$INTERMEDIATE/iphonesimulator/MapboxMaps_MapboxMaps.bundle" \
    archives/iossim.xcarchive/Products/Library/Frameworks/IndikoMapboxKit.framework/

# The binding consumes only the generated ObjC header (IndikoMapboxKit-Swift.h).
# Strip the Swift module folders — without BUILD_LIBRARY_FOR_DISTRIBUTION there are
# no .swiftinterface files and -create-xcframework would refuse the frameworks.
rm -rf archives/ios.xcarchive/Products/Library/Frameworks/IndikoMapboxKit.framework/Modules
rm -rf archives/iossim.xcarchive/Products/Library/Frameworks/IndikoMapboxKit.framework/Modules

xcodebuild -create-xcframework \
    -framework archives/ios.xcarchive/Products/Library/Frameworks/IndikoMapboxKit.framework \
    -framework archives/iossim.xcarchive/Products/Library/Frameworks/IndikoMapboxKit.framework \
    -output out/IndikoMapboxKit.xcframework

mkdir -p "$DEST"
rm -rf "$DEST/IndikoMapboxKit.xcframework" "$DEST/MapboxCommon.xcframework" "$DEST/MapboxCoreMaps.xcframework" "$DEST/Turf.xcframework"
cp -R out/IndikoMapboxKit.xcframework "$DEST/"

# The dynamic Mapbox binaries must ship alongside the facade — grab them from
# the SPM artifact cache that xcodebuild just populated. Turf is dynamic too:
# both our facade and MapboxCommon/CoreMaps link @rpath/Turf.framework.
for name in MapboxCommon MapboxCoreMaps Turf; do
    FOUND="$(find "$DERIVED/SourcePackages/artifacts" -type d -name "$name.xcframework" | head -1)"
    if [[ -z "$FOUND" ]]; then
        echo "ERROR: $name.xcframework not found in SPM artifacts" >&2
        exit 1
    fi
    cp -R "$FOUND" "$DEST/"
done

echo ""
echo "Exported artifacts:"
ls -la "$DEST"
