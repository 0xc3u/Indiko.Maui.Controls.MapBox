using Indiko.Maui.Controls.MapBox.Models;

namespace Indiko.Maui.Controls.MapBox.Sample;

public partial class MainPage : ContentPage
{
	private static readonly Color Accent = Color.FromArgb("#6AA127");
	private static readonly Color Surface = Colors.White;
	private static readonly Color Ink = Color.FromArgb("#1F2937");

	public MainPage()
	{
		InitializeComponent();
		PaintChip(ChipOutdoor, true);
	}

	/* ------------------------------ Chrome helpers ------------------------------ */

	private static void PaintChip(Button chip, bool active)
	{
		chip.BackgroundColor = active ? Accent : Surface;
		chip.TextColor = active ? Colors.White : Ink;
	}

	private bool sheetCollapsed;

	private void OnSheetHandleTapped(object? sender, TappedEventArgs e)
	{
		sheetCollapsed = !sheetCollapsed;
		SheetChevron.Text = sheetCollapsed ? "⌃" : "⌄";
		SheetBody.IsVisible = !sheetCollapsed;
	}

	private void OnStyleChipClicked(object? sender, EventArgs e)
	{
		if (sender is not Button chip)
			return;

		Map.StyleUri = chip == ChipStreets ? MapStyles.Streets
			: chip == ChipSatellite ? MapStyles.SatelliteStreets
			: chip == ChipDark ? MapStyles.Dark
			: MapStyles.Outdoors;

		foreach (var candidate in new[] { ChipOutdoor, ChipStreets, ChipSatellite, ChipDark })
			PaintChip(candidate, candidate == chip);
		StatusLabel.Text = $"Style: {chip.Text}";
	}

	private void OnFitToContent(object? sender, EventArgs e)
	{
		Map.FitBoundsToContent(800);
		StatusLabel.Text = "Kamera auf Karteninhalt eingepasst";
	}

	private void OnZoomIn(object? sender, EventArgs e) => NudgeZoom(+1);

	private void OnZoomOut(object? sender, EventArgs e) => NudgeZoom(-1);

	private async void NudgeZoom(double delta)
	{
		var camera = Map.CurrentCamera;
		if (camera is null)
			return;

		Map.FlyTo(new MapCameraPosition(camera.Latitude, camera.Longitude, camera.Zoom + delta,
			camera.Bearing, camera.Pitch), 200);

		// Report the zoom the map actually settled on — with MinZoom/MaxZoom set it can
		// differ from what was requested.
		await Task.Delay(350);
		if (Map.CurrentCamera is { } settled)
			StatusLabel.Text = $"Zoom {settled.Zoom:F1}";
	}

	private void OnFlyToLauterbrunnen(object? sender, EventArgs e)
	{
		Map.FlyTo(new MapCameraPosition(46.60, 7.91, 12,
			bearing: 0, pitch: Map.TerrainEnabled ? 65 : 0), 2000);
	}

