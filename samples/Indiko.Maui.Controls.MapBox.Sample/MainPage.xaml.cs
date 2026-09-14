using Indiko.Maui.Controls.MapBox.Models;

namespace Indiko.Maui.Controls.MapBox.Sample;

public partial class MainPage : ContentPage
{
	private static readonly string[] Styles =
	[
		MapStyles.Streets, MapStyles.Dark, MapStyles.SatelliteStreets, MapStyles.Outdoors
	];

	private int styleIndex;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnMapReady(object? sender, EventArgs e)
	{
		StatusLabel.Text = "Karte bereit — tippe für Marker, lang drücken für FlyTo";

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
		StatusLabel.Text = $"Klick: {e.Latitude:F5}, {e.Longitude:F5}";
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

	private void OnToggleStyle(object? sender, EventArgs e)
	{
		styleIndex = (styleIndex + 1) % Styles.Length;
		Map.StyleUri = Styles[styleIndex];
		StatusLabel.Text = $"Style: {Styles[styleIndex]}";
	}

	private void OnClearMarkers(object? sender, EventArgs e)
	{
		Map.Annotations.Clear();
		StatusLabel.Text = "Marker gelöscht";
	}

	private bool geoJsonActive;

	// Tram/S-Bahn-Halte als Points + eine Verbindung als LineString (GeoJSON ist [lng, lat]!)
	private const string DemoGeoJson = """
	{
		"type": "FeatureCollection",
		"features": [
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5402, 47.3782] }, "properties": {} },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5317, 47.3859] }, "properties": {} },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5482, 47.3903] }, "properties": {} },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5610, 47.3846] }, "properties": {} },
			{ "type": "Feature", "geometry": { "type": "Point", "coordinates": [8.5170, 47.3910] }, "properties": {} },
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
			Map.FlyTo(new MapCameraPosition(47.3855, 8.5400, 13), 1200);
			StatusLabel.Text = "GeoJSON-Source + Circle/Line-Layer aktiv";
		}

		geoJsonActive = !geoJsonActive;
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
	}

	private void OnToggleAutoFit(object? sender, EventArgs e)
	{
		Map.AutoFitBounds = !Map.AutoFitBounds;
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
		StatusLabel.Text = e.IsActive
			? "Follow-Modus aktiv — Karte folgt deiner Position"
			: "Follow-Modus beendet";
	}

	private void OnDownloadOffline(object? sender, EventArgs e)
	{
		Map.DownloadOfflineRegion(new MapOfflineRegion
		{
			Id = "zuerich-region",
			StyleUri = MapStyles.Streets,
			MinLatitude = 47.32,
			MinLongitude = 8.46,
			MaxLatitude = 47.43,
			MaxLongitude = 8.62,
			MinZoom = 6,
			MaxZoom = 12,
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
	}
}
