namespace Indiko.Maui.Controls.MapBox.Sample;

public partial class NavigationTestPage : ContentPage
{
	private static int visits;

	private readonly int visit;

	public NavigationTestPage()
	{
		InitializeComponent();
		visit = ++visits;
	}

	private void OnMapReady(object? sender, EventArgs e)
	{
		StatusLabel.Text = $"Besuch #{visit} — MapReady";
	}

	private void OnStyleLoaded(object? sender, EventArgs e)
	{
		StatusLabel.Text = $"Besuch #{visit} — Style geladen, Karte rendert";
	}

	private async void OnBack(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