	private async void OnMapReady(object? sender, EventArgs e)
	{
		// Promo hook: cinematic flight Lauterbrunnen (3D) -> Bern (2D), used to
		// record marketing clips (SIMCTL_CHILD_DEMO_FLIGHT=1). Skips demo content.
		if (Environment.GetEnvironmentVariable("DEMO_FLIGHT") == "1")
		{
			_ = RunPromoFlightAsync();
			return;
		}

		StatusLabel.Text = "Karte bereit — tippe für Marker, lang drücken für FlyTo";

		using var pawStream = await FileSystem.OpenAppPackageFileAsync("paw.png");
		using var pawBuffer = new MemoryStream();
		await pawStream.CopyToAsync(pawBuffer);

		Map.Annotations.Add(new MapAnnotation
		{
			Latitude = 47.3600,
			Longitude = 8.5300,
			Title = "Custom Icon",
			IconData = pawBuffer.ToArray(),
			IconWidth = 44,
			IconAnchor = MapIconAnchor.Center,
		});

		Map.Annotations.AddRange(
		[
			new MapAnnotation { Latitude = 47.3769, Longitude = 8.5417, Title = "Zürich", Color = "#E74C3C", IsDraggable = true },
			new MapAnnotation { Latitude = 47.3667, Longitude = 8.5500, Title = "Zürichsee", Color = "#2980B9" },
		]);

		// Test hooks: SIMCTL_CHILD_DEMO_GEOJSON=1 / SIMCTL_CHILD_DEMO_CLUSTER=1 activate
		// the demos on launch (the iOS simulator cannot inject taps).
		if (Environment.GetEnvironmentVariable("DEMO_GEOJSON") == "1")
			OnToggleGeoJson(this, EventArgs.Empty);
		if (Environment.GetEnvironmentVariable("DEMO_CLUSTER") == "1")
			OnToggleCluster(this, EventArgs.Empty);
		if (Environment.GetEnvironmentVariable("DEMO_BUBBLE") == "1")
			OnToggleBubble(this, EventArgs.Empty);
		if (Environment.GetEnvironmentVariable("DEMO_OFFLINE") == "1")
			OnDownloadOffline(this, EventArgs.Empty);
		if (Environment.GetEnvironmentVariable("DEMO_ELEVATION") == "1")
			OnShowElevationDemo(this, EventArgs.Empty);
		if (Environment.GetEnvironmentVariable("DEMO_REGIONS") == "1")
			OnShowOfflineRegions(this, EventArgs.Empty);
		// Forces a Style load error so MapLoadError can be verified without taking the
		// device offline (the iOS simulator has no airplane mode).
		if (Environment.GetEnvironmentVariable("DEMO_LOADERROR") == "1")
			Map.StyleUri = "mapbox://styles/mapbox/this-style-does-not-exist";
		if (Environment.GetEnvironmentVariable("DEMO_AUTOFIT") == "1")
			OnToggleAutoFit(this, EventArgs.Empty);
		if (Environment.GetEnvironmentVariable("DEMO_FOLLOW") == "1")
			OnToggleFollow(this, EventArgs.Empty);

		Map.Polylines.Add(new MapPolyline
		{
			Points =
			[
				new MapPosition(47.3769, 8.5417),   // Zürich HB
				new MapPosition(47.3660, 8.5450),   // Bürkliplatz
				new MapPosition(47.3550, 8.5530),   // Seefeld
				new MapPosition(47.3400, 8.5570),   // Zollikon
				new MapPosition(47.3200, 8.5520),   // Küsnacht
			],
			Title = "Seeuferweg",
			Color = "#E67E22",
			Width = 5,
		});

		Map.Polygons.Add(new MapPolygon
		{
			Points =
			[
				new MapPosition(47.3660, 8.5410),
				new MapPosition(47.3660, 8.5560),
				new MapPosition(47.3330, 8.5620),
				new MapPosition(47.3310, 8.5450),
			],
			Title = "Unteres Seebecken",
			FillColor = "#9B59B6",
			FillOpacity = 0.35,
			StrokeColor = "#6C3483",
		});
	}

	/// <summary>
	/// Cinematic promo flight: 3D terrain over the Lauterbrunnen valley, a slow
	/// pan, then a fly-to towards Bern during which the pitch interpolates to 0 —
	/// the 3D → 2D transition happens mid-flight.
	/// </summary>
	private async Task RunPromoFlightAsync()
	{
		if (!sheetCollapsed)
			OnSheetHandleTapped(this, new TappedEventArgs(null));

		Map.TerrainEnabled = true;
		PaintChip(Fab3D, true);
		StatusLabel.Text = "3D-Terrain · Lauterbrunnental";
		Map.Camera = new MapCameraPosition(46.60, 7.91, 12.2, bearing: 0, pitch: 68);

		// Let terrain + tiles settle, then a slow cinematic pan.
		await Task.Delay(3500);
		Map.FlyTo(new MapCameraPosition(46.585, 7.905, 12.6, bearing: 40, pitch: 66), 7000);
		await Task.Delay(7600);

		// Flight to Bern — pitch eases 66 -> 0 on the way (3D becomes 2D).
		StatusLabel.Text = "FlyTo Bern — Übergang in 2D";
		Map.FlyTo(new MapCameraPosition(46.9480, 7.4474, 12.5, bearing: 0, pitch: 0), 11000);
		await Task.Delay(11600);

		Map.TerrainEnabled = false;
		PaintChip(Fab3D, false);
		StatusLabel.Text = "2D · Bern — Indiko.Maui.Controls.MapBox";
	}

	/// <summary>
	/// Surfaces load failures that would otherwise be invisible — offline, every tile
	/// outside a downloaded region raises one, so tile errors are counted rather than
	/// shown one by one.
	/// </summary>
	private int tileErrorCount;

	private void OnMapLoadError(object? sender, MapLoadErrorEventArgs e)
	{
		if (e.Type == MapLoadErrorType.Tile)
		{
			tileErrorCount++;
			StatusLabel.Text = $"\u26A0 {tileErrorCount} Kachel(n) nicht geladen — {e.Message}";
			return;
		}

		StatusLabel.Text = e.SourceId is { } source
			? $"\u26A0 {e.Type} '{source}': {e.Message}"
			: $"\u26A0 {e.Type}: {e.Message}";
	}

	/// <summary>
	/// Restricts the camera to Switzerland and zoom 7–14, then flies towards Paris to show
	/// the clamping: the camera stops at the western edge of the box instead.
	/// </summary>
	private async void OnToggleCameraLimit(object? sender, EventArgs e)
	{
		var enable = Map.CameraBounds is null;
		PaintChip(ChipCameraLimit, enable);

		if (!enable)
		{
			Map.CameraBounds = null;
			Map.MinZoom = null;
			Map.MaxZoom = null;
			StatusLabel.Text = "Kamera-Limit aufgehoben";
			return;
		}

		Map.CameraBounds = new MapBounds(45.80, 5.95, 47.81, 10.50);
		Map.MinZoom = 7;
		Map.MaxZoom = 14;
		StatusLabel.Text = "Limit: Schweiz, Zoom 7–14 — FlyTo Paris…";

		Map.FlyTo(new MapCameraPosition(48.8566, 2.3522, 10), 2000);
		await Task.Delay(2600);

		var camera = Map.CurrentCamera;
		StatusLabel.Text = camera is null
			? "Limit aktiv"
			: $"Limit aktiv — Paris geblockt bei {camera.Latitude:F2}, {camera.Longitude:F2} (Zoom {camera.Zoom:F1})";
	}

	/// <summary>
	/// The bottom sheet covers the map's lower edge, where Mapbox puts the logo and the
	/// attribution button by default — so move them (and the scale bar) to the top. Setting
	/// the properties back to null restores the SDK defaults.
	/// </summary>
	private void OnToggleOrnamentPlacement(object? sender, EventArgs e)
	{
		var moved = Map.LogoPlacement is null;
		PaintChip(ChipOrnamentPos, moved);

		if (moved)
		{
			Map.ScaleBarPlacement = new MapOrnamentPlacement(MapOrnamentPosition.TopRight, 8, 110);
			Map.LogoPlacement = new MapOrnamentPlacement(MapOrnamentPosition.TopLeft, 8, 160);
			Map.AttributionPlacement = new MapOrnamentPlacement(MapOrnamentPosition.TopLeft, 150, 160);
			StatusLabel.Text = "Ornamente oben — nicht mehr hinter dem Sheet";
		}
		else
		{
			Map.ScaleBarPlacement = null;
			Map.LogoPlacement = null;
			Map.AttributionPlacement = null;
			StatusLabel.Text = "Ornamente zurück auf SDK-Standard";
		}
	}

	/// <summary>
	/// The bottom sheet hides the lower ~420 units of the map, so a centred camera is not
	/// centred for the user. Padding the camera moves its anchor into the visible strip —
	/// the marker dropped at the camera coordinate shows where that anchor actually is.
	/// </summary>
	private void OnToggleCameraPadding(object? sender, EventArgs e)
	{
		var padded = Map.CameraPadding.Bottom == 0;
		PaintChip(ChipCameraPadding, padded);

		Map.CameraPadding = padded ? new Thickness(0, 0, 0, 420) : new Thickness(0);

		const double latitude = 47.3769;
		const double longitude = 8.5417;
		Map.Annotations.Add(new MapAnnotation
		{
			Latitude = latitude,
			Longitude = longitude,
			Title = "Kamera-Anker",
			Color = padded ? "#16A34A" : "#DC2626",
		});
		Map.FlyTo(new MapCameraPosition(latitude, longitude, 13), 800);

		StatusLabel.Text = padded
			? "Camera-Padding unten 420 — Anker über dem Sheet"
			: "Camera-Padding aus — Anker in der Viewport-Mitte";
	}

	/// <summary>
	/// Cycles the label language and flies to the Geneva area, where the difference is
	/// obvious: Genève / Genf / Geneva. Only names Mapbox has a translation for change.
	/// </summary>
	private static readonly string?[] Languages = [null, "de", "fr", "en"];

	private int languageIndex;

	private void OnCycleLanguage(object? sender, EventArgs e)
	{
		languageIndex = (languageIndex + 1) % Languages.Length;
		var language = Languages[languageIndex];

		Map.MapLanguage = language;
		PaintChip(ChipLanguage, language is not null);
		ChipLanguage.Text = $"Sprache: {language ?? "Standard"}";

		Map.FlyTo(new MapCameraPosition(46.35, 6.80, 8), 900);
		StatusLabel.Text = language is null
			? "Labels in der Landessprache (Style-Standard)"
			: $"Labels auf '{language}' übersetzt";
	}

	/// <summary>
	/// One source, one circle layer, no per-feature code: radius interpolates on the
	/// "magnitude" property and colour matches on "category". The label layer builds its
	/// text with an expression too, which also shows that a Properties entry overrides the
	/// typed TextField.
	/// </summary>
	private bool expressionsActive;

