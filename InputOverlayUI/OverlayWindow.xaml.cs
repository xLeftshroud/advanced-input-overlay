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
        private DispatcherTimer? _inputTimer;
        private Dictionary<int, bool> _keyStates = new Dictionary<int, bool>();
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private OverlayItem _overlayItem;
        private HwndSource? _hwndSource;
        private const int CornerSize = 15; // Size of corner resize areas

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

        // Window message constants for resize handling
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_SIZING = 0x0214;
        private const int WM_SIZE = 0x0005;

        // Hit test result constants
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        // Sizing constants
        private const int WMSZ_LEFT = 1;
        private const int WMSZ_RIGHT = 2;
        private const int WMSZ_TOP = 3;
        private const int WMSZ_TOPLEFT = 4;
        private const int WMSZ_TOPRIGHT = 5;
        private const int WMSZ_BOTTOM = 6;
        private const int WMSZ_BOTTOMLEFT = 7;
        private const int WMSZ_BOTTOMRIGHT = 8;

        public OverlayWindow(OverlayItem overlayItem)
        {
            InitializeComponent();
            _overlayItem = overlayItem;

            // Set topmost property
            Topmost = overlayItem.TopMost;

            LoadOverlay(overlayItem);
            SetupInputDetection();

            // Restore window position
            Left = overlayItem.WindowLeft;
            Top = overlayItem.WindowTop;
            Width = overlayItem.WindowWidth;
            Height = overlayItem.WindowHeight;

            // Setup dragging
            MouseLeftButtonDown += OverlayWindow_MouseLeftButtonDown;
            MouseLeftButtonUp += OverlayWindow_MouseLeftButtonUp;
            MouseMove += OverlayWindow_MouseMove;

            // Subscribe to property changes for real-time updates
            overlayItem.PropertyChanged += OverlayItem_PropertyChanged;

            // Subscribe to window events to save position and size
            LocationChanged += OverlayWindow_LocationChanged;
            SizeChanged += OverlayWindow_SizeChanged;
            SourceInitialized += OverlayWindow_SourceInitialized;
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




        private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only start dragging if not near resize corners
            var position = e.GetPosition(this);
            if (!IsNearResizeCorner(position))
            {
                _isDragging = true;
                _dragStartPoint = position;
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
            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector delta = currentPosition - _dragStartPoint;

                Left += delta.X;
                Top += delta.Y;
            }
        }

        private bool IsNearResizeCorner(Point position)
        {
            var overlayBounds = GetOverlayContentBounds();
            if (overlayBounds == Rect.Empty) return false;

            // Check if cursor is near any corner of the overlay content
            bool nearLeft = position.X >= overlayBounds.Left - CornerSize && position.X <= overlayBounds.Left + CornerSize;
            bool nearRight = position.X >= overlayBounds.Right - CornerSize && position.X <= overlayBounds.Right + CornerSize;
            bool nearTop = position.Y >= overlayBounds.Top - CornerSize && position.Y <= overlayBounds.Top + CornerSize;
            bool nearBottom = position.Y >= overlayBounds.Bottom - CornerSize && position.Y <= overlayBounds.Bottom + CornerSize;

            // Return true if near any corner
            return (nearLeft && nearTop) || (nearRight && nearTop) ||
                   (nearLeft && nearBottom) || (nearRight && nearBottom);
        }

        private void OverlayItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayItem.TopMost))
            {
                // Update topmost property in real-time
                Topmost = _overlayItem.TopMost;
            }
        }





        private void OverlayWindow_LocationChanged(object? sender, EventArgs e)
        {
            // Save current position to the model
            _overlayItem.WindowLeft = Left;
            _overlayItem.WindowTop = Top;
        }

        private void OverlayWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _overlayItem.WindowWidth = ActualWidth;
            _overlayItem.WindowHeight = ActualHeight;
        }

        private void OverlayWindow_SourceInitialized(object? sender, EventArgs e)
        {
            _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            _hwndSource?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    handled = true;
                    return HandleHitTest(lParam);
            }
            return IntPtr.Zero;
        }

        private IntPtr HandleHitTest(IntPtr lParam)
        {
            // Get cursor position relative to screen
            int x = lParam.ToInt32() & 0xFFFF;
            int y = (lParam.ToInt32() >> 16) & 0xFFFF;

            // Convert to client coordinates
            var point = PointFromScreen(new Point(x, y));

            // Get the overlay content boundaries (Viewbox content)
            var overlayBounds = GetOverlayContentBounds();
            if (overlayBounds == Rect.Empty)
            {
                // If we can't get overlay bounds, default to client area
                return new IntPtr(HTCLIENT);
            }

            // Check if cursor is near the corners of the overlay content
            bool nearLeft = point.X >= overlayBounds.Left - CornerSize && point.X <= overlayBounds.Left + CornerSize;
            bool nearRight = point.X >= overlayBounds.Right - CornerSize && point.X <= overlayBounds.Right + CornerSize;
            bool nearTop = point.Y >= overlayBounds.Top - CornerSize && point.Y <= overlayBounds.Top + CornerSize;
            bool nearBottom = point.Y >= overlayBounds.Bottom - CornerSize && point.Y <= overlayBounds.Bottom + CornerSize;

            // Check for corner resize areas
            if (nearLeft && nearTop) return new IntPtr(HTTOPLEFT);
            if (nearRight && nearTop) return new IntPtr(HTTOPRIGHT);
            if (nearLeft && nearBottom) return new IntPtr(HTBOTTOMLEFT);
            if (nearRight && nearBottom) return new IntPtr(HTBOTTOMRIGHT);

            // Default to client area (allows dragging)
            return new IntPtr(HTCLIENT);
        }

        private Rect GetOverlayContentBounds()
        {
            try
            {
                // Get the transform from the Viewbox to get actual content bounds
                var viewbox = OverlayViewbox;
                if (viewbox?.Child == null) return Rect.Empty;

                // Get the render transform of the viewbox content
                var transform = viewbox.Child.TransformToAncestor(this);
                var contentSize = viewbox.Child.RenderSize;

                // Transform the content bounds to window coordinates
                var topLeft = transform.Transform(new Point(0, 0));
                var bottomRight = transform.Transform(new Point(contentSize.Width, contentSize.Height));

                return new Rect(topLeft, bottomRight);
            }
            catch
            {
                // Fallback: use the entire window area
                return new Rect(0, 0, ActualWidth, ActualHeight);
            }
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
            SourceInitialized -= OverlayWindow_SourceInitialized;

            // Remove window message hook
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource = null;

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