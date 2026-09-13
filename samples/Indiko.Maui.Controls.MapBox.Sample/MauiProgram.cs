using Indiko.Maui.Controls.MapBox;
using Microsoft.Extensions.Logging;

namespace Indiko.Maui.Controls.MapBox.Sample;

public static class MauiProgram
{
	// Token lives in MapboxToken.cs (git-ignored) — copy MapboxToken.cs.template to create it.
	private const string MapboxAccessToken = MapboxToken.Value;

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
