using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using InputOverlayUI.Models;
using Newtonsoft.Json;

namespace InputOverlayUI
{
    public partial class OverlayWindow : Window
    {
        private OverlayConfig? _config;
        private BitmapImage? _overlayImage;
        private List<KeyElement> _keyElements = new List<KeyElement>();
        private DispatcherTimer _inputTimer;
        private Dictionary<int, bool> _keyStates = new Dictionary<int, bool>();
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private OverlayItem _overlayItem;

        // Windows API for key state detection
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_LAYERED = 0x80000;
        private const uint WS_EX_TRANSPARENT = 0x20;

        public OverlayWindow(OverlayItem overlayItem)
        {
            InitializeComponent();
            _overlayItem = overlayItem;

            // Set window properties based on no border mode BEFORE loading
            if (overlayItem.NoBorders)
            {
                WindowStyle = WindowStyle.None;
                AllowsTransparency = true;
                Background = Brushes.Transparent;
                ShowInTaskbar = false;
                ResizeMode = ResizeMode.NoResize;
            }

            // Set topmost property
            Topmost = overlayItem.TopMost;

            LoadOverlay(overlayItem);
            SetupInputDetection();

            // Restore window position
            Left = overlayItem.WindowLeft;
            Top = overlayItem.WindowTop;

            // Set size - always use the saved window size
            Width = overlayItem.WindowWidth;
            Height = overlayItem.WindowHeight;

            // Initialize the no border mode based on the overlay item
            NoBorderMenuItem.IsChecked = overlayItem.NoBorders;

            // Setup dragging if in no border mode
            if (overlayItem.NoBorders)
            {
                MouseLeftButtonDown += OverlayWindow_MouseLeftButtonDown;
                MouseLeftButtonUp += OverlayWindow_MouseLeftButtonUp;
                MouseMove += OverlayWindow_MouseMove;
            }

            // Subscribe to property changes for real-time updates
            overlayItem.PropertyChanged += OverlayItem_PropertyChanged;

            // Subscribe to window events to save position and size
            LocationChanged += OverlayWindow_LocationChanged;
            SizeChanged += OverlayWindow_SizeChanged;
        }

        private void LoadOverlay(OverlayItem overlayItem)
        {
            try
            {
                // Load configuration
                if (!File.Exists(overlayItem.ConfigPath))
                {
                    MessageBox.Show($"Configuration file not found: {overlayItem.ConfigPath}");
                    return;
                }

                string configJson = File.ReadAllText(overlayItem.ConfigPath);
                _config = JsonConvert.DeserializeObject<OverlayConfig>(configJson);

                if (_config == null)
                {
                    MessageBox.Show("Failed to parse configuration file");
                    return;
                }

                // Load image
                if (!File.Exists(overlayItem.ImagePath))
                {
                    MessageBox.Show($"Image file not found: {overlayItem.ImagePath}");
                    return;
                }

                _overlayImage = new BitmapImage(new Uri(overlayItem.ImagePath, UriKind.Absolute));

                // Set canvas size from config (the Viewbox will scale this to fit the window)
                OverlayCanvas.Width = _config.Canvas.Size[0];
                OverlayCanvas.Height = _config.Canvas.Size[1];

                // Set background if specified
                if (_config.Canvas.Background != null && _config.Canvas.Background.Length >= 4)
                {
                    var bgColor = Color.FromArgb(
                        (byte)_config.Canvas.Background[3], // Alpha
                        (byte)_config.Canvas.Background[0], // Red
                        (byte)_config.Canvas.Background[1], // Green
                        (byte)_config.Canvas.Background[2]  // Blue
                    );
                    OverlayBorder.Background = new SolidColorBrush(bgColor);
                }

                CreateKeyElements();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading overlay: {ex.Message}");
            }
        }

        private void CreateKeyElements()
        {
            if (_config?.Elements == null || _overlayImage == null) return;

            _keyElements.Clear();
            OverlayCanvas.Children.Clear();

            foreach (var element in _config.Elements)
            {
                var keyElement = new KeyElement(element, _overlayImage, _config.Defaults);
                _keyElements.Add(keyElement);

                var image = keyElement.CreateImageControl();
                Canvas.SetLeft(image, element.Position[0]);
                Canvas.SetTop(image, element.Position[1]);
                Canvas.SetZIndex(image, element.Z ?? 1);

                OverlayCanvas.Children.Add(image);

                // Track key state
                if (element.Codes.WinVk.HasValue)
                {
                    _keyStates[element.Codes.WinVk.Value] = false;
                }
            }
        }

        private void SetupInputDetection()
        {
            _inputTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _inputTimer.Tick += CheckKeyStates;
            _inputTimer.Start();
        }

        private void CheckKeyStates(object? sender, EventArgs e)
        {
            if (_keyElements.Count == 0) return;

            foreach (var keyElement in _keyElements)
            {
                if (keyElement.Element.Codes.WinVk.HasValue)
                {
                    int vk = keyElement.Element.Codes.WinVk.Value;
                    bool isPressed = (GetAsyncKeyState(vk) & 0x8000) != 0;

                    if (_keyStates.ContainsKey(vk) && _keyStates[vk] != isPressed)
                    {
                        _keyStates[vk] = isPressed;
                        keyElement.SetPressed(isPressed);
                    }
                }
            }
        }


        private void NoBorderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _overlayItem.NoBorders = NoBorderMenuItem.IsChecked;
            // ToggleNoBorderMode() will be called automatically via PropertyChanged event
        }


        private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_overlayItem.NoBorders)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                CaptureMouse();
            }
        }

        private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
            }
        }

        private void OverlayWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _overlayItem.NoBorders)
            {
                Point currentPosition = e.GetPosition(this);
                Vector delta = currentPosition - _dragStartPoint;

                Left += delta.X;
                Top += delta.Y;
            }
        }

        private void OverlayItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayItem.NoBorders))
            {
                // Update checkbox to match the model (avoid infinite loop by checking if it's different)
                if (NoBorderMenuItem.IsChecked != _overlayItem.NoBorders)
                {
                    NoBorderMenuItem.IsChecked = _overlayItem.NoBorders;
                }

                // For real-time switching, we need to recreate the window
                // Save current state (always save current size regardless of mode)
                _overlayItem.WindowLeft = Left;
                _overlayItem.WindowTop = Top;
                _overlayItem.WindowWidth = ActualWidth;
                _overlayItem.WindowHeight = ActualHeight;

                // Signal that we need to recreate the window
                RecreateWindow?.Invoke();
            }
            else if (e.PropertyName == nameof(OverlayItem.TopMost))
            {
                // Update topmost property in real-time
                Topmost = _overlayItem.TopMost;
            }
        }

        public event Action? RecreateWindow;

        private void OverlayWindow_LocationChanged(object? sender, EventArgs e)
        {
            // Save current position to the model
            _overlayItem.WindowLeft = Left;
            _overlayItem.WindowTop = Top;
        }

        private void OverlayWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Save current size to the model
            _overlayItem.WindowWidth = ActualWidth;
            _overlayItem.WindowHeight = ActualHeight;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from events
            if (_overlayItem != null)
            {
                _overlayItem.PropertyChanged -= OverlayItem_PropertyChanged;
            }
            LocationChanged -= OverlayWindow_LocationChanged;
            SizeChanged -= OverlayWindow_SizeChanged;

            _inputTimer?.Stop();
            base.OnClosed(e);
        }
    }

    internal class KeyElement
    {
        public ElementInfo Element { get; }
        private BitmapImage _sourceImage;
        private DefaultsInfo _defaults;
        private Image? _imageControl;

        public KeyElement(ElementInfo element, BitmapImage sourceImage, DefaultsInfo defaults)
        {
            Element = element;
            _sourceImage = sourceImage;
            _defaults = defaults;
        }

        public Image CreateImageControl()
        {
            _imageControl = new Image();
            SetPressed(false); // Start with normal state
            return _imageControl;
        }

        public void SetPressed(bool isPressed)
        {
            if (_imageControl == null) return;

            CroppedBitmap croppedBitmap;

            if (isPressed && Element.Sprite.Pressed != null)
            {
                // Use explicit pressed sprite
                croppedBitmap = new CroppedBitmap(_sourceImage, new Int32Rect(
                    Element.Sprite.Pressed[0], Element.Sprite.Pressed[1],
                    Element.Sprite.Pressed[2], Element.Sprite.Pressed[3]));
            }
            else if (isPressed && _defaults.PressedOffset != null)
            {
                // Use normal sprite with pressed offset
                croppedBitmap = new CroppedBitmap(_sourceImage, new Int32Rect(
                    Element.Sprite.Normal[0],
                    Element.Sprite.Normal[1] + _defaults.PressedOffset[1],
                    Element.Sprite.Normal[2], Element.Sprite.Normal[3]));
            }
            else
            {
                // Use normal sprite
                croppedBitmap = new CroppedBitmap(_sourceImage, new Int32Rect(
                    Element.Sprite.Normal[0], Element.Sprite.Normal[1],
                    Element.Sprite.Normal[2], Element.Sprite.Normal[3]));
            }

            _imageControl.Source = croppedBitmap;
            _imageControl.Width = Element.Sprite.Normal[2];
            _imageControl.Height = Element.Sprite.Normal[3];
        }
    }
}