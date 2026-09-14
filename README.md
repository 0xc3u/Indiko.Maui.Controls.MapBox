# Indiko.Maui.Controls.MapBox

[![NuGet](https://img.shields.io/nuget/v/Indiko.Maui.Controls.MapBox.svg)](https://www.nuget.org/packages/Indiko.Maui.Controls.MapBox)

Native Mapbox map control for .NET MAUI (Android + iOS), built on the **Mapbox Maps SDK v11**
with self-maintained bindings via a thin native facade (`IndikoMapboxKit`).

```
MapView (cross-platform MAUI View)
    └── MapViewHandler (per platform)
            └── IndikoMapboxKit facade (Kotlin / Swift @objc)
                    └── Mapbox Maps SDK v11 (native)
```

Every feature is exposed **twice**: as a classic .NET **event** for code-behind usage and as a
bindable **`ICommand`** for MVVM — pick whichever fits your app. All events/commands are raised
on the UI thread; command parameters carry the same `EventArgs` object the event delivers.

## Features

- 🗺️ All Mapbox styles (Standard, Streets, Dark, Satellite, … or any custom style URI)
- 🎥 Camera control: declarative `Camera` property, animated `FlyTo`, live `CameraChanged`
- 🔲 FitBounds: fit the camera to a bounding box or to all map content — manually or automatically (`AutoFitBounds`)
- 📍 Markers (point annotations) with per-marker color, title, payload and click events
- ✋ Draggable markers with a drag-end event and automatic model sync
- ➰ Polylines and polygons with styling and click events
- 🧩 GeoJSON sources with fill/line/circle style layers (survive style switches automatically)
- 🔵 Point clustering with managed layers and tap-to-expand zoom
- 💬 View annotations — any MAUI view anchored to a coordinate, gestures included
- 📴 Offline regions (style pack + tiles) with download progress events
- 🧭 Compass on rotation, scale bar, user-location puck, per-gesture configuration
- 🎯 Follow-puck mode: camera follows the user's position — switchable, with state-change events
- 👆 Click-consumed semantics: `MapClicked` fires only for taps on empty map

## Screenshots

| | iOS | Android |
|---|---|---|
| **Map, markers, polyline & polygon** | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-map-shapes.png" width="260" alt="iOS map with markers and shapes" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-map-shapes.png" width="260" alt="Android map with markers and shapes" /> |
| **GeoJSON sources & layers** (survive style switches) | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-geojson.png" width="260" alt="iOS GeoJSON layers" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-geojson-dark.png" width="260" alt="Android GeoJSON layers on dark style" /> |
| **Clustering with tap-to-expand** | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-clustering.png" width="260" alt="iOS clustering" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-cluster-expand.png" width="260" alt="Android cluster expanded after tap" /> |
| **View annotations** (MAUI views on the map) | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-view-annotation.png" width="260" alt="iOS view annotation bubble" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-view-annotation.png" width="260" alt="Android view annotation with tapped MAUI gesture" /> |
| **Follow-puck mode** | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-follow-puck.png" width="260" alt="iOS follow puck" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-follow-puck.png" width="260" alt="Android follow puck" /> |
| **Offline regions** (Android shown in airplane mode) | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-offline.png" width="260" alt="iOS offline region downloaded" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-offline-airplane.png" width="260" alt="Android rendering offline in airplane mode" /> |
| **AutoFitBounds / draggable markers** | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/ios-autofit.png" width="260" alt="iOS auto fit bounds" /> | <img src="https://raw.githubusercontent.com/0xc3u/Indiko.Maui.Controls.MapBox/main/docs/images/android-marker-dragged.png" width="260" alt="Android marker dragged to a new position" /> |

## Compatibility

### Versions

| Component | Version |
|---|---|
| .NET / MAUI | net10.0-android, net10.0-ios (MAUI 10) |
| Minimum OS | Android 11 (API 30) · iOS 14.2 |
| Mapbox Maps SDK Android | 11.30.1 (pinned) |
| Mapbox Maps SDK iOS | 11.26.0 (pinned) |

### Feature matrix vs. the official Mapbox Maps SDK v11

✅ supported · 🔶 partial · ❌ not yet — feature requests and PRs are welcome.

| Mapbox SDK area | Status | This control's API / notes |
|---|:---:|---|
| **Map display & styles** | | |
| Style URIs (Standard, Classic, custom `mapbox://styles/…`) | ✅ | `StyleUri`, `MapStyles` constants |
| Runtime style switching | ✅ | runtime sources/layers/clusters are re-applied automatically |
| Standard style configuration (light preset, theme, 3D objects) | ❌ | style renders with its defaults |
| Style JSON strings | ❌ | URIs only |
| **Camera** | | |
| Set camera (center, zoom, bearing, pitch) | ✅ | `Camera` property (one-way), `CurrentCamera` (live) |
| Animated camera (`flyTo`) | ✅ | `FlyTo(camera, durationMs)` |
| Fit to bounding box (`cameraForCoordinateBounds`) | ✅ | `FitBounds`, `FitBoundsToContent`, switchable `AutoFitBounds` |
| Camera padding / anchor offsets | 🔶 | uniform `FitBoundsPadding` only |
| **Annotations** | | |
| Point annotations (markers) | ✅ | `Annotations` collection: color, title, `Tag` payload, click events |
| Draggable point annotations | ✅ | `IsDraggable` + `AnnotationDragged` (model auto-synced) |
| Custom marker icons (own bitmaps/SVGs) | ❌ | colored pin glyph per marker |
| Polyline / polygon annotations | ✅ | `Polylines` / `Polygons` collections with click events |
| Circle annotations | 🔶 | via GeoJSON circle layers, not as annotation objects |
| View annotations (native views on the map) | ✅ | `ViewAnnotations` — **MAUI views incl. working gesture recognizers** |
| **Data-driven styling** | | |
| GeoJSON sources (add / live-update / remove) | ✅ | `AddGeoJsonSource` (add-or-update semantics) |
| Fill / line / circle layers | ✅ | `AddLayer(MapLayer)` with color, opacity, width, radius, insert position |
| Symbol, heatmap, fill-extrusion, raster, hillshade, sky layers | ❌ | |
| Vector / raster / image sources | ❌ | GeoJSON only |
| Expressions | 🔶 | used internally (cluster steps); not exposed as API |
| Clustering | ✅ | `AddClusteredSource` — managed layers, counts, tap-to-expand zoom |
| **Gestures & interaction** | | |
| Map tap / long-press with coordinates | ✅ | events + commands; taps on markers/shapes/clusters are consumed |
| Per-gesture enable/disable | ✅ | `ScrollEnabled`, `ZoomEnabled`, `RotateEnabled`, `PitchEnabled` |
| `queryRenderedFeatures` | 🔶 | used internally (cluster tap); not exposed as API |
| **Location** | | |
| Location puck | ✅ | `ShowUserLocation` (2D puck with bearing) |
| Follow-puck viewport | ✅ | `FollowPuck` (+ zoom/bearing options), state synced back on user pan |
| Custom puck appearance | ❌ | |
| **Offline** | | |
| Style pack + tile region download | ✅ | `DownloadOfflineRegion` with progress/completed events |
| Cancel / delete regions | ✅ | `RemoveOfflineRegion` |
| List regions, size estimates | ❌ | |
| **Ornaments** | | |
| Compass (auto-shows on rotation, tap resets north) | ✅ | SDK default behavior |
| Scale bar | ✅ | SDK default behavior |
| Ornament configuration (visibility, position) | ❌ | Mapbox logo/attribution stay visible (Mapbox ToS) |
| **Other** | | |
| Lifecycle events (`MapReady`, `StyleLoaded`, `CameraChanged`) | ✅ | events + bindable commands (full MVVM parity) |
| Snapshotter (static map images) | ❌ | |
| 3D terrain, globe, custom projections | ❌ | whatever the chosen style ships by default |

## Getting started

```
dotnet add package Indiko.Maui.Controls.MapBox
```

Register the handler and your Mapbox **public access token** (`pk.…` from
https://account.mapbox.com) in `MauiProgram.cs`:

```csharp
using Indiko.Maui.Controls.MapBox;

builder
    .UseMauiApp<App>()
    .UseMapbox("pk.YOUR_MAPBOX_ACCESS_TOKEN");
```

Add the map to a page:

```xml
<ContentPage xmlns:map="clr-namespace:Indiko.Maui.Controls.MapBox;assembly=Indiko.Maui.Controls.MapBox"
             xmlns:mapModels="clr-namespace:Indiko.Maui.Controls.MapBox.Models;assembly=Indiko.Maui.Controls.MapBox">

    <map:MapView x:Name="Map" StyleUri="{x:Static mapModels:MapStyles.Streets}">
        <map:MapView.Camera>
            <mapModels:MapCameraPosition Latitude="47.3769" Longitude="8.5417" Zoom="11" />
        </map:MapView.Camera>
    </map:MapView>

</ContentPage>
```

The MVVM samples below use [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
(`[RelayCommand]`, `[ObservableProperty]`), but any `ICommand` implementation works.

---

## Map styles

`StyleUri` accepts the constants from `MapStyles` (`Standard`, `StandardSatellite`, `Streets`,
`Outdoors`, `Light`, `Dark`, `Satellite`, `SatelliteStreets`) or any custom
`mapbox://styles/{user}/{styleId}` URI. `StyleLoaded` fires after every style switch.
Runtime content (GeoJSON layers, clusters) is re-applied automatically after a switch.

**Event-driven**

```csharp
Map.StyleUri = MapStyles.Dark;
Map.StyleLoaded += (_, _) => Debug.WriteLine("style is live");
```

**MVVM**

```xml
<map:MapView StyleUri="{Binding StyleUri}"
             StyleLoadedCommand="{Binding StyleLoadedCommand}" />
```

```csharp
[ObservableProperty]
private string styleUri = MapStyles.Streets;

[RelayCommand]
private void StyleLoaded() => IsStyleReady = true;
```

---

## Camera & FlyTo

`Camera` sets the position instantly (one-way). `FlyTo` animates. `CurrentCamera` always holds
the live position; `CameraChanged` fires continuously while the user pans/zooms/rotates.

**Event-driven**

```csharp
Map.MapReady += (_, _) => Map.FlyTo(new MapCameraPosition(46.9480, 7.4474, zoom: 12), durationMs: 2000);
Map.CameraChanged += (_, e) => ZoomLabel.Text = $"Zoom {e.Camera.Zoom:F1}";
```

**MVVM**

```xml
<map:MapView x:Name="Map"
             Camera="{Binding StartCamera}"
             MapReadyCommand="{Binding MapReadyCommand}"
             CameraChangedCommand="{Binding CameraChangedCommand}" />
```

```csharp
public MapCameraPosition StartCamera { get; } = new(47.3769, 8.5417, zoom: 11);

[RelayCommand]
private void CameraChanged(CameraChangedEventArgs e) => CurrentZoom = e.Camera.Zoom;
```

`FlyTo` is imperative by design. In MVVM, expose the `MapView` to the ViewModel via a slim
interface, or call it from the view in response to a ViewModel message.

---

## FitBounds

Fit the camera to a bounding box, to the current content, or keep it fitted automatically.
`FitBoundsPadding` (default 40 dp) controls the uniform edge padding.

**Event-driven / imperative**

```csharp
// explicit bounding box (e.g. a GPS track's extent):
Map.FitBounds(new MapBounds(minLat: 47.32, minLng: 8.46, maxLat: 47.43, maxLng: 8.62),
              padding: 60, durationMs: 800);

// fit to everything currently on the map (annotations, polylines, polygons, view annotations):
Map.FitBoundsToContent();

// helper: compute bounds from any positions
var bounds = MapBounds.FromPositions(track.Points.Select(p => (p.Lat, p.Lng)));
```

**MVVM — switchable auto mode**

```xml
<map:MapView Annotations="{Binding Pins}"
             Polylines="{Binding Routes}"
             AutoFitBounds="{Binding IsAutoFitEnabled}"
             FitBoundsPadding="60" />
```

```csharp
[ObservableProperty]
private bool isAutoFitEnabled = true;
```

While `AutoFitBounds` is enabled, every change to `Annotations`/`Polylines`/`Polygons`/
`ViewAnnotations` re-fits the camera to the full content; turning it off returns camera
control to the user. Explicit `FlyTo`/`Camera` calls are not overridden — auto-fit only
reacts to content changes.

---

## Markers (annotations)

`Annotations` is a bindable `ObservableRangeCollection<MapAnnotation>` — add/remove/clear and
the map updates. `AddRange`/`ReplaceRange` avoid per-item notifications for bulk updates.
Each marker has `Id`, `Latitude`, `Longitude`, `Title`, `Color` (hex) and a free `Tag` payload.
Tapping a marker raises `AnnotationClicked` (and suppresses `MapClicked`).

**Event-driven**

```csharp
Map.Annotations.Add(new MapAnnotation
{
    Latitude = 47.3769, Longitude = 8.5417,
    Title = "Zürich HB", Color = "#E74C3C", Tag = stationModel,
});

Map.AnnotationClicked += (_, e) => ShowDetails((Station)e.Annotation.Tag!);
```

**MVVM**

```xml
<map:MapView Annotations="{Binding Pins}"
             AnnotationClickedCommand="{Binding PinTappedCommand}" />
```

```csharp
public ObservableRangeCollection<MapAnnotation> Pins { get; } = [];

public void LoadStations(IEnumerable<Station> stations) =>
    Pins.ReplaceRange(stations.Select(s => new MapAnnotation
    {
        Latitude = s.Lat, Longitude = s.Lng, Title = s.Name, Tag = s,
    }));

[RelayCommand]
private void PinTapped(AnnotationClickedEventArgs e) =>
    SelectedStation = (Station)e.Annotation.Tag!;
```

---

## Draggable markers

Set `IsDraggable` on a marker and the user can move it: on Android by dragging the icon
directly, on iOS via long-press + drag. The final position is written back to the
annotation's `Latitude`/`Longitude` automatically, and `AnnotationDragged` reports it.
Drags never surface as map clicks or long-presses.

**Event-driven**

```csharp
Map.Annotations.Add(new MapAnnotation
{
    Latitude = 47.3769, Longitude = 8.5417,
    Title = "Route point 3", IsDraggable = true, Tag = routePoint,
});

Map.AnnotationDragged += (_, e) =>
    UpdateRoutePoint((RoutePoint)e.Annotation.Tag!, e.Latitude, e.Longitude);
```

**MVVM**

```xml
<map:MapView Annotations="{Binding RoutePoints}"
             AnnotationDraggedCommand="{Binding PointMovedCommand}" />
```

```csharp
[RelayCommand]
private void PointMoved(AnnotationDraggedEventArgs e) =>
    Route.MovePoint((RoutePoint)e.Annotation.Tag!, e.Latitude, e.Longitude);
```

---

## Map clicks & long-presses

`MapClicked` / `MapLongPressed` deliver the geographic coordinate of the tap.
**Click-consumed semantics:** taps that hit a marker, polyline, polygon or cluster are consumed
by that element — `MapClicked` only fires for taps on empty map.

**Event-driven**

```csharp
Map.MapClicked += (_, e) => Map.Annotations.Add(new MapAnnotation
{
    Latitude = e.Latitude, Longitude = e.Longitude, Color = "#27AE60",
});
Map.MapLongPressed += (_, e) => Map.FlyTo(new MapCameraPosition(e.Latitude, e.Longitude, 14), 1500);
```

**MVVM**

```xml
<map:MapView MapClickedCommand="{Binding MapClickedCommand}"
             MapLongPressedCommand="{Binding MapLongPressedCommand}" />
```

```csharp
[RelayCommand]
private void MapClicked(MapClickedEventArgs e) =>
    Waypoints.Add(new MapAnnotation { Latitude = e.Latitude, Longitude = e.Longitude });
```

---

## Polylines

`Polylines` is a bindable `ObservableRangeCollection<MapPolyline>`; each polyline has `Points`
(at least two `MapPosition`s), `Color`, `Width`, `Opacity`, `Title` and `Tag`. Tapping one
raises `PolylineClicked`.

**Event-driven**

```csharp
Map.Polylines.Add(new MapPolyline
{
    Points = [new(47.3769, 8.5417), new(47.3660, 8.5450), new(47.3550, 8.5530)],
    Color = "#E67E22", Width = 5, Title = "Lakeside trail",
});
Map.PolylineClicked += (_, e) => Toast($"Route: {e.Polyline.Title}");
```

**MVVM**

```xml
<map:MapView Polylines="{Binding Routes}"
             PolylineClickedCommand="{Binding RouteTappedCommand}" />
```

```csharp
public ObservableRangeCollection<MapPolyline> Routes { get; } = [];

[RelayCommand]
private void RouteTapped(PolylineClickedEventArgs e) => SelectedRoute = e.Polyline;
```

---

## Polygons

`Polygons` works the same way: `Points` (at least three, ring closes automatically),
`FillColor`, `FillOpacity`, `StrokeColor`, `Title`, `Tag`, plus `PolygonClicked`.

**Event-driven**

```csharp
Map.Polygons.Add(new MapPolygon
{
    Points = [new(47.366, 8.541), new(47.366, 8.556), new(47.333, 8.562), new(47.331, 8.545)],
    FillColor = "#9B59B6", FillOpacity = 0.35, StrokeColor = "#6C3483", Title = "Zone A",
});
Map.PolygonClicked += (_, e) => Toast($"Zone: {e.Polygon.Title}");
```

**MVVM**

```xml
<map:MapView Polygons="{Binding Zones}"
             PolygonClickedCommand="{Binding ZoneTappedCommand}" />
```

```csharp
[RelayCommand]
private void ZoneTapped(PolygonClickedEventArgs e) => SelectedZone = e.Polygon;
```

---

## GeoJSON sources & style layers

For data-driven rendering beyond individual shapes: add a GeoJSON source once, style it with
fill/line/circle layers. Calling `AddGeoJsonSource` again with the same id **replaces only the
data** — perfect for live updates. Sources and layers survive style switches automatically
(the native facade re-applies them after every style load).

These APIs are imperative (`AddGeoJsonSource`, `RemoveGeoJsonSource`, `AddLayer`,
`RemoveLayer`) — call them from code-behind, or hand the `MapView` to your ViewModel behind a
slim interface.

```csharp
Map.AddGeoJsonSource("live-vehicles", featureCollectionJson);

Map.AddLayer(new MapLayer
{
    Id = "vehicle-dots", SourceId = "live-vehicles",
    Type = MapLayerType.Circle, Color = "#C0392B", CircleRadius = 8,
});
Map.AddLayer(new MapLayer
{
    Id = "route-line", SourceId = "live-vehicles",
    Type = MapLayerType.Line, Color = "#16A085", LineWidth = 4,
});

// live update — only the data is replaced:
timer.Tick += (_, _) => Map.AddGeoJsonSource("live-vehicles", FetchLatestGeoJson());
```

`MapLayer` options: `Type` (`Fill` | `Line` | `Circle`), `Color`, `Opacity`, `LineWidth`,
`CircleRadius`, `BelowLayerId` (insert position in the style).

---

## Clustering

One call creates a clustered source plus three managed layers (cluster circles that grow with
the point count, the count label, unclustered points). **Tapping a cluster automatically zooms
to its expansion level** — handled inside the native facade.

```csharp
Map.AddClusteredSource(new MapClusterSource
{
    SourceId = "stations",
    GeoJson = stationsFeatureCollection,   // point features
    ClusterRadius = 50, ClusterMaxZoom = 14,
    ClusterColor = "#2563EB", ClusterTextColor = "#FFFFFF",
    PointColor = "#DC2626", PointRadius = 6,
});

Map.AddGeoJsonSource("stations", updatedGeoJson);  // live data update, cluster config stays
Map.RemoveClusteredSource("stations");             // removes source + all managed layers
```

---

## View annotations (MAUI views on the map)

Anchor **any MAUI view** to a coordinate — it is rendered natively, moves with the map, and its
gesture recognizers keep working. The native view-annotation system requires a fixed size.

**Event-driven**

```csharp
var bubble = new Border
{
    Background = Color.FromArgb("#1F2937"),
    StrokeShape = new RoundRectangle { CornerRadius = 12 },
    Content = new Label { Text = "🚉 Zürich HB", TextColor = Colors.White },
};
var tap = new TapGestureRecognizer();
tap.Tapped += (_, _) => ShowStationSheet();
bubble.GestureRecognizers.Add(tap);

Map.ViewAnnotations.Add(new MapViewAnnotation
{
    Latitude = 47.3779, Longitude = 8.5403,
    Content = bubble, Width = 150, Height = 44,
});
```

**MVVM**

```xml
<map:MapView ViewAnnotations="{Binding Bubbles}" />
```

```csharp
public ObservableRangeCollection<MapViewAnnotation> Bubbles { get; } = [];

// Content can be built from a DataTemplate-style factory in the VM layer:
Bubbles.Add(new MapViewAnnotation
{
    Latitude = poi.Lat, Longitude = poi.Lng,
    Content = _bubbleFactory.Create(poi),   // returns a MAUI View with bound commands
    Width = 150, Height = 44,
});
```

---

## Offline regions

`DownloadOfflineRegion` downloads the style pack **and** the map tiles for a bounding box in
one call. Progress and completion are reported per region id; downloaded regions render
automatically without network (the map reads the shared tile store).

**Event-driven**

```csharp
Map.OfflineRegionProgress += (_, e) => DownloadBar.Progress = e.Progress;      // 0.0 … 1.0
Map.OfflineRegionCompleted += (_, e) =>
    Status.Text = e.Success ? "Available offline ✓" : $"Failed: {e.ErrorMessage}";

Map.DownloadOfflineRegion(new MapOfflineRegion
{
    Id = "zurich",
    StyleUri = MapStyles.Streets,
    MinLatitude = 47.32, MinLongitude = 8.46,
    MaxLatitude = 47.43, MaxLongitude = 8.62,
    MinZoom = 6, MaxZoom = 14,          // higher MaxZoom = more detail, much more data
});

Map.RemoveOfflineRegion("zurich");      // cancels a running download / deletes tiles
```

**MVVM**

```xml
<map:MapView OfflineRegionProgressCommand="{Binding DownloadProgressCommand}"
             OfflineRegionCompletedCommand="{Binding DownloadCompletedCommand}" />
```

```csharp
[ObservableProperty]
private double downloadProgress;

[RelayCommand]
private void DownloadProgress(OfflineRegionProgressEventArgs e) => DownloadProgress = e.Progress;

[RelayCommand]
private void DownloadCompleted(OfflineRegionCompletedEventArgs e) =>
    IsOfflineReady = e.Success;
```

---

## Follow-puck mode

When enabled, the camera continuously follows the user-location puck (shown implicitly) using
Mapbox's native viewport engine. Panning the map ends the mode natively — the `FollowPuck`
property is synced back and `FollowPuckChanged` fires, so a "follow" button always reflects
the real state. Your app must request location permission itself.

**Event-driven**

```csharp
var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
if (status == PermissionStatus.Granted)
    Map.FollowPuck = true;                    // camera flies to the puck and stays on it

Map.FollowPuckChanged += (_, e) =>
    FollowButton.Text = e.IsActive ? "Following ✓" : "Follow me";
```

**MVVM**

```xml
<map:MapView FollowPuck="{Binding IsFollowing, Mode=TwoWay}"
             FollowPuckZoom="16"
             FollowPuckTrackBearing="False"
             FollowPuckChangedCommand="{Binding FollowChangedCommand}" />
```

```csharp
[ObservableProperty]
private bool isFollowing;

[RelayCommand]
private void FollowChanged(FollowPuckChangedEventArgs e) => IsFollowing = e.IsActive;
```

`FollowPuckZoom` (default 16) sets the follow zoom; `FollowPuckTrackBearing` rotates the map
with the puck's heading instead of keeping north up.

---

## User location, gestures & compass

```xml
<map:MapView ShowUserLocation="True"
             ScrollEnabled="True" ZoomEnabled="True"
             RotateEnabled="True" PitchEnabled="False" />
```

- `ShowUserLocation` shows the native location puck (your app must request location
  permission itself).
- The four gesture switches toggle pan / zoom (pinch, double-tap, quick-zoom) / rotate / pitch
  individually — all bindable.
- A **compass ornament appears automatically** (top-right) whenever the map is rotated away
  from north and hides again when facing north; tapping it resets the bearing to 0.
  It indicates the map's north, not the device's magnetometer heading.

---

## Events ↔ Commands

Every event has a bindable command counterpart; command parameters are the event args.

| Event | Command | Args |
|---|---|---|
| `MapReady` | `MapReadyCommand` | – |
| `StyleLoaded` | `StyleLoadedCommand` | – |
| `MapClicked` | `MapClickedCommand` | `MapClickedEventArgs` |
| `MapLongPressed` | `MapLongPressedCommand` | `MapClickedEventArgs` |
| `AnnotationClicked` | `AnnotationClickedCommand` | `AnnotationClickedEventArgs` |
| `PolylineClicked` | `PolylineClickedCommand` | `PolylineClickedEventArgs` |
| `PolygonClicked` | `PolygonClickedCommand` | `PolygonClickedEventArgs` |
| `CameraChanged` | `CameraChangedCommand` | `CameraChangedEventArgs` |
| `OfflineRegionProgress` | `OfflineRegionProgressCommand` | `OfflineRegionProgressEventArgs` |
| `OfflineRegionCompleted` | `OfflineRegionCompletedCommand` | `OfflineRegionCompletedEventArgs` |

## Sample app

`samples/Indiko.Maui.Controls.MapBox.Sample` demonstrates every feature. Copy
`MapboxToken.cs.template` to `MapboxToken.cs` (git-ignored) and insert your `pk.…` token.

## Building from source

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

Releases are automated via Semantic Release — conventional commits on `main` produce the
version tag, CHANGELOG and NuGet package.
