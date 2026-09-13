# Indiko.Maui.Controls.MapBox

Native Mapbox map control for .NET MAUI (Android + iOS), built on the **Mapbox Maps SDK v11** with
own, self-maintained bindings via a thin native facade (`IndikoMapboxKit`).

```
MapView (cross-platform MAUI View)
    └── MapViewHandler (per platform)
            └── IndikoMapboxKit facade (Kotlin / Swift @objc)
                    └── Mapbox Maps SDK v11 (native)
```

## Features (MVP)

- Map display with all Mapbox styles (Standard, Streets, Dark, Satellite, … or custom style URIs)
- Camera control: declarative via `Camera` property, animated via `FlyTo(...)`
- Point annotations (markers) with per-marker color and click events
- Map click / long-press events with geo coordinates
- Live camera observation (`CameraChanged` / `CurrentCamera`)
- User location puck, gesture configuration (scroll/zoom/rotate/pitch)

## Getting started

```csharp
// MauiProgram.cs
builder.UseMapbox("pk.YOUR_MAPBOX_ACCESS_TOKEN");
```

```xml
<map:MapView StyleUri="{x:Static mapModels:MapStyles.Streets}"
             MapClicked="OnMapClicked">
    <map:MapView.Camera>
        <mapModels:MapCameraPosition Latitude="47.3769" Longitude="8.5417" Zoom="11" />
    </map:MapView.Camera>
</map:MapView>
```

A public access token (pk.…) from https://account.mapbox.com is required at runtime.
For the sample app, set it in `samples/Indiko.Maui.Controls.MapBox.Sample/MauiProgram.cs`.

## Building

```bash
# 1. Native facades — run once after cloning (fetches Mapbox binaries) and
#    whenever native/ changes; only the small facade artifacts are committed
./scripts/build-android-native.sh   # Kotlin facade AAR + com.mapbox.* deps
./scripts/build-ios-native.sh       # Swift facade XCFramework + Mapbox dynamic frameworks

# 2. .NET library
dotnet build src/Indiko.Maui.Controls.MapBox.sln -c Release

# 3. Sample app
dotnet build samples/Indiko.Maui.Controls.MapBox.Sample.sln -c Release
```

Prerequisites: .NET 10 with `maui` workload, Xcode, XcodeGen (`brew install xcodegen`),
JDK 21 (for Gradle), Android SDK.

The Mapbox artifact downloads (Maven / SPM binaries) are currently public. Should Mapbox
re-enable authentication, provide a secret download token (sk.…, scope `DOWNLOADS:READ`)
via `MAPBOX_DOWNLOADS_TOKEN` in `~/.gradle/gradle.properties` and `~/.netrc`.

## Pinned Mapbox versions

| Platform | Version | Pinned in |
| -------- | ------- | --------- |
| Android  | 11.30.1 | `native/android/indikomapboxkit/build.gradle.kts` |
| iOS      | 11.26.0 | `native/ios/IndikoMapboxKit/project.yml` |
