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
    }
}