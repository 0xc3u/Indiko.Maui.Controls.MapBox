# [1.11.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.10.0...v1.11.0) (2026-11-11)

### Features

* data-driven layer styling with expressions ([91faae8](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/91faae891b9855a5ac0fe1faff98fe83c6acb5a8))


# [1.10.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.9.0...v1.10.0) (2026-11-11)

### Features

* translate place labels with MapLanguage ([dc76add](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/dc76addd63fd7108db4724bc852af8e601395aa9))
* offset the camera anchor with CameraPadding ([c8817b5](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/c8817b5c117a1d33db39414654011cd3cebd0d11))
* place ornaments in any corner with an offset ([bb22a66](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/bb22a66ae019707f58a63a5d728e4e9e29d24326))
* constrain the camera to bounds and a zoom/pitch range ([62a8c94](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/62a8c94414ed799ddf02d75a84a86db485275b00))
* report map load errors instead of failing silently ([d8b5d3c](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/d8b5d3c292550b5955463173893cb0ddb8e7881b))


# [1.9.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.8.4...v1.9.0) (2026-11-11)

### Features

* terrain elevation query and offline region management ([b72468c](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/b72468cd9d6b1fd109b05bb9c0e7b32e769854b1))

### Bug Fixes

* **ios:** compile the Swift facade and verify terrain/offline on device ([76ea3e7](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/76ea3e70d94ee18a1e2d764d6cf323dd88fa8c2c))


## [1.8.4](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.8.3...v1.8.4) (2026-11-02)

### Bug Fixes

* **package:** add android/ios tags and release notes link to the NuGet metadata ([4cbfe90](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/4cbfe9057b8dd660ca8a63329d9affcb70948cdc))


## [1.8.3](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.8.2...v1.8.3) (2026-11-02)

### Bug Fixes

* **ios:** rebuild the facade XCFramework with the deferred teardown ([b93bacb](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/b93bacb8f9f424593c027da80d824f26e9188717))


## [1.8.2](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.8.1...v1.8.2) (2026-11-02)

### Bug Fixes

* defer the native map teardown so a reopened page keeps rendering ([372f9df](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/372f9df0e3dca55532bf97d38b1440f7307f8b66))
* **deps:** align AppCompat with MAUI's AndroidX graph ([678617a](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/678617ae29819f9c5570c3870a0954d9bf8cd474))
* **deps:** update NuGet packages to their latest stable versions ([b8f21b1](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/b8f21b1a7aaafafe7fa8f5af239ea1b371ceb660))


## [1.8.1](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.8.0...v1.8.1) (2026-11-02)

### Bug Fixes

* **android:** use Mapbox -ndk27 artifacts for 16 KB page support ([44d1545](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/44d1545eb1fa89d2d182501c746de73a2fb51a15))


# [1.8.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.7.0...v1.8.0) (2026-09-15)


### Features

* allow disabling Mapbox location telemetry via UseMapbox ([ec78401](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/ec78401ff755aee5157b54281188f7ecbdda9752))

# [1.7.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.6.0...v1.7.0) (2026-09-14)


### Features

* 3D terrain — Mapbox DEM with sky atmosphere and configurable exaggeration ([b9079e5](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/b9079e57aebc8c3fb86755d126b71a9c98944096))

# [1.6.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.5.0...v1.6.0) (2026-09-14)


### Features

* ornament visibility, GetVisibleBounds and deferred imperative call queue ([dfaf5cd](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/dfaf5cd8d36fab0e5e40be363fce1d2933e48dbb))

# [1.5.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.4.0...v1.5.0) (2026-09-14)


### Features

* custom marker icons — PNG/JPEG bytes with dp sizing and anchor ([f78a9dc](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/f78a9dc5ab67072a6d5b36b4408bbbeba5945188))
* symbol layers — data-driven labels and icons from feature properties ([2fda146](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/2fda1464d432c8ede3301385499e59ccf32386d2))

# [1.4.0](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/compare/v1.3.0...v1.4.0) (2026-09-14)


### Features

* NuGet package icon (chat-control style) and embedded README ([1cf2511](https://github.com/0xc3u/Indiko.Maui.Controls.MapBox/commit/1cf25116763aafe9998bc39a4f087b8555c13efb))

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
