using Indiko.Maui.Controls.MapBox.Handlers;
using Indiko.Maui.Controls.MapBox.Models;

namespace Indiko.Maui.Controls.MapBox;

/// <summary>
/// Manages downloaded offline regions without a map on screen (e.g. from a settings
/// page). Downloads themselves run through <see cref="MapView.DownloadOfflineRegion"/>.
/// </summary>
public static class MapboxOffline
{
    /// <summary>Lists all offline regions stored on the device.</summary>
    public static Task<IReadOnlyList<MapOfflineRegionInfo>> GetRegionsAsync()
    {
#if ANDROID || IOS
        var completion = new TaskCompletionSource<IReadOnlyList<MapOfflineRegionInfo>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        void OnResult(string json) => completion.TrySetResult(AnnotationSerializer.ParseOfflineRegions(json));

#if ANDROID
        Bindings.IKMapbox.ListOfflineRegions(new StringCallback(OnResult));
#else
        Bindings.IKMapbox.ListOfflineRegions(OnResult);
#endif
        return completion.Task;
#else
        return Task.FromResult<IReadOnlyList<MapOfflineRegionInfo>>([]);
#endif
    }

    /// <summary>
    /// Removes an offline region's tiles. Tiles shared with other regions stay on
    /// disk. To cancel a download that is still running, use
    /// <see cref="MapView.RemoveOfflineRegion"/> on the downloading map instead.
    /// </summary>
    public static void RemoveRegion(string regionId)
    {
        ArgumentException.ThrowIfNullOrEmpty(regionId);
#if ANDROID || IOS
        Bindings.IKMapbox.RemoveOfflineRegion(regionId);
#endif
    }

#if ANDROID
    private sealed class StringCallback(Action<string> onResult) : Java.Lang.Object, Bindings.IKStringCallback
    {
        public void OnResult(string json) => onResult(json);
    }
#endif
}
