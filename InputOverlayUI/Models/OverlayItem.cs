using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace InputOverlayUI.Models
{
    public class OverlayItem : ObservableObject
    {
        private string _name = "";
        private bool _isVisible;
        private bool _noBorders;
        private bool _topMost;
        private string _configPath = "";
        private string _imagePath = "";
        private double _windowLeft = 100;
        private double _windowTop = 100;
        private double _windowWidth = 705;
        private double _windowHeight = 394;

        [DisplayName("Name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [DisplayName("Visible")]
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        [DisplayName("No Borders")]
        public bool NoBorders
        {
            get => _noBorders;
            set => SetProperty(ref _noBorders, value);
        }

        [DisplayName("Top Most")]
        public bool TopMost
        {
            get => _topMost;
            set => SetProperty(ref _topMost, value);
        }

        public string ConfigPath
        {
            get => _configPath;
            set => SetProperty(ref _configPath, value);
        }

        public string ImagePath
        {
            get => _imagePath;
            set => SetProperty(ref _imagePath, value);
        }

        public double WindowLeft
        {
            get => _windowLeft;
            set => SetProperty(ref _windowLeft, value);
        }

        public double WindowTop
        {
            get => _windowTop;
            set => SetProperty(ref _windowTop, value);
        }

        public double WindowWidth
        {
            get => _windowWidth;
            set => SetProperty(ref _windowWidth, value);
        }

        public double WindowHeight
        {
            get => _windowHeight;
            set => SetProperty(ref _windowHeight, value);
        }

        public OverlayConfig? Config { get; set; }

        public int Id { get; set; }
    }
}