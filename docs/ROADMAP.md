# Roadmap — remaining gaps against the Mapbox SDK

Stand: 2026-11-11, nach Release **1.15.0**. Quelle der Wahrheit ist die Feature-Matrix im
[README](../README.md#feature-comparison); diese Datei ergänzt sie um Aufwand, Wert und die
Fallstricke, die bei der jeweiligen Umsetzung schon bekannt sind.

## Erledigt (1.9.0 – 1.15.0)

| Release | Inhalt |
|---|---|
| 1.9.0 | Terrain-Höhenabfrage, Offline-Regionen mit Terrain, `MapboxOffline` |
| 1.10.0 | `MapLoadError`, Kamera-Constraints, Ornament-Placement, `CameraPadding`, `MapLanguage` |
| 1.11.0 | Data-driven Styling: `MapExpression` + `MapLayer.Properties` |
| 1.12.0 | Raster-/Vector-/Image-Sources, Raster- und Hillshade-Layer, `SourceLayer` |
| 1.13.0 | Heatmap- und Fill-Extrusion-Layer |
| 1.14.0 | `QueryFeaturesAsync`, Feature-State, Gesten-Detail, Standard-Style-Config |
| 1.15.0 | `Projection` (Globus), `Atmosphere`, `Sky` |

Die Matrix ist damit von 13 auf 6 offene Zeilen geschrumpft.

## Offen

### Snapshotter (statische Kartenbilder)

**Aufwand M–L · Wert mittel.** Eigenständig genug für einen eigenen Lauf.

Der einzige verbleibende Punkt, der **neue Mechanik** braucht: Bild-Bytes müssen *zurück*
über die Facade-Grenze. Bisher fliessen Bilder nur hinein (Marker-Icons als Base64). Zu
klären ist, ob der Rückweg als Base64-String über den bestehenden `IKStringCallback` geht
(einfach, aber speicherhungrig bei grossen Bildern) oder über einen Dateipfad, den die
Facade schreibt und C# liest.

### Custom-Puck-Aussehen

**Aufwand M · Wert mittel.** Vorsicht: braucht Bitmaps über die Facade-Grenze — genau das
Problem, an dem die Image-Sources in 1.12.0 gescheitert sind (Mapbox v11 hat
`ImageSource.updateImage` auf Android entfernt). Vor dem Start prüfen, ob
`LocationPuck2D` auf **beiden** Plattformen ein Bild aus Bytes akzeptiert; sonst droht
dieselbe „funktioniert nur auf einer Seite"-Sackgasse.

### Grössen-Schätzung vor dem Offline-Download

**Aufwand S–M · Wert mittel.** Mapbox bietet keine fertige „estimate"-API. Realistisch ist
eine Hochrechnung aus Bounding-Box und Zoom-Bereich (Kachelanzahl × mittlere Kachelgrösse).
Das ist eine Schätzung, keine Zusage — entsprechend benennen und dokumentieren, sonst
erzeugt die API falsche Erwartungen.

### Circle-Annotations als Objekte

**Aufwand M · Wert niedrig.** Wird durch GeoJSON-Circle-Layer bereits abgedeckt, seit
1.11.0 sogar datengesteuert. Nur sinnvoll, wenn jemand die Annotation-Semantik
(Drag, Klick-Events pro Objekt) explizit braucht.

### Style-JSON-Strings

**Aufwand S · Wert niedrig.** Heute nur Style-URIs. Ein kompletter Style als JSON-String
ist ein Nischenfall; `AddSource` + `AddLayer` + `Properties` decken das meiste ab.

### Custom (non-Mapbox) Projektionen

**Bleibt ❌.** Mapbox Maps SDK v11 kennt nur Globe und Mercator. Die Zeile steht bewusst
als Abgrenzung in der Matrix, nicht als offene Aufgabe.

### Nicht in der Matrix

- **Lights** (ambient/directional für 3D-Beleuchtung) — S–M, niedrig–mittel. Atmosphere
  und Sky sind in 1.15.0 erledigt, die Lichtquellen für 3D-Objekte nicht.
- **Performance-Knöpfe** (Prefetch-Zoom-Delta, Tile-Cache-Budget) — S, niedrig.

## Was das Hinzufügen heute billig macht

Drei Entscheidungen aus 1.11.0–1.12.0 zahlen sich bei jedem weiteren Feature aus:

1. **`MapLayer.Properties`** reicht rohe Style-Spec-Properties an Mapbox' eigenen Setter
   weiter. Ein neuer Layer-Typ kostet deshalb nur einen Konstruktor pro Facade — Heatmap,
   Fill-Extrusion, Raster und Hillshade waren so fast gratis.
2. **Sources werden als Style-Spec-JSON übergeben**, nicht pro Property modelliert.
   `minzoom`, `tileSize`, `scheme` usw. funktionieren ohne Facade-Änderung.
3. **Alles Style-bezogene wird nach `StyleLoaded` neu angewendet.** Mapbox verwirft
   Projection, Atmosphere, Sky, Sprache, Sources, Layer und Import-Config bei jedem
   Style-Reload. Wer etwas Neues ergänzt, muss es in diesen Handler eintragen — sonst
   verschwindet es beim ersten Style-Wechsel.

## Offene Nicht-Feature-Punkte

- **11 Remote-Branches auf GitHub** sind gemergt und könnten gelöscht werden
  (`git push origin --delete <branch>`). Lokal ist bereits aufgeräumt.
- **Worktree** `/Users/indikomac/Projects/_worktrees/Indiko.Maui.Controls.MapBox` hält
  `fix/publish-dispatch-and-metadata` ausgecheckt.
- **README-Troubleshooting** zu „`MapClicked` feuert nicht": ein überlagerndes Seitenlayout
  schluckt die Taps und sieht von aussen exakt wie ein nicht feuerndes Event aus. Dazu
  passen die bereits dokumentierten Fälle (konsumierte Marker-Taps, 300-ms-Fenster nach
  einem Annotation-Tap). Angeboten, noch nicht geschrieben.
