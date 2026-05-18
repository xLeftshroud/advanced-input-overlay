# Advanced Input Overlay

Lightweight Windows portable app that visualizes keyboard and mouse input on screen using a sprite-sheet PNG plus a JSON layout file — the same model as the [OBS input-overlay](https://github.com/univrsal/input-overlay) plugin, but standalone (no OBS required).

Built for stream / tutorial / preset-demo use: import any compatible PNG (e.g. from [obs-input-overlay-preset](https://github.com/univrsal/obs-input-overlay-preset)), write a small JSON describing where each key sits and what source-rect to swap on press, and you get a transparent always-on-top overlay that mirrors your input in real time.

<!-- TODO: add screenshot of main window + a couple of overlays -->

---

## Features

- Manage multiple independent overlay windows from a single main panel.
- Per-overlay toggles: **Show/Hide**, **Window Mode** (native title bar + resize), **Always-on-top**, **Click-through**.
- Drag-handle live reorder, with two-layer z-order (topmost layer above normal layer, list order within each).
- Per-pixel alpha transparency. Proportional sprite scaling via WPF `Viewbox` — drag the window border and the artwork scales to fit.
- Tray icon (Show / Exit), single-instance lock, main + overlay window position recall, multi-monitor safe-bounds.
- Global low-level keyboard + mouse hook — works system-wide regardless of which window has focus.
- Self-contained single-file `.exe`, no .NET install required on the target machine.

## Quick start

1. Download `AdvancedInputOverlay.exe` (single file, ~72 MB self-contained).
2. Double-click to run. The main window opens.
3. Click **`+`** in the top-right → fill in the form:
   - **Name** — anything memorable
   - **Overlay image** — path to a sprite-sheet PNG (Browse...)
   - **Overlay config (json)** — path to a layout JSON describing positions + source rects (Browse...)
4. Click **Save** — a borderless transparent overlay window appears. Drag the content area to move it; toggle **W** to add a native frame for resizing.

State (overlay list, every overlay's position / size / toggles, main window position) is persisted to `config.json` next to the `.exe` automatically.

## Sample

A bundled minimal sample at `samples/wasd-minimal.json` pairs with the WASD sprite sheet at [obs-input-overlay-preset/wasd/wasd.png](https://github.com/univrsal/obs-input-overlay-preset/blob/master/wasd/wasd.png). Point the Add Overlay form at:

- **Overlay image** — `C:\path\to\obs-input-overlay-preset\wasd\wasd.png`
- **Overlay config** — `C:\path\to\advanced-input-overlay\samples\wasd-minimal.json`

Press **Q W E LShift A S D LCtrl Space** and watch the keys light up.

## Convert OBS presets

If you already have layouts in the OBS input-overlay schema — from [obs-input-overlay/presets](https://github.com/univrsal/input-overlay/tree/master/presets) or any other source — the bundled converter translates them to AIO's simpler format. Run it from a PowerShell prompt:

```powershell
cd advanced-input-overlay
pwsh -File tools/Convert-ObsLayout.ps1
    -InputPath  "path\to\obs\preset.json"
    -OutputPath "path\to\output\preset.json"
```

What the converter does:

- **Translates** uiohook scan codes (`17`) to AIO key names (`"W"`), and mouse button numbers (`1`-`5`) to `"MouseLeft"` / `"MouseRight"` / `"MouseMiddle"` / `"MouseSide1"` / `"MouseSide2"`.
- **Reshapes** OBS's `mapping: [x, y, w, h]` and `pos: [x, y]` arrays into AIO's `src: {x,y,w,h}` and `pos: {x,y}` objects.
- **Skips** element types not in v1 (mouse wheel, mouse movement dot, gamepad family) and prints a warning per skipped element to stderr.
- **Drops** OBS-only fields (`id`, `z_level`, `default_*`, `flags`, `space_*`, `version`) that AIO does not consume.

Output is UTF-8 (no BOM) and is ready to load via **Add Overlay → Browse... → Overlay config (json)** alongside the original PNG.

## Layout JSON schema

```json
{
  "width": 612,
  "height": 264,
  "elements": [
    {
      "type": "texture",
      "src": { "x": 328, "y": 1, "w": 283, "h": 242 },
      "pos": { "x": 1, "y": 179 }
    },
    {
      "type": "key",
      "key": "W",
      "src":         { "x": 161, "y": 1,   "w": 157, "h": 128 },
      "pressed_src": { "x": 161, "y": 132, "w": 157, "h": 128 },
      "pos": { "x": 274, "y": 0 }
    },
    {
      "type": "mouse",
      "key": "MouseLeft",
      "src": { "x": 1, "y": 1, "w": 139, "h": 174 },
      "pos": { "x": 0, "y": 0 }
    }
  ]
}
```

Field reference:

| Field | Type | Notes |
|-------|------|-------|
| `width` / `height` | int | Initial overlay window size in px. Omit (or `0`) to auto-fit element bounding box. |
| `elements[]` | array | List of renderable sprites. |
| `element.type` | `"texture"` / `"key"` / `"mouse"` | `texture` is always rendered (background or static art). `key` / `mouse` swap to `pressed_src` while the named input is held. |
| `element.key` | string | Required for `type: "key"` / `"mouse"`. See the key-name table below. |
| `element.src` | `{x, y, w, h}` | Source rect on the PNG for the normal (un-pressed) state. |
| `element.pressed_src` | `{x, y, w, h}` | **Optional.** Source rect for the pressed state. If omitted, defaults to `{ x: src.x, y: src.y + src.h + 3, w: src.w, h: src.h }` — matches OBS input-overlay's vertical-strip convention. |
| `element.pos` | `{x, y}` | Top-left position inside the overlay window. |

## Supported key names

The `key` field accepts the following case-insensitive strings:

| Category | Names |
|----------|-------|
| Letters | `A`–`Z` |
| Digits | `0`–`9` |
| Function | `F1`–`F24` |
| Modifiers (left/right distinguished) | `LShift`, `RShift`, `LCtrl`, `RCtrl`, `LAlt`, `RAlt`, `LWin`, `RWin` |
| Navigation | `Up`, `Down`, `Left`, `Right` |
| Editing | `Backspace`, `Tab`, `Enter`, `Escape`, `Space`, `Insert`, `Delete`, `Home`, `End`, `PageUp`, `PageDown`, `PrintScreen`, `Pause`, `Apps` |
| Symbols | `Comma`, `Period`, `Slash`, `Semicolon`, `Quote`, `BackQuote`, `Minus`, `Equal`, `LBracket`, `RBracket`, `Backslash` |
| Numpad | `Num0`–`Num9`, `NumAdd`, `NumSub`, `NumMul`, `NumDiv`, `NumDot` |
| Locks | `CapsLock`, `NumLock`, `ScrollLock` |
| Mouse | `MouseLeft`, `MouseRight`, `MouseMiddle`, `MouseSide1`, `MouseSide2` |

## Main window controls

Each overlay in the list has six round controls plus a drag handle:

| Button | Meaning |
|--------|---------|
| **S** | **S**how / hide — open or close the overlay window. |
| **W** | **W**indow Mode — add native title bar + 4-side resize border + taskbar entry. |
| **T** | Always on **T**op — moves the overlay into the topmost z-layer. |
| **P** | **P**ass-through (click-through) — mouse events pass through the overlay to the window below. **Note**: once enabled, the overlay can no longer receive clicks. Turn it off from this row to interact with it again. |
| ✎ | Edit — re-opens the Add Overlay form pre-filled. |
| 🗑 | Delete — with confirmation (only removes the list entry; your PNG / JSON files are untouched). |
| ☰ | Drag handle — hold and drag up / down to reorder. Reordering immediately re-applies z-order. |

## Window Mode behavior

- **ON** → 28 px title bar + 4 px resize border on all four sides. Drag the title bar to move, drag any edge / corner to resize (8 directions). Taskbar icon appears. Closing the overlay's X button (or right-clicking its taskbar entry → Close) is equivalent to toggling **S** off — the main window row stays in sync.
- **OFF** → fully borderless. Left-click anywhere on the artwork to drag-move the window. Resize is not possible until Window Mode is re-enabled.

When toggling modes, the overlay's *artwork* stays at the exact same screen position — only the outer window grows / shrinks to add or remove the chrome around it.

## Persistence

All state lives in **`config.json`** next to the `.exe`:

```json
{
  "main_window": { "x": 100, "y": 100, "w": 780, "h": 500 },
  "overlays": [
    {
      "id": "5f2a...",
      "name": "WASD",
      "image_path": "C:\\path\\to\\wasd.png",
      "layout_path": "C:\\path\\to\\wasd.json",
      "visible": true, "window_mode": false, "topmost": true, "click_through": false,
      "window": { "x": 200, "y": 200, "w": 612, "h": 394 }
    }
  ]
}
```

Saves are debounced 500 ms after the last change and written atomically. Image / layout paths are stored as absolute paths — moving the source files breaks the link until you Edit the row.

## Build from source

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```pwsh
# Dev build
dotnet build src/AdvancedInputOverlay.csproj

# Release single-file self-contained exe (~72 MB, no runtime needed on target)
dotnet publish src/AdvancedInputOverlay.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

The published `publish/AdvancedInputOverlay.exe` runs on any Win10 / Win11 x64 machine — no install, no .NET dependency.

## Project layout

```
advanced-input-overlay/
├── AdvancedInputOverlay.sln
├── samples/
│   └── wasd-minimal.json                ← example layout pairing with obs-input-overlay-preset/wasd/wasd.png
├── tools/
│   └── Convert-ObsLayout.ps1            ← OBS input-overlay JSON → AIO JSON converter
└── src/
    ├── AdvancedInputOverlay.csproj      ← net8.0-windows, UseWPF, UseWindowsForms, SelfContained, PublishSingleFile
    ├── App.xaml(.cs)                    ← startup, tray, single-instance Mutex, global hook subscription
    ├── Controls/
    │   └── OverlayCanvas.cs             ← sprite-sheet renderer (CroppedBitmap per element)
    ├── Models/
    │   ├── AppState.cs                  ← config.json root
    │   ├── OverlayConfig.cs             ← per-overlay persisted state
    │   ├── LayoutSchema.cs              ← user-supplied layout JSON shape
    │   └── KeyMap.cs                    ← VK ↔ string-name bidirectional map
    ├── Resources/
    │   └── Styles.xaml                  ← circular toggle / button / add-button styles
    ├── Services/
    │   ├── ConfigStore.cs               ← System.Text.Json + debounced atomic save
    │   ├── InputHook.cs                 ← background thread, WH_KEYBOARD_LL + WH_MOUSE_LL
    │   ├── OverlayManager.cs            ← window lifecycle + two-layer z-order pass
    │   ├── ScreenHelper.cs              ← multi-monitor visibility clamp
    │   ├── TrayIcon.cs                  ← WinForms NotifyIcon wrapper
    │   └── WindowStyleHelper.cs         ← Win32 click-through / tool window / topmost
    ├── ViewModels/
    │   ├── MainViewModel.cs             ← list + Add / Edit / Delete / Move commands
    │   ├── OverlayRowViewModel.cs       ← bindable view over OverlayConfig + IsDragging
    │   ├── ObservableObject.cs          ← INotifyPropertyChanged base
    │   └── RelayCommand.cs              ← minimal ICommand
    └── Windows/
        ├── MainWindow.xaml(.cs)         ← overlay list + drag-handle reorder
        ├── AddOverlayWindow.xaml(.cs)   ← Add / Edit modal with Browse + validation
        └── OverlayWindow.xaml(.cs)      ← per-overlay transparent window with toggleable WindowChrome
```

## Tech stack

.NET 8 WPF + WindowsForms (tray `NotifyIcon`) + Win32 P/Invoke (low-level keyboard / mouse hooks, click-through via `WS_EX_TRANSPARENT`, z-order via `SetWindowPos`) + WPF `WindowChrome` (dynamic caption + resize border on top of `AllowsTransparency=true`, the one combination that lets WPF render per-pixel alpha *and* swap chrome at runtime).

## Credits

Inspired by and learning from:

- [**OBS input-overlay**](https://github.com/univrsal/input-overlay) by univrsal — the sprite-sheet + pressed-state-strip pattern that this whole app is a standalone reimagining of. The `pressed_src` default offset of `y + h + 3` comes straight from `CFG_INNER_BORDER` in its source.
- [**Bongo-Cat-Mver**](https://github.com/MMmmmoko/Bongo-Cat-Mver) by MMmmmoko — the multi-window transparent always-on-top pattern, and the DWM blur trick for per-pixel alpha (which we ended up replacing with WPF's native `AllowsTransparency` for WPF rendering reasons, but the architectural inspiration stuck).
- [**BongoCat**](https://github.com/ayangweb/BongoCat) by ayangweb — the cross-platform Tauri rewrite, particularly the Windows topmost-guarding thread idea (held as a backup fallback should we ever observe other apps stealing topmost).

[obs-input-overlay-preset](https://github.com/univrsal/obs-input-overlay-preset) provides the sprite sheets used in samples.

## License

[MIT](LICENSE) © 2026 xLeftshroud

## Roadmap / Out of scope for v1

The current release intentionally focuses on keyboard + mouse buttons. Not yet supported, in roughly decreasing likelihood of being added:

- Mouse wheel scroll indicator
- Mouse cursor position indicator (dot / arrow within a bounded box)
- Gamepad buttons, analog sticks, triggers, D-pad
- OBS input-overlay JSON schema importer (one-shot conversion from existing presets)
- Global hotkey to hide-all / unlock click-through
- Per-overlay opacity slider
- Multi-language UI

The `type` field in the layout schema is forward-compatible — adding `"wheel"`, `"stick"`, etc. in a future version won't break v1 files.
