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
			new MapAnnotation { Latitude = 47.3769, Longitude = 8.5417, Title = "Zürich", Color = "#E74C3C" },
			new MapAnnotation { Latitude = 47.3667, Longitude = 8.5500, Title = "Zürichsee", Color = "#2980B9" },
		]);

		// Test hook: SIMCTL_CHILD_DEMO_GEOJSON=1 activates the GeoJSON demo on launch
		// (the iOS simulator cannot inject taps).
		if (Environment.GetEnvironmentVariable("DEMO_GEOJSON") == "1")
			OnToggleGeoJson(this, EventArgs.Empty);

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
}