	private void OnToggleExpressions(object? sender, EventArgs e)
	{
		expressionsActive = !expressionsActive;
		PaintChip(ChipExpressions, expressionsActive);

		if (!expressionsActive)
		{
			Map.RemoveLayer("expr-labels");
			Map.RemoveLayer("expr-circles");
			Map.RemoveGeoJsonSource("expr-source");
			StatusLabel.Text = "Expressions-Demo entfernt";
			return;
		}

		Map.AddGeoJsonSource("expr-source", ExpressionGeoJson);

		var circles = new MapLayer
		{
			Id = "expr-circles",
			SourceId = "expr-source",
			Type = MapLayerType.Circle,
		};
		circles.Properties["circle-radius"] = MapExpression.Interpolate(
			MapExpression.Get("magnitude"), (1, 6.0), (6, 30.0));
		circles.Properties["circle-color"] = MapExpression.Match(
			MapExpression.Get("category"),
			[("see", "#2563EB"), ("berg", "#16A34A"), ("stadt", "#F59E0B")],
			"#9CA3AF");
		circles.Properties["circle-opacity"] = 0.85;
		circles.Properties["circle-stroke-width"] = 2.0;
		circles.Properties["circle-stroke-color"] = "#FFFFFF";
		Map.AddLayer(circles);

		var labels = new MapLayer
		{
			Id = "expr-labels",
			SourceId = "expr-source",
			Type = MapLayerType.Symbol,
			TextField = "name",
			TextSize = 12,
			TextColor = "#111827",
			TextHaloColor = "#FFFFFF",
			TextHaloWidth = 1.6,
			AllowOverlap = true,
		};
		labels.Properties["text-field"] = MapExpression.Raw(
			"[\"concat\",[\"get\",\"name\"],\" \\u00b7 \",[\"to-string\",[\"get\",\"magnitude\"]]]");
		labels.Properties["text-offset"] = MapExpression.Raw("[0,1.6]");
		Map.AddLayer(labels);

		Map.FlyTo(new MapCameraPosition(47.3769, 8.5400, 11.4), 1200);
		StatusLabel.Text = "Expressions: Radius nach magnitude, Farbe nach category";
	}

	/// <summary>Points carrying the properties the expressions above read.</summary>
	private const string ExpressionGeoJson = """
	{
	  "type": "FeatureCollection",
	  "features": [
	    {"type":"Feature","properties":{"name":"HB","magnitude":6,"category":"stadt"},
	     "geometry":{"type":"Point","coordinates":[8.5417,47.3769]}},
	    {"type":"Feature","properties":{"name":"Enge","magnitude":3,"category":"stadt"},
	     "geometry":{"type":"Point","coordinates":[8.5310,47.3640]}},
	    {"type":"Feature","properties":{"name":"Wollishofen","magnitude":2,"category":"see"},
	     "geometry":{"type":"Point","coordinates":[8.5360,47.3430]}},
	    {"type":"Feature","properties":{"name":"Oerlikon","magnitude":4,"category":"stadt"},
	     "geometry":{"type":"Point","coordinates":[8.5500,47.4100]}},
	    {"type":"Feature","properties":{"name":"Höngg","magnitude":2,"category":"berg"},
	     "geometry":{"type":"Point","coordinates":[8.4970,47.4010]}},
	    {"type":"Feature","properties":{"name":"Riesbach","magnitude":5,"category":"see"},
	     "geometry":{"type":"Point","coordinates":[8.5560,47.3560]}},
	    {"type":"Feature","properties":{"name":"Affoltern","magnitude":1,"category":"berg"},
	     "geometry":{"type":"Point","coordinates":[8.5020,47.4210]}},
	    {"type":"Feature","properties":{"name":"Zürichberg","magnitude":3,"category":"berg"},
	     "geometry":{"type":"Point","coordinates":[8.5760,47.3830]}}
	  ]
	}
	""";

	/// <summary>
	/// A third-party raster tile server (swisstopo) drawn over the basemap — the usual
	/// reason to add a source: official maps a style does not ship.
	/// </summary>
	private bool rasterSourceActive;

