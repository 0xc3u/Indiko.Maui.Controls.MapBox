# [1.3.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.2.0...v1.3.0) (2026-09-14)


### Features

* draggable markers with drag-end event and model sync ([33379bc](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/33379bca555bdc464e2d05caf55b91ea2504471f))

# [1.2.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.1.0...v1.2.0) (2026-09-14)


### Features

* follow-puck mode — camera follows the user location, switchable with state sync ([a776ced](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/a776cedcfbad47f1cf7a051c1295600a8c0aaf1e))

# [1.1.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.0.0...v1.1.0) (2026-09-14)


### Bug Fixes

* apply PrivateAssets=all to binding references only while packing ([9aaab93](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/9aaab93fada252b57c5ca97fef3f4ffb37647307))
* wrap plain-JAR Mapbox dependencies into AARs so their classes reach the app ([89fc31e](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/89fc31e469635d548f637de65e60fda04085bdc1))


### Features

* FitBounds — fit camera to bounds or content, with switchable AutoFitBounds ([34db8b9](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/34db8b9e781d238966d37ec6e1538d61ee92e5b0))

# 1.0.0 (2026-09-13)


### Bug Fixes

* Android runtime — ship cronet-api classes for MapboxCommon HTTP fallback ([7640286](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/76402866a166759d90836b4277860da99b8ecd5e))
* ship Turf.xcframework and MapboxMaps resource bundle with the iOS facade ([0b22f24](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/0b22f24d735712b3ecbe356102b90d9ce442cb2e))


### Features

* click-consumed semantics — annotation and cluster taps suppress MapClicked ([11088f9](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/11088f98bdda82df502a3ce0f7c4dfaf18e6032b))
* full MVVM parity — bindable commands for every MapView event ([fad843e](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/fad843ef26e18ee60b896a8c30829c252f1d678c))
* GeoJSON sources and fill/line/circle style layers ([20367ad](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/20367ad54542b0432d10df8ebca9d75d7fcc3492))
* initial Mapbox MAUI plugin skeleton with native facade bindings ([a4aa0ff](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/a4aa0ffc813d0ff76f07248af59d8873e76076cc))
* offline regions — style pack + tile region download with progress events ([9150e51](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/9150e511e30bac411976cc3bb98a3e0aa791a9f7))
* point clustering with managed layers and tap-to-expand ([ce01797](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/ce01797e2695095ed4d112b18049345e2314dee7))
* polyline and polygon support ([a0a797c](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/a0a797c54e4ffb5fb44f4685e2dc063c9fef381d))
* view annotations — MAUI views anchored to map coordinates ([90881f2](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/90881f2155d5748f93e8b1af8bcb72e404259561))
