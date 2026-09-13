# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Native facades — run once after cloning (fetches the large, git-ignored Mapbox
# binaries) and whenever native/** changes; facade artifacts are committed
./scripts/build-android-native.sh    # Kotlin facade AAR + com.mapbox.* deps → Bindings.Android/Artifacts
./scripts/build-ios-native.sh        # Swift facade XCFramework + Mapbox dylibs → Bindings.iOS/Artifacts

# .NET library
dotnet build src/Indiko.Maui.Controls.MapBox.sln -c Release

# Sample app
dotnet build samples/Indiko.Maui.Controls.MapBox.Sample.sln -c Release
```

Running the sample requires a Mapbox public token (pk.…) in `samples/.../MauiProgram.cs`.

## Architecture

.NET MAUI control library targeting `net10.0-android` and `net10.0-ios` (min Android 30, iOS 14.2),
architecturally modeled after Indiko.Maui.Controls.Chat, plus a native binding layer:

```
MapView (src/Indiko.Maui.Controls.MapBox/MapView.cs — BindableProperties, events, commands)
    └── MapViewHandler (Platforms/Android + Platforms/iOS — PropertyMapper + CommandMapper)
            └── Binding projects (Bindings.Android: AAR; Bindings.iOS: hand-written ApiDefinition.cs)
                    └── IndikoMapboxKit facade (native/android Kotlin; native/ios Swift @objc)
                            └── Mapbox Maps SDK v11 (Android 11.30.1, iOS 11.26.0 — pinned)
```

### The facade principle (key design decision)

The full Mapbox SDK is **never** bound to C#. Instead, each platform has a small native facade
(`IKMapView`, `IKMapbox`, `IKCameraState`, `IKMapEventListener`) with an intentionally
binding-friendly, platform-identical API: primitives, String, and one listener interface only.
Markers cross the boundary as a JSON array (see `AnnotationSerializer`). Consequences:

- New features always touch three layers: facade (Kotlin + Swift) → binding → handler/MapView.
- Mapbox breaking changes are absorbed in the native layer; the C# surface stays stable.
- The iOS ApiDefinition.cs is written by hand (no Objective Sharpie); its selectors must exactly
  match the `@objc(...)` names in IKMapView.swift.

### Layer notes

- **native/ios**: XcodeGen project (`project.yml`), MapboxMaps via SPM. The build script strips
  the Swift `Modules` folder before `-create-xcframework` (no library evolution; only the
  generated ObjC header `IndikoMapboxKit-Swift.h` is consumed). MapboxCommon/MapboxCoreMaps
  dynamic XCFrameworks are copied out of the SPM artifact cache and referenced as
  `NativeReference`s in the binding so they get embedded into apps.
- **native/android**: Gradle 8.13 needs JDK 21 (`/Library/Java/JavaVirtualMachines/microsoft-21.jdk`);
  the build script sets JAVA_HOME/ANDROID_HOME automatically. The `exportMapboxDeps` task copies
  all `com.mapbox.*` runtime AARs/JARs for the binding project (`Bind="false"`); non-Mapbox Java
  deps (Kotlin stdlib, coroutines, OkHttp, AndroidX, Gson) come from NuGet instead.
- **Bindings.Android**: `EnableDefaultAndroidItems=false` is required — default globbing would
  re-add every AAR with `Bind=true` and try to bind the whole Mapbox SDK (~1200 errors).
  The Kotlin interface binds as `IKMapEventListener` (no extra `I` prefix). Namespace is forced
  to `Indiko.Maui.Controls.MapBox.Bindings` via Transforms/Metadata.xml.
- **Handlers**: follow the Chat plugin conventions — `ConnectHandler`/`DisconnectHandler`,
  listeners hold only a `WeakReference<MapView>` (NSObject subclasses leak on circular refs on
  Apple platforms), events are dispatched to the UI thread via `view.Dispatcher`.
- **Annotations**: `ObservableRangeCollection<MapAnnotation>` (also Polylines/Polygons) with
  replace-all JSON push on every collection change (spike-level; diffing is a later optimization).
- **GeoJSON sources/layers**: imperative MapView API (`AddGeoJsonSource`/`AddLayer`/`Remove*`) via
  CommandMapper. Mapbox drops runtime sources/layers on every style reload — both facades keep a
  registry and re-apply it after each StyleLoaded event, so they survive style switches.
- **Clustering**: `AddClusteredSource(MapClusterSource)` creates a clustered GeoJSON source plus
  three facade-managed layers (`<id>-clusters`, `<id>-cluster-count`, `<id>-points`); tapping a
  cluster queries the rendered feature and eases to its expansion zoom — all inside the facades.
  `AddGeoJsonSource` on a clustered id only replaces its data. Expressions are built from raw
  style-spec JSON (`Exp` via JSONDecoder on iOS, `Expression.fromRaw` on Android).

### Adding a new MapView property

1. Facade: add the setter to IKMapView.swift **and** IKMapView.kt (identical signatures).
2. iOS: mirror the `@objc(...)` selector in Bindings.iOS/ApiDefinition.cs.
3. Rebuild native artifacts: both `scripts/build-*-native.sh`.
4. Declare the `BindableProperty` in MapView.cs and map it in **both** handlers' PropertyMapper.

## Code Style

- Allman braces, 4-space indentation, 120-character line limit (C#, Swift, Kotlin alike).
- `using` statements outside the namespace; remove unused usings.
- Commit messages use Semantic Release prefixes: `feat:`, `fix:`, `docs:`, `chore:`, `refactor:`,
  `perf:`, `style:`, `ci:`, `build:`, `test:`.

## MAUI-Specific Rules

- Prefer `Grid` over nested layouts; use `VerticalStackLayout`/`HorizontalStackLayout`, not `StackLayout`.
- Use `Border`, not `Frame`.
- Handler registration belongs in `MauiProgram.cs` via `builder.UseMapbox("pk.…")`.