	private void OnToggleRasterSource(object? sender, EventArgs e)
	{
		rasterSourceActive = !rasterSourceActive;
		PaintChip(ChipSwisstopo, rasterSourceActive);

		if (!rasterSourceActive)
		{
			Map.RemoveLayer("swisstopo-layer");
			Map.RemoveSource("swisstopo");
			StatusLabel.Text = "Swisstopo-Raster entfernt";
			return;
		}

		var source = new MapSource
		{
			Id = "swisstopo",
			Type = MapSourceType.Raster,
			TileSize = 256,
			MaxZoom = 18,
			Attribution = "\u00a9 swisstopo",
		};
		source.Tiles.Add("https://wmts.geo.admin.ch/1.0.0/ch.swisstopo.pixelkarte-farbe/" +
			"default/current/3857/{z}/{x}/{y}.jpeg");
		Map.AddSource(source);

		var layer = new MapLayer
		{
			Id = "swisstopo-layer",
			SourceId = "swisstopo",
			Type = MapLayerType.Raster,
		};
		layer.Properties["raster-opacity"] = 0.9;
		Map.AddLayer(layer);

		Map.FlyTo(new MapCameraPosition(46.80, 8.22, 8), 1200);
		StatusLabel.Text = "Swisstopo-Raster-Source über dem Basemap";
	}

	/// <summary>
	/// Mapbox' own vector tileset. Vector sources need SourceLayer — the layer inside the
	/// tileset — which is what separates them from raster and GeoJSON.
	/// </summary>
	private bool vectorSourceActive;

	private void OnToggleVectorSource(object? sender, EventArgs e)
	{
		vectorSourceActive = !vectorSourceActive;
		PaintChip(ChipVectorTiles, vectorSourceActive);

		if (!vectorSourceActive)
		{
			Map.RemoveLayer("buildings-layer");
			Map.RemoveSource("mapbox-streets");
			StatusLabel.Text = "Vector-Source entfernt";
			return;
		}

		Map.AddSource(new MapSource
		{
			Id = "mapbox-streets",
			Type = MapSourceType.Vector,
			Url = "mapbox://mapbox.mapbox-streets-v8",
		});

		var buildings = new MapLayer
		{
			Id = "buildings-layer",
			SourceId = "mapbox-streets",
			SourceLayer = "building",
			Type = MapLayerType.Fill,
		};
		buildings.Properties["fill-color"] = "#DC2626";
		buildings.Properties["fill-opacity"] = 0.55;
		Map.AddLayer(buildings);

		Map.FlyTo(new MapCameraPosition(47.3769, 8.5400, 15.5), 1200);
		StatusLabel.Text = "Vector-Source: Mapbox Streets, Layer 'building'";
	}

	private void OnPolylineClicked(object? sender, PolylineClickedEventArgs e)
	{
		StatusLabel.Text = $"Polyline: {e.Polyline.Title ?? e.Polyline.Id}";
	}

	private void OnPolygonClicked(object? sender, PolygonClickedEventArgs e)
	{
		StatusLabel.Text = $"Polygon: {e.Polygon.Title ?? e.Polygon.Id}";
	}

	private void OnMapClicked(object? sender, MapClickedEventArgs e)
	{
		var elevation = Map.QueryTerrainElevation(e.Latitude, e.Longitude);
		StatusLabel.Text = elevation is { } meters
			? $"Klick: {e.Latitude:F5}, {e.Longitude:F5} · {meters:F0} m ü. M."
			: $"Klick: {e.Latitude:F5}, {e.Longitude:F5}";
		Map.Annotations.Add(new MapAnnotation
		{
			Latitude = e.Latitude,
			Longitude = e.Longitude,
			Title = "Neuer Marker",
			Color = "#27AE60",
		});
	}

	private void OnMapLongPressed(object? sender, MapClickedEventArgs e)
	{
		StatusLabel.Text = $"FlyTo: {e.Latitude:F5}, {e.Longitude:F5}";
		Map.FlyTo(new MapCameraPosition(e.Latitude, e.Longitude, 14), 1500);
	}

	private void OnAnnotationDragged(object? sender, AnnotationDraggedEventArgs e)
	{
		StatusLabel.Text = $"Marker verschoben: {e.Latitude:F5}, {e.Longitude:F5}";
	}

	private void OnAnnotationClicked(object? sender, AnnotationClickedEventArgs e)
	{
		StatusLabel.Text = $"Marker: {e.Annotation.Title ?? e.Annotation.Id}";
	}

	private void OnFlyToZurich(object? sender, EventArgs e)
	{
		Map.FlyTo(new MapCameraPosition(47.3769, 8.5417, 12), 2000);
	}

	private void OnFlyToBern(object? sender, EventArgs e)
	{
		Map.FlyTo(new MapCameraPosition(46.9480, 7.4474, 12), 2000);
	}

	private void OnClearMarkers(object? sender, EventArgs e)
	{
		Map.Annotations.Clear();
		StatusLabel.Text = "Marker gelöscht";
	}

