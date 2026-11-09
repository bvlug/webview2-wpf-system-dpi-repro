# WebView2 WPF System-DPI repro — permanent input hit-test offset after mid-session scale change

Minimal standalone repro for a bug in `Microsoft.Web.WebView2.Wpf.WebView2` hosted in a
**System-DPI-aware** WPF application: when the WebView2 control is destroyed and recreated
after a mid-session monitor scale change, the resulting controller has a **permanent input
hit-test offset** that cannot be corrected at runtime.

## Environment

| | |
|---|---|
| Framework | .NET Framework 4.7.2, WPF, **x86** |
| DPI awareness | **System-DPI-aware** (`<dpiAware>true</dpiAware>` in manifest — deliberately NOT PerMonitorV2) |
| WebView2 SDK | 1.0.4314.35 (also confirmed on 1.0.3650.58) |
| OS | Windows 11 |

## Build

```
dotnet build WV2DpiRepro\WV2DpiRepro.csproj
```

Requires .NET SDK 6+ (for SDK-style build of net472 target) and the WebView2 runtime installed.

## Repro procedure

> **Prerequisite:** a physical machine (not RDP) where you can change Display Scale in
> Settings without signing out.

1. Launch `WV2DpiRepro.exe` while the monitor is at **100%** scale.
2. Click **Open WebView2** — a 10×10 clickable grid loads.
3. Click several cells — confirm the correct cell highlights.
4. Go to **Settings › Display › Scale** and change to **150%**. Do NOT sign out or restart.
5. Click **Close WebView2**, then **Open WebView2** again.

**Expected:** the grid fills the container; clicks land on the correct cell.

**Bug A (visual):** the grid content is rendered offset — visually inset in a smaller box
inside the host border.

**Bug B (decisive):** click any cell. A *different* cell highlights. The status bar in the
page reports which cell the browser thinks was clicked; the visually highlighted cell is the
one the user actually clicked. These are not the same.

6. Resize the app window (drag a corner).

**Bug B persists:** the visual offset corrects after resize, but click coordinates remain
wrong.

## Diagnostic log

The app writes `%TEMP%\wv2-dpi-repro.log` on every open/close with:

- `PresentationSource.CompositionTarget.TransformToDevice.M11`
- `GetDpiForMonitor` MDT_EFFECTIVE_DPI and MDT_RAW_DPI
- `wvContainer.RenderSize`

Values observed at 150% (System-aware process started at 100%):

```
PresentationSource M11  = 1.0000
MDT_EFFECTIVE_DPI       = 96 dpi  (1.000×)   ← latched at startup
MDT_RAW_DPI             = [physical panel DPI]
```

## Known workaround

Adding `<dpiAwareness>PerMonitorV2</dpiAwareness>` to `app.manifest` fixes both symptoms.
This is intentionally absent from this repro — it is the fix, not the configuration under test.
