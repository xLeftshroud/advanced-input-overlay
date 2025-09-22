using System.Collections.Generic;

namespace InputOverlayUI.Models
{
    public class AppSettings
    {
        public List<OverlayItem> Overlays { get; set; } = new List<OverlayItem>();
        public string LastImageDirectory { get; set; } = "";
        public string LastConfigDirectory { get; set; } = "";
        public int NextOverlayId { get; set; } = 1;
    }
}