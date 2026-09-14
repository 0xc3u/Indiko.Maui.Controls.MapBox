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

Known issue: after deleting ALL obj/bin folders, the very first sample iOS build can fail with
"Undefined symbols … _OBJC_CLASS_$_IK…" — the binding's `.resources.zip` sidecar is produced too
late for the app link in the same invocation. Simply build a second time. NuGet consumers and CI
(library-only build) are not affected.

## Releasing

Semantic Release CI (like Indiko.Maui.Controls.Chat): pushes to main run
`.github/workflows/semanticrelease.yml` (build on macOS incl. cached native artifacts, then
semantic-release creates the version tag + CHANGELOG from conventional commits). The tag push
triggers `.github/workflows/release-nuget.yml`, which packs and pushes to nuget.org.
Required repo secrets: `GH_TOKEN` (environment "Release") and `NUGET_API_KEY`.
Do not bump versions by hand — `PackageVersion` is injected from the tag at pack time.

Packaging: the binding projects are IsPackable=false; `PackBindingOutputs` in the main csproj
packs their DLLs, all native AARs and the iOS `.resources.zip` sidecar into lib/. The
`AllowedOutputExtensionsInPackageBuildOutputFolder` override (.aar/.zip) is required for that.
The Android binding's NuGet dependencies are mirrored in the main csproj (PrivateAssets=all on
the ProjectReference suppresses transitive flow) — keep both lists in sync.

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
- **Click-consumed semantics**: `MapClicked` fires only for taps on empty map. Map-click dispatch
  is deferred past the synchronous gesture pipeline; annotation click handlers stamp a timestamp
  and the cluster hit-test reports through a completion callback.
- **View annotations**: `ViewAnnotations` collection of `MapViewAnnotation` (MAUI `View` content,
  fixed Width/Height, bottom-center anchor). The handler converts content via `ToPlatform` +
  Measure/Arrange and syncs by id (no content updates — remove/re-add). Android facade must set
  `layoutParams` before `addViewAnnotation` (Mapbox casts them unconditionally). MAUI gesture
  recognizers inside the content keep working.
- **Offline regions**: `DownloadOfflineRegion(MapOfflineRegion)` downloads the style pack
  (OfflineManager) plus the bounding-box tile region (default TileStore) in one call; progress
  and completion arrive via `OfflineRegionProgress`/`OfflineRegionCompleted`. The MapView reads
  the default TileStore automatically, so downloaded regions render without network.
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
