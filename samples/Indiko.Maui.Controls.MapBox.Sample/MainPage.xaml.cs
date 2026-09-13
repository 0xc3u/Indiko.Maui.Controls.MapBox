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
}
