using Newtonsoft.Json;
using System.Collections.Generic;

namespace InputOverlayUI.Models
{
    public class OverlayConfig
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("texture")]
        public TextureInfo Texture { get; set; } = new TextureInfo();

        [JsonProperty("canvas")]
        public CanvasInfo Canvas { get; set; } = new CanvasInfo();

        [JsonProperty("defaults")]
        public DefaultsInfo Defaults { get; set; } = new DefaultsInfo();

        [JsonProperty("elements")]
        public List<ElementInfo> Elements { get; set; } = new List<ElementInfo>();
    }

    public class TextureInfo
    {
        [JsonProperty("file")]
        public string File { get; set; } = "";

        [JsonProperty("size")]
        public int[] Size { get; set; } = new int[2];
    }

    public class CanvasInfo
    {
        [JsonProperty("size")]
        public int[] Size { get; set; } = new int[2];

        [JsonProperty("background")]
        public int[] Background { get; set; } = new int[] { 0, 0, 0, 0 };
    }

    public class DefaultsInfo
    {
        [JsonProperty("pressed_offset")]
        public int[] PressedOffset { get; set; } = new int[2];
    }

    public class ElementInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = "";

        [JsonProperty("codes")]
        public CodesInfo Codes { get; set; } = new CodesInfo();

        [JsonProperty("pos")]
        public int[] Position { get; set; } = new int[2];

        [JsonProperty("sprite")]
        public SpriteInfo Sprite { get; set; } = new SpriteInfo();

        [JsonProperty("z")]
        public int? Z { get; set; }

        [JsonProperty("wheel")]
        public bool? Wheel { get; set; }

        [JsonProperty("cursor")]
        public CursorInfo? Cursor { get; set; }
    }

    public class CodesInfo
    {
        [JsonProperty("hid")]
        public int? Hid { get; set; }

        [JsonProperty("winvk")]
        public int? WinVk { get; set; }

        [JsonProperty("evdev")]
        public int? Evdev { get; set; }
    }

    public class SpriteInfo
    {
        [JsonProperty("normal")]
        public int[] Normal { get; set; } = new int[4];

        [JsonProperty("pressed")]
        public int[]? Pressed { get; set; }

        [JsonProperty("up")]
        public int[]? Up { get; set; }

        [JsonProperty("down")]
        public int[]? Down { get; set; }

        [JsonProperty("left")]
        public int[]? Left { get; set; }

        [JsonProperty("right")]
        public int[]? Right { get; set; }

        [JsonProperty("up_left")]
        public int[]? UpLeft { get; set; }

        [JsonProperty("up_right")]
        public int[]? UpRight { get; set; }

        [JsonProperty("down_left")]
        public int[]? DownLeft { get; set; }

        [JsonProperty("down_right")]
        public int[]? DownRight { get; set; }
    }

    public class CursorInfo
    {
        [JsonProperty("mode")]
        public string Mode { get; set; } = "";

        [JsonProperty("radius")]
        public int? Radius { get; set; }

        [JsonProperty("sensitivity")]
        public double? Sensitivity { get; set; }

        [JsonProperty("use_monitor_center")]
        public bool? UseMonitorCenter { get; set; }

        [JsonProperty("monitor_center_x")]
        public int? MonitorCenterX { get; set; }

        [JsonProperty("monitor_center_y")]
        public int? MonitorCenterY { get; set; }
    }

    public class MouseEventData
    {
        [JsonProperty("position")]
        public int[] Position { get; set; } = new int[2];

        [JsonProperty("movement")]
        public int[] Movement { get; set; } = new int[2];

        [JsonProperty("wheelDelta")]
        public int WheelDelta { get; set; }

        [JsonProperty("leftButton")]
        public bool LeftButton { get; set; }

        [JsonProperty("rightButton")]
        public bool RightButton { get; set; }

        [JsonProperty("middleButton")]
        public bool MiddleButton { get; set; }

        [JsonProperty("xButton1")]
        public bool XButton1 { get; set; }

        [JsonProperty("xButton2")]
        public bool XButton2 { get; set; }
    }
}