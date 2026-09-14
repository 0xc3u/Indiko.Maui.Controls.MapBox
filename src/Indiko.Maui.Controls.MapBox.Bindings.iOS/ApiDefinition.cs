using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Indiko.Maui.Controls.MapBox.Bindings;

[BaseType(typeof(NSObject), Name = "IKMapbox")]
interface IKMapbox
{
    [Static]
    [Export("setAccessToken:")]
    void SetAccessToken(string token);
}

[BaseType(typeof(NSObject), Name = "IKCameraState")]
[DisableDefaultCtor]
interface IKCameraState
{
    [Export("latitude")]
    double Latitude { get; }

    [Export("longitude")]
    double Longitude { get; }

    [Export("zoom")]
    double Zoom { get; }

    [Export("bearing")]
    double Bearing { get; }

    [Export("pitch")]
    double Pitch { get; }
}

[Protocol]
[Model]
[BaseType(typeof(NSObject), Name = "IKMapEventListener")]
interface IKMapEventListener
{
    [Abstract]
    [Export("onMapReady")]
    void OnMapReady();

    [Abstract]
    [Export("onStyleLoaded")]
    void OnStyleLoaded();

    [Abstract]
    [Export("onMapClick:longitude:")]
    void OnMapClick(double latitude, double longitude);

    [Abstract]
    [Export("onMapLongPress:longitude:")]
    void OnMapLongPress(double latitude, double longitude);

    [Abstract]
    [Export("onMarkerClick:")]
    void OnMarkerClick(string id);

    [Abstract]
    [Export("onPolylineClick:")]
    void OnPolylineClick(string id);

    [Abstract]
    [Export("onPolygonClick:")]
    void OnPolygonClick(string id);

    [Abstract]
    [Export("onCameraChanged:")]
    void OnCameraChanged(IKCameraState camera);

    [Abstract]
    [Export("onOfflineRegionProgress:progress:")]
    void OnOfflineRegionProgress(string id, double progress);

    [Abstract]
    [Export("onOfflineRegionCompleted:success:error:")]
    void OnOfflineRegionCompleted(string id, bool success, [NullAllowed] string? error);

    [Abstract]
    [Export("onFollowPuckChanged:")]
    void OnFollowPuckChanged(bool active);

    [Abstract]
    [Export("onMarkerDragEnd:latitude:longitude:")]
    void OnMarkerDragEnd(string id, double latitude, double longitude);
}

[BaseType(typeof(UIView), Name = "IKMapView")]
[DisableDefaultCtor]
interface IKMapView
{
    [Export("initWithFrame:styleUri:latitude:longitude:zoom:bearing:pitch:")]
    NativeHandle Constructor(CGRect frame, string styleUri, double latitude, double longitude,
        double zoom, double bearing, double pitch);

    [Wrap("WeakListener")]
    [NullAllowed]
    IKMapEventListener Listener { get; set; }

    [NullAllowed]
    [Export("listener", ArgumentSemantic.Weak)]
    NSObject WeakListener { get; set; }

    [Export("setStyleUri:")]
    void SetStyleUri(string uri);

    [Export("setCamera:longitude:zoom:bearing:pitch:")]
    void SetCamera(double latitude, double longitude, double zoom, double bearing, double pitch);

    [Export("flyTo:longitude:zoom:bearing:pitch:durationMs:")]
    void FlyTo(double latitude, double longitude, double zoom, double bearing, double pitch, double durationMs);

    [Export("fitBounds:minLng:maxLat:maxLng:padding:durationMs:")]
    void FitBounds(double minLat, double minLng, double maxLat, double maxLng,
        double padding, double durationMs);

    [Export("setMarkersJson:")]
    void SetMarkers(string json);

    [Export("clearMarkers")]
    void ClearMarkers();

    [Export("setPolylinesJson:")]
    void SetPolylines(string json);

    [Export("clearPolylines")]
    void ClearPolylines();

    [Export("setPolygonsJson:")]
    void SetPolygons(string json);

    [Export("clearPolygons")]
    void ClearPolygons();

    [Export("addGeoJsonSource:geoJson:")]
    void AddGeoJsonSource(string id, string geoJson);

    [Export("removeGeoJsonSource:")]
    void RemoveGeoJsonSource(string id);

    [Export("addLayerJson:")]
    void AddLayerJson(string json);

    [Export("removeLayer:")]
    void RemoveLayer(string id);

    [Export("addViewAnnotation:view:latitude:longitude:width:height:")]
    void AddViewAnnotation(string id, UIView view, double latitude, double longitude,
        double width, double height);

    [Export("removeViewAnnotation:")]
    void RemoveViewAnnotation(string id);

    [Export("addClusteredSourceJson:")]
    void AddClusteredSourceJson(string json);

    [Export("removeClusteredSource:")]
    void RemoveClusteredSource(string id);

    [Export("downloadOfflineRegionJson:")]
    void DownloadOfflineRegionJson(string json);

    [Export("removeOfflineRegion:")]
    void RemoveOfflineRegion(string id);

    [Export("setUserLocationEnabled:")]
    void SetUserLocationEnabled(bool enabled);

    [Export("setFollowPuck:zoom:trackBearing:")]
    void SetFollowPuck(bool enabled, double zoom, bool trackBearing);

    [Export("setTerrainEnabled:exaggeration:")]
    void SetTerrain(bool enabled, double exaggeration);

    [Export("setOrnamentsCompass:scaleBar:logo:attribution:")]
    void SetOrnaments(bool compass, bool scaleBar, bool logo, bool attribution);

    [Export("visibleBoundsJson")]
    string VisibleBoundsJson();

    [Export("setGesturesScroll:zoom:rotate:pitch:")]
    void SetGestures(bool scroll, bool zoom, bool rotate, bool pitch);

    [Export("destroy")]
    void Destroy();
}
