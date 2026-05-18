<#
.SYNOPSIS
    Convert an OBS input-overlay layout JSON to the Advanced Input Overlay (AIO) format.

.DESCRIPTION
    Reads a layout file in the OBS input-overlay schema (the format used by
    obs-input-overlay-preset/) and writes out a simpler AIO layout that the app
    can consume via Add Overlay -> Overlay config (json).

    Translations performed:
      - int `type` (0/1/3) -> string "texture" / "key" / "mouse"
      - uiohook scan code  -> AIO key name      (17 -> "W", 42 -> "LShift", ...)
      - mouse button number -> AIO key name     (1 -> "MouseLeft", 4 -> "MouseSide2", ...)
      - `mapping: [x, y, w, h]` array -> `src: { x, y, w, h }` object
      - `pos: [x, y]` array           -> `pos: { x, y }` object
      - `overlay_width` / `overlay_height` -> `width` / `height`

    Unsupported element types (wheel / mouse movement / gamepad family) are
    skipped with a warning to stderr; v1 of the AIO app only renders keyboard
    and mouse buttons.

    Output is UTF-8 without BOM.

.PARAMETER InputPath
    Path to the source OBS layout JSON.

.PARAMETER OutputPath
    Path where the AIO layout JSON will be written. Parent directories are
    created if needed.

.EXAMPLE
    pwsh -File tools/Convert-ObsLayout.ps1 -InputPath wasd-minimal.json -OutputPath wasd-aio.json
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = 'Source OBS layout JSON')]
    [string]$InputPath,

    [Parameter(Mandatory = $true, HelpMessage = 'Destination AIO layout JSON')]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

# PowerShell's $PWD and .NET's [Environment]::CurrentDirectory can diverge — `cd`
# only updates the former. Sync them so [System.IO.*] APIs resolve relative paths
# the same way Test-Path / Get-Content do.
[Environment]::CurrentDirectory = $PWD.Path

# uiohook scan code (as it appears in OBS preset `code` fields) -> AIO key name.
# Names match src/Models/KeyMap.cs exactly.
$Keyboard = @{
    1  = 'Escape'
    2  = '1'; 3 = '2'; 4 = '3'; 5 = '4'; 6 = '5'
    7  = '6'; 8 = '7'; 9 = '8'; 10 = '9'; 11 = '0'
    12 = 'Minus'; 13 = 'Equal'; 14 = 'Backspace'; 15 = 'Tab'

    16 = 'Q'; 17 = 'W'; 18 = 'E'; 19 = 'R'; 20 = 'T'
    21 = 'Y'; 22 = 'U'; 23 = 'I'; 24 = 'O'; 25 = 'P'
    26 = 'LBracket'; 27 = 'RBracket'; 28 = 'Enter'; 29 = 'LCtrl'

    30 = 'A'; 31 = 'S'; 32 = 'D'; 33 = 'F'; 34 = 'G'
    35 = 'H'; 36 = 'J'; 37 = 'K'; 38 = 'L'
    39 = 'Semicolon'; 40 = 'Quote'; 41 = 'BackQuote'
    42 = 'LShift'; 43 = 'Backslash'

    44 = 'Z'; 45 = 'X'; 46 = 'C'; 47 = 'V'; 48 = 'B'; 49 = 'N'; 50 = 'M'
    51 = 'Comma'; 52 = 'Period'; 53 = 'Slash'; 54 = 'RShift'
    55 = 'NumMul'; 56 = 'LAlt'; 57 = 'Space'; 58 = 'CapsLock'

    59 = 'F1'; 60 = 'F2'; 61 = 'F3'; 62 = 'F4'; 63 = 'F5'
    64 = 'F6'; 65 = 'F7'; 66 = 'F8'; 67 = 'F9'; 68 = 'F10'

    69 = 'NumLock'; 70 = 'ScrollLock'
    71 = 'Num7'; 72 = 'Num8'; 73 = 'Num9'; 74 = 'NumSub'
    75 = 'Num4'; 76 = 'Num5'; 77 = 'Num6'; 78 = 'NumAdd'
    79 = 'Num1'; 80 = 'Num2'; 81 = 'Num3'; 82 = 'Num0'; 83 = 'NumDot'

    87 = 'F11'; 88 = 'F12'
    91 = 'F13'; 92 = 'F14'; 93 = 'F15'
    99 = 'F16'; 100 = 'F17'; 101 = 'F18'; 102 = 'F19'; 103 = 'F20'
    104 = 'F21'; 105 = 'F22'; 106 = 'F23'; 107 = 'F24'

    # Extended (0xE0__) scan codes for keys that share a primary VK in Windows.
    # AIO does not differentiate numpad-Enter from main Enter; both map to "Enter".
    57372 = 'Enter'      # 0xE01C numpad Enter
    57373 = 'RCtrl'      # 0xE01D
    57397 = 'NumDiv'     # 0xE035
    57400 = 'RAlt'       # 0xE038
    57413 = 'Pause'      # 0xE045
    57415 = 'Home'       # 0xE047
    57416 = 'Up'         # 0xE048
    57417 = 'PageUp'     # 0xE049
    57419 = 'Left'       # 0xE04B
    57421 = 'Right'      # 0xE04D
    57423 = 'End'        # 0xE04F
    57424 = 'Down'       # 0xE050
    57425 = 'PageDown'   # 0xE051
    57426 = 'Insert'     # 0xE052
    57427 = 'Delete'     # 0xE053
    57435 = 'LWin'       # 0xE05B
    57436 = 'RWin'       # 0xE05C
    57437 = 'Apps'       # 0xE05D
}

# OBS preset mouse button numbering (matches obs-input-overlay-preset/mouse/* files).
$Mouse = @{
    1 = 'MouseLeft'
    2 = 'MouseRight'
    3 = 'MouseMiddle'
    4 = 'MouseSide2'     # XBUTTON2 (typically "forward")
    5 = 'MouseSide1'     # XBUTTON1 (typically "back")
}

function Has-Prop {
    param($Obj, [string]$Name)
    return ($null -ne $Obj -and ($Obj.PSObject.Properties.Name -contains $Name))
}

function Warn-Stderr {
    param([string]$Message)
    [Console]::Error.WriteLine($Message)
}

# --- Load input -----------------------------------------------------------

if (-not (Test-Path -LiteralPath $InputPath)) {
    throw "Input file not found: $InputPath"
}

$obs = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json

# --- Build output ---------------------------------------------------------

$aio = [ordered]@{
    width    = 0
    height   = 0
    elements = [System.Collections.ArrayList]@()
}

if (Has-Prop $obs 'overlay_width')  { $aio.width  = [int]$obs.overlay_width }
if (Has-Prop $obs 'overlay_height') { $aio.height = [int]$obs.overlay_height }

if (-not (Has-Prop $obs 'elements') -or $obs.elements.Count -eq 0) {
    Warn-Stderr "WARNING: input has no 'elements' to convert."
}

$converted = 0
$skipped = 0

foreach ($el in $obs.elements) {
    $obsType = [int]$el.type
    $id = if (Has-Prop $el 'id') { $el.id } else { '<unnamed>' }

    $aioType = $null
    $aioKey = $null

    switch ($obsType) {
        0 { $aioType = 'texture' }
        1 {
            $aioType = 'key'
            $code = [int]$el.code
            if ($Keyboard.ContainsKey($code)) {
                $aioKey = $Keyboard[$code]
            }
            else {
                Warn-Stderr ("[skip] '{0}': unknown keyboard scan code {1} (0x{2:X4})" -f $id, $code, $code)
                $skipped++
                continue
            }
        }
        3 {
            $aioType = 'mouse'
            $code = [int]$el.code
            if ($Mouse.ContainsKey($code)) {
                $aioKey = $Mouse[$code]
            }
            else {
                Warn-Stderr ("[skip] '{0}': unknown mouse button code {1}" -f $id, $code)
                $skipped++
                continue
            }
        }
        default {
            $kind = switch ($obsType) {
                4 { 'mouse wheel' }
                5 { 'analog stick' }
                6 { 'trigger' }
                7 { 'gamepad id' }
                8 { 'dpad' }
                9 { 'mouse movement' }
                default { "unknown type $obsType" }
            }
            Warn-Stderr ("[skip] '{0}': {1} is not supported by Advanced Input Overlay v1" -f $id, $kind)
            $skipped++
            continue
        }
    }

    if ($null -eq $el.mapping -or $el.mapping.Count -lt 4) {
        Warn-Stderr ("[skip] '{0}': missing or malformed 'mapping' (need 4 ints)" -f $id)
        $skipped++
        continue
    }
    if ($null -eq $el.pos -or $el.pos.Count -lt 2) {
        Warn-Stderr ("[skip] '{0}': missing or malformed 'pos' (need 2 ints)" -f $id)
        $skipped++
        continue
    }

    $entry = [ordered]@{ type = $aioType }
    if ($aioKey) { $entry.key = $aioKey }
    $entry.src = [ordered]@{
        x = [int]$el.mapping[0]
        y = [int]$el.mapping[1]
        w = [int]$el.mapping[2]
        h = [int]$el.mapping[3]
    }
    $entry.pos = [ordered]@{
        x = [int]$el.pos[0]
        y = [int]$el.pos[1]
    }

    [void]$aio.elements.Add($entry)
    $converted++
}

# --- Write output (UTF-8 without BOM) -------------------------------------

$json = $aio | ConvertTo-Json -Depth 10

$outDir = Split-Path -Parent $OutputPath
if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
    [void](New-Item -ItemType Directory -Force -Path $outDir)
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText(
    [System.IO.Path]::GetFullPath($OutputPath),
    $json,
    $utf8NoBom
)

$summary = "Converted $converted element(s)"
if ($skipped -gt 0) { $summary += ", skipped $skipped" }
$summary += " -> $OutputPath"
Write-Host $summary
