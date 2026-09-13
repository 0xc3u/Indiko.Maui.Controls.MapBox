using Indiko.Maui.Controls.MapBox;
using Microsoft.Extensions.Logging;

namespace Indiko.Maui.Controls.MapBox.Sample;

public static class MauiProgram
{
	// TODO: Replace with your Mapbox public access token (pk.…) from https://account.mapbox.com
	private const string MapboxAccessToken = "pk.YOUR_MAPBOX_ACCESS_TOKEN";

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMapbox(MapboxAccessToken)
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