	private async void OnOpenNavigationTest(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new NavigationTestPage());
	}

	private bool geoJsonActive;

	// Tram/S-Bahn-Halte als Points + eine Verbindung als LineString (GeoJSON ist [lng, lat]!)
	private const string DemoGeoJson = """
	{
		"type": "FeatureCollection",
		"features": [
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5402, 47.3782] }, "properties": { "name": "Hauptbahnhof" } },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5317, 47.3859] }, "properties": { "name": "Wipkingen" } },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5482, 47.3903] }, "properties": { "name": "Oerlikon" } },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5610, 47.3846] }, "properties": { "name": "Stettbach" } },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5170, 47.3910] }, "properties": { "name": "Hardbrücke" } },
			{ "type": "Feature", "geometry": { "type": "LineString", "coordinates": [
				[8.5170, 47.3910], [8.5317, 47.3859], [8.5402, 47.3782],
				[8.5482, 47.3903], [8.5610, 47.3846]
			] }, "properties": {} }
		]
	}
	""";

	private void OnToggleGeoJson(object? sender, EventArgs e)
	{
		if (geoJsonActive)
		{
			Map.RemoveLayer("demo-labels");
			Map.RemoveLayer("demo-circles");
			Map.RemoveLayer("demo-route");
			Map.RemoveGeoJsonSource("demo-source");
			StatusLabel.Text = "GeoJSON entfernt";
		}
		else
		{
			Map.AddGeoJsonSource("demo-source", DemoGeoJson);
			Map.AddLayer(new MapLayer
			{
				Id = "demo-route",
				SourceId = "demo-source",
				Type = MapLayerType.Line,
				Color = "#16A085",
				LineWidth = 4,
			});
			Map.AddLayer(new MapLayer
			{
				Id = "demo-circles",
				SourceId = "demo-source",
				Type = MapLayerType.Circle,
				Color = "#C0392B",
				CircleRadius = 9,
				Opacity = 0.9,
			});
			Map.AddLayer(new MapLayer
			{
				Id = "demo-labels",
				SourceId = "demo-source",
				Type = MapLayerType.Symbol,
				TextField = "name",
				TextSize = 13,
				TextColor = "#1F2937",
				TextHaloColor = "#FFFFFF",
				TextHaloWidth = 1.6,
				AllowOverlap = true,
			});
			Map.FlyTo(new MapCameraPosition(47.3855, 8.5400, 13), 1200);
			StatusLabel.Text = "GeoJSON-Source + Circle/Line-Layer aktiv";
		}

		geoJsonActive = !geoJsonActive;
		PaintChip(ChipGeoJson, geoJsonActive);
	}

	private bool clusterActive;

	private static string BuildClusterGeoJson()
	{
		// 8 points each around Zürich, Luzern, Bern, Lausanne and Basel
		(double Lat, double Lng)[] centers =
		[
			(47.3769, 8.5417), (47.0502, 8.3093), (46.9480, 7.4474), (46.5197, 6.6323), (47.5596, 7.5886),
		];

		var builder = new System.Text.StringBuilder("{\"type\":\"FeatureCollection\",\"features\":[");
		var first = true;
		foreach (var (lat, lng) in centers)
		{
			for (var i = 0; i < 8; i++)
			{
				if (!first)
					builder.Append(',');
				first = false;

				var pointLat = lat + (i % 3) * 0.020 - 0.020;
				var pointLng = lng + (i % 4) * 0.025 - 0.0375;
				builder.Append("{\"type\":\"Feature\",\"properties\":{},\"geometry\":{\"type\":\"Point\",\"coordinates\":[")
					.Append(pointLng.ToString(System.Globalization.CultureInfo.InvariantCulture))
					.Append(',')
					.Append(pointLat.ToString(System.Globalization.CultureInfo.InvariantCulture))
					.Append("]}}");
			}
		}

		return builder.Append("]}").ToString();
	}

	private void OnToggleCluster(object? sender, EventArgs e)
	{
		if (clusterActive)
		{
			Map.RemoveClusteredSource("demo-cluster");
			StatusLabel.Text = "Cluster entfernt";
		}
		else
		{
			Map.AddClusteredSource(new MapClusterSource
			{
				SourceId = "demo-cluster",
				GeoJson = BuildClusterGeoJson(),
				ClusterRadius = 50,
				ClusterColor = "#2563EB",
				ClusterTextColor = "#FFFFFF",
				PointColor = "#DC2626",
				PointRadius = 6,
			});
			Map.FlyTo(new MapCameraPosition(46.95, 7.9, 7), 1200);
			StatusLabel.Text = "Cluster aktiv — tippe auf einen Cluster zum Zoomen";
		}

		clusterActive = !clusterActive;
		PaintChip(ChipCluster, clusterActive);
	}

	private bool bubbleActive;

	private void OnToggleBubble(object? sender, EventArgs e)
	{
		if (bubbleActive)
		{
			var bubble = Map.ViewAnnotations.FirstOrDefault(a => a.Id == "bubble-hb");
			if (bubble is not null)
				Map.ViewAnnotations.Remove(bubble);
			StatusLabel.Text = "Bubble entfernt";
		}
		else
		{
			var label = new Label
			{
				Text = "🚉 Zürich HB",
				TextColor = Colors.White,
				FontSize = 14,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
			};
			var border = new Border
			{
				Background = Color.FromArgb("#1F2937"),
				Stroke = Color.FromArgb("#F59E0B"),
				StrokeThickness = 2,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
				Content = label,
			};
			var tap = new TapGestureRecognizer();
			tap.Tapped += (_, _) => StatusLabel.Text = "Bubble getippt! (MAUI-Gesture in View-Annotation)";
			border.GestureRecognizers.Add(tap);

			Map.ViewAnnotations.Add(new MapViewAnnotation
			{
				Id = "bubble-hb",
				Latitude = 47.3779,
				Longitude = 8.5403,
				Content = border,
				Width = 150,
				Height = 44,
			});
			Map.FlyTo(new MapCameraPosition(47.3779, 8.5403, 13), 1000);
			StatusLabel.Text = "View-Annotation aktiv — tippe die Bubble";
		}

		bubbleActive = !bubbleActive;
		PaintChip(ChipBubble, bubbleActive);
	}

	private void OnToggleAutoFit(object? sender, EventArgs e)
	{
		Map.AutoFitBounds = !Map.AutoFitBounds;
		PaintChip(ChipAutoFit, Map.AutoFitBounds);
		StatusLabel.Text = Map.AutoFitBounds
			? "AutoFit aktiv — Kamera folgt dem Karteninhalt"
			: "AutoFit aus";
	}

	private async void OnToggleFollow(object? sender, EventArgs e)
	{
		if (!Map.FollowPuck)
		{
			var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
			if (status != PermissionStatus.Granted)
			{
				StatusLabel.Text = "Follow-Modus: Standort-Berechtigung fehlt";
				return;
			}
		}

		Map.FollowPuck = !Map.FollowPuck;
	}

	private void OnFollowPuckChanged(object? sender, FollowPuckChangedEventArgs e)
	{
		PaintChip(FabFollow, e.IsActive);
		StatusLabel.Text = e.IsActive
			? "Follow-Modus aktiv — Karte folgt deiner Position"
			: "Follow-Modus beendet";
	}

	private void OnDownloadOffline(object? sender, EventArgs e)
	{
		Map.DownloadOfflineRegion(new MapOfflineRegion
		{
			Id = "zuerich-region",
			// Must be the style the map actually shows — the style pack is downloaded
			// per style, and offline a missing style pack leaves the map blank.
			StyleUri = Map.StyleUri,
			MinLatitude = 47.32,
			MinLongitude = 8.46,
			MaxLatitude = 47.43,
			MaxLongitude = 8.62,
			MinZoom = 6,
			MaxZoom = 12,
			Name = "Zürich",
			IncludeTerrain = true,
		});
		StatusLabel.Text = "Offline-Download gestartet…";
	}

	private void OnOfflineProgress(object? sender, OfflineRegionProgressEventArgs e)
	{
		StatusLabel.Text = $"Offline-Download: {e.Progress:P0}";
	}

	private void OnOfflineCompleted(object? sender, OfflineRegionCompletedEventArgs e)
	{
		StatusLabel.Text = e.Success
			? $"Offline-Region '{e.RegionId}' fertig geladen ✓"
			: $"Offline-Download fehlgeschlagen: {e.ErrorMessage}";

		if (e.Success)
			_ = ShowOfflineRegionsAsync();
	}

	private async Task ShowOfflineRegionsAsync()
	{
		var regions = await MapboxOffline.GetRegionsAsync();
		if (regions.Count == 0)
		{
			StatusLabel.Text = "Keine Offline-Regionen auf dem Gerät";
			await DisplayAlertAsync("Offline-Regionen",
				"Noch keine Region geladen — erst 'Offline laden' antippen.", "OK");
			return;
		}

		var lines = regions.Select(r =>
			$"{r.Region?.Name ?? r.Id}: {r.CompletedResourceSize / 1_048_576.0:F1} MB" +
			(r.Region?.IncludeTerrain == true ? " · Terrain" : string.Empty) +
			(r.IsComplete ? " ✓" : $" ({r.CompletedResourceCount}/{r.RequiredResourceCount})"));
		StatusLabel.Text = $"{regions.Count} Offline-Region(en) auf dem Gerät";
		await DisplayAlertAsync("Offline-Regionen", string.Join(Environment.NewLine, lines), "OK");
	}

	private void OnShowOfflineRegions(object? sender, EventArgs e) => _ = ShowOfflineRegionsAsync();

	/// <summary>Deletes every downloaded region — MapboxOffline works without a map on screen.</summary>
	private async void OnRemoveOfflineRegions(object? sender, EventArgs e)
	{
		var regions = await MapboxOffline.GetRegionsAsync();
		if (regions.Count == 0)
		{
			StatusLabel.Text = "Keine Offline-Regionen zum Löschen";
			return;
		}

		var confirmed = await DisplayAlertAsync("Offline-Regionen löschen",
			$"{regions.Count} Region(en) entfernen?", "Löschen", "Abbrechen");
		if (!confirmed)
			return;

		foreach (var region in regions)
			MapboxOffline.RemoveRegion(region.Id);

		StatusLabel.Text = $"{regions.Count} Offline-Region(en) gelöscht";
	}

	/// <summary>
	/// Demonstrates MapView.QueryTerrainElevation: enables terrain, then reads the
	/// ground elevation at a few known Swiss coordinates. The DEM tile has to be
	/// loaded for the coordinate, so each point is centred before it is queried.
	/// </summary>
	private async void OnShowElevationDemo(object? sender, EventArgs e)
	{
		(double Lat, double Lon, string Name)[] points =
		[
			(46.5934, 7.9088, "Lauterbrunnen, Talboden"),
			(47.2225, 8.7527, "Zürichsee, Seespiegel"),
			(46.5475, 7.9822, "Jungfraujoch"),
		];

		Map.TerrainEnabled = true;
		PaintChip(Fab3D, true);
		PaintChip(ChipElevation, true);

		var lines = new List<string>();
		foreach (var point in points)
		{
			StatusLabel.Text = $"Höhe wird gemessen: {point.Name}…";
			var meters = await ProbeElevationAsync(point.Lat, point.Lon);
			lines.Add(meters is { } value
				? $"{point.Name}: {value:F0} m ü. M."
				: $"{point.Name}: keine DEM-Daten");
		}

		PaintChip(ChipElevation, false);
		StatusLabel.Text = "Höhen gemessen — tippe die Karte für weitere Punkte";
		await DisplayAlertAsync("Terrain-Höhen", string.Join(Environment.NewLine, lines), "OK");
	}

	/// <summary>
	/// Centres a coordinate and polls the elevation until the DEM tile has arrived.
	/// Returns null if no data shows up within roughly five seconds.
	/// </summary>
	private async Task<double?> ProbeElevationAsync(double latitude, double longitude)
	{
		Map.Camera = new MapCameraPosition(latitude, longitude, 13, bearing: 0, pitch: 0);

		for (var attempt = 0; attempt < 10; attempt++)
		{
			await Task.Delay(500);
			if (Map.QueryTerrainElevation(latitude, longitude) is { } meters)
				return meters;
		}
		return null;
	}

	private void OnToggleOrnaments(object? sender, EventArgs e)
	{
		var show = !Map.ShowCompass;
		Map.ShowCompass = show;
		Map.ShowScaleBar = show;
		Map.ShowMapboxLogo = show;
		Map.ShowAttribution = show;
		PaintChip(ChipOrnaments, !show);
		StatusLabel.Text = show ? "Ornaments sichtbar" : "Ornaments ausgeblendet";
	}

	private void OnToggle3D(object? sender, EventArgs e)
	{
		Map.TerrainEnabled = !Map.TerrainEnabled;
		PaintChip(Fab3D, Map.TerrainEnabled);
		if (Map.TerrainEnabled)
		{
			// Tilted camera over the Bernese Alps — pitch makes the relief visible.
			Map.FlyTo(new MapCameraPosition(46.60, 7.91, 12, bearing: 0, pitch: 65), 2500);
			StatusLabel.Text = "3D-Terrain aktiv (Lauterbrunnental)";
		}
		else
		{
			var camera = Map.CurrentCamera;
			if (camera is not null)
				Map.FlyTo(new MapCameraPosition(camera.Latitude, camera.Longitude, camera.Zoom,
					camera.Bearing, pitch: 0), 800);
			StatusLabel.Text = "3D-Terrain aus";
		}
	}

	private void OnShowVisibleBounds(object? sender, EventArgs e)
	{
		var bounds = Map.GetVisibleBounds();
		StatusLabel.Text = bounds is null
			? "VisibleBounds: nicht verfügbar"
			: $"Bounds: {bounds.MinLatitude:F4},{bounds.MinLongitude:F4} → " +
			  $"{bounds.MaxLatitude:F4},{bounds.MaxLongitude:F4}";
	}
}
