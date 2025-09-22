# Input Overlay Presets

This directory contains preset configuration files for the Input Overlay application. These presets provide pre-configured key layouts that users can easily select when adding new overlays.

## Available Presets

### WASD Keys (`wasd.json`)
- Basic WASD movement keys layout
- Includes: W, A, S, D keys
- Suitable for: FPS games, action games

### Arrow Keys (`arrow_keys.json`)
- Directional arrow keys layout
- Includes: Up, Down, Left, Right arrow keys
- Suitable for: Platformers, puzzle games, retro games

### Gaming Full Layout (`gaming_full.json`)
- Comprehensive gaming layout
- Includes: WASD, Space, Shift, Ctrl, Mouse Left/Right clicks
- Suitable for: Complex games requiring multiple inputs

## Configuration Schema

Each preset file follows this JSON schema:

```json
{
  "version": 1,
  "texture": {
    "file": "image_file.png",
    "size": [width, height]
  },
  "canvas": {
    "size": [width, height],
    "background": [r, g, b, a]
  },
  "defaults": {
    "pressed_offset": [x, y]
  },
  "elements": [
    {
      "id": "key_name",
      "codes": {
        "hid": 123,
        "winvk": 456
      },
      "pos": [x, y],
      "sprite": {
        "normal": [sx, sy, w, h],
        "pressed": [sx, sy, w, h]  // optional if using defaults.pressed_offset
      },
      "z": 1  // optional draw order
    }
  ]
}
```

## Key Field Descriptions

- **id**: Descriptive name for the key
- **codes**: Input codes that trigger the key
  - **hid**: HID usage code
  - **winvk**: Windows Virtual Key code
  - **evdev**: Linux evdev code (optional)
- **pos**: Position on overlay canvas [x, y]
- **sprite.normal**: Sprite rectangle for normal state [x, y, width, height]
- **sprite.pressed**: Sprite rectangle for pressed state (optional if using defaults)
- **z**: Draw order (higher values drawn on top)

## Adding Custom Presets

1. Create a new JSON file in this directory
2. Follow the schema above
3. The file will automatically appear in the preset dropdown
4. Ensure corresponding image files are available

## HID/VK Code Reference

Common key codes:
- A-Z: HID 4-29, VK 65-90
- 0-9: HID 30-39, VK 48-57
- Space: HID 44, VK 32
- Enter: HID 40, VK 13
- Shift: HID 225, VK 16
- Ctrl: HID 224, VK 17
- Arrow keys: HID 79-82, VK 37-40