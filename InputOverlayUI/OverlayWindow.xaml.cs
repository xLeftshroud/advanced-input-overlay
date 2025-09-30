using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private Dictionary<int, bool> _mouseButtonStates = new Dictionary<int, bool>();
        private bool _isDragging = false;
        private Point _dragStartPoint;
        private OverlayItem _overlayItem;
        private HwndSource? _hwndSource;
        private const int CornerSize = 15; // Size of corner resize areas
        private List<CursorElement> _cursorElements = new List<CursorElement>();
        private Point _lastMousePosition = new Point();
        private bool _hasMouseElements = false;
        private bool _isMouseOverlay = false;
        private Dictionary<string, int> _wheelStates = new Dictionary<string, int>(); // Track wheel scroll states
        private Dictionary<string, DateTime> _wheelScrollTimes = new Dictionary<string, DateTime>();
        private DateTime _lastScrollTime = DateTime.MinValue;
        private int _lastScrollDelta = 0;

        // Windows API for key state detection
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

        [StructLayout(LayoutKind.Sequential)]
        public struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        private HookProc? _mouseHookProc;
        private IntPtr _mouseHook = IntPtr.Zero;
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL_HOOK = 0x020A;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // Virtual key codes for mouse buttons
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04;
        private const int VK_XBUTTON1 = 0x05;
        private const int VK_XBUTTON2 = 0x06;

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong32(IntPtr hWnd, int nIndex);

        private static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, (uint)dwNewLong);
        }

        private static IntPtr GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private const int GWL_EXSTYLE = -20;
        private const uint WS_EX_LAYERED = 0x80000;
        private const uint WS_EX_TRANSPARENT = 0x20;

        // Window message constants for resize handling
        private const int WM_NCHITTEST = 0x0084;
        private const int WM_SIZING = 0x0214;
        private const int WM_SIZE = 0x0005;
        private const int WM_MOUSEWHEEL = 0x020A;

        // Hit test result constants
        private const int HTTRANSPARENT = -1;
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

            // Apply initial window penetration setting
            UpdateWindowPenetration();

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

                // Add mouse wheel event handling for mouse overlays
                if (_isMouseOverlay)
                {
                    // Enable all possible wheel event capture methods
                    MouseWheel += OverlayWindow_MouseWheel;
                    PreviewMouseWheel += OverlayWindow_PreviewMouseWheel;

                    // Setup global mouse hook for system-wide wheel detection
                    SetupMouseHook();

                    // Make sure window can receive wheel events
                    Background = Brushes.Transparent;
                    AllowsTransparency = true;

                    System.Diagnostics.Debug.WriteLine("Mouse overlay wheel detection enabled with global hook");
                }
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
            _cursorElements.Clear();
            OverlayCanvas.Children.Clear();
            _hasMouseElements = false;
            _isMouseOverlay = false;

            // First pass: detect if this is a mouse overlay
            foreach (var element in _config.Elements)
            {
                if (IsMouseElement(element))
                {
                    _isMouseOverlay = true;
                    _hasMouseElements = true;
                    System.Diagnostics.Debug.WriteLine($"Detected mouse overlay due to element: {element.Id}");
                    break;
                }
            }

            System.Diagnostics.Debug.WriteLine($"Is mouse overlay: {_isMouseOverlay}");

            foreach (var element in _config.Elements)
            {
                // Check if this is a cursor element
                if (element.Cursor != null && !string.IsNullOrEmpty(element.Cursor.Mode))
                {
                    var cursorElement = new CursorElement(element, _overlayImage, _config.Defaults);
                    _cursorElements.Add(cursorElement);

                    var image = cursorElement.CreateImageControl();
                    Canvas.SetLeft(image, element.Position[0]);
                    Canvas.SetTop(image, element.Position[1]);
                    Canvas.SetZIndex(image, element.Z ?? 1);

                    OverlayCanvas.Children.Add(image);
                    _hasMouseElements = true;
                }
                else
                {
                    var keyElement = new KeyElement(element, _overlayImage, _config.Defaults);
                    _keyElements.Add(keyElement);

                    var image = keyElement.CreateImageControl();
                    Canvas.SetLeft(image, element.Position[0]);
                    Canvas.SetTop(image, element.Position[1]);
                    Canvas.SetZIndex(image, element.Z ?? 1);

                    OverlayCanvas.Children.Add(image);

                    // Track key or mouse button state
                    if (element.Wheel == true || element.Id.ToLower().Contains("wheel") ||
                        element.Sprite.Up != null || element.Sprite.Down != null)
                    {
                        // For wheel elements, we track middle mouse button for press detection
                        _mouseButtonStates[VK_MBUTTON] = false;
                        _wheelStates[element.Id] = 0; // Initialize wheel state
                        _hasMouseElements = true;
                        System.Diagnostics.Debug.WriteLine($"Found wheel element: {element.Id}");
                    }
                    else if (element.Codes.WinVk.HasValue)
                    {
                        var vk = element.Codes.WinVk.Value;
                        if (IsMouseButton(vk))
                        {
                            _mouseButtonStates[vk] = false;
                            _hasMouseElements = true;
                        }
                        else
                        {
                            _keyStates[vk] = false;
                        }
                    }
                    else if (element.Codes.Hid.HasValue)
                    {
                        // Map HID codes to VK codes for mouse buttons
                        var hidCode = element.Codes.Hid.Value;
                        var vk = HidToVirtualKey(hidCode);
                        if (vk != 0)
                        {
                            if (IsMouseButton(vk))
                            {
                                _mouseButtonStates[vk] = false;
                                _hasMouseElements = true;
                            }
                            else
                            {
                                _keyStates[vk] = false;
                            }
                        }
                    }
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
            // Check keyboard elements
            foreach (var keyElement in _keyElements)
            {
                if (keyElement.Element.Wheel == true || keyElement.Element.Id.ToLower().Contains("wheel") ||
                    keyElement.Element.Sprite.Up != null || keyElement.Element.Sprite.Down != null)
                {
                    // Handle wheel element specifically
                    CheckWheelElement(keyElement);
                }
                else
                {
                    // Handle regular key/button elements
                    bool isPressed = false;
                    int trackingKey = 0;

                    if (keyElement.Element.Codes.WinVk.HasValue)
                    {
                        trackingKey = keyElement.Element.Codes.WinVk.Value;
                        isPressed = (GetAsyncKeyState(trackingKey) & 0x8000) != 0;
                    }
                    else if (keyElement.Element.Codes.Hid.HasValue)
                    {
                        trackingKey = HidToVirtualKey(keyElement.Element.Codes.Hid.Value);
                        if (trackingKey != 0)
                        {
                            isPressed = (GetAsyncKeyState(trackingKey) & 0x8000) != 0;
                        }
                    }

                    if (trackingKey != 0)
                    {
                        var stateDict = IsMouseButton(trackingKey) ? _mouseButtonStates : _keyStates;
                        if (stateDict.ContainsKey(trackingKey) && stateDict[trackingKey] != isPressed)
                        {
                            stateDict[trackingKey] = isPressed;
                            keyElement.SetPressed(isPressed);
                        }
                    }
                }
            }

            // Check cursor elements and update positions
            if (_hasMouseElements && _cursorElements.Count > 0)
            {
                GetCursorPos(out POINT cursorPos);
                var currentMousePos = new Point(cursorPos.X, cursorPos.Y);

                if (currentMousePos != _lastMousePosition)
                {
                    UpdateCursorElements(currentMousePos);
                    _lastMousePosition = currentMousePos;
                }
            }

            // Update wheel element states (reset scroll states after delay)
            if (_isMouseOverlay)
            {
                UpdateWheelStates();
            }
        }

        private void OverlayWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MouseWheel event: delta={e.Delta}, isMouseOverlay={_isMouseOverlay}");
            if (!_isMouseOverlay) return;
            HandleMouseWheelMessage(e.Delta);
            e.Handled = true;
        }

        private void OverlayWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"PreviewMouseWheel event: delta={e.Delta}, isMouseOverlay={_isMouseOverlay}");
            if (!_isMouseOverlay) return;
            HandleMouseWheelMessage(e.Delta);
            // Don't set e.Handled = true here to allow the event to bubble up
        }

        private void HandleMouseWheelMessage(int delta)
        {
            if (!_isMouseOverlay) return;

            var currentTime = DateTime.Now;

            // Prevent duplicate events within 10ms only (更快响应，减少延迟)
            if ((currentTime - _lastScrollTime).TotalMilliseconds < 10 && delta == _lastScrollDelta)
            {
                System.Diagnostics.Debug.WriteLine($"Ignoring duplicate wheel event: delta={delta}");
                return;
            }

            _lastScrollTime = currentTime;
            _lastScrollDelta = delta;

            System.Diagnostics.Debug.WriteLine($"Processing mouse wheel: delta={delta}, direction={(delta > 0 ? "UP" : "DOWN")}");

            // Update wheel elements based on scroll direction
            foreach (var keyElement in _keyElements)
            {
                if (keyElement.Element.Wheel == true || keyElement.Element.Id.ToLower().Contains("wheel") ||
                    keyElement.Element.Sprite.Up != null || keyElement.Element.Sprite.Down != null)
                {
                    var wheelState = delta > 0 ? 2 : 3; // 2 = up, 3 = down

                    // Force set the wheel state
                    keyElement.SetWheelState(wheelState);

                    // Track the scroll time to reset state later
                    _wheelStates[keyElement.Element.Id] = wheelState;
                    _wheelScrollTimes[keyElement.Element.Id] = currentTime;

                    System.Diagnostics.Debug.WriteLine($"✓ Applied wheel state {(wheelState == 2 ? "UP" : "DOWN")} to element '{keyElement.Element.Id}'");
                }
            }
        }

        private void CheckWheelElement(KeyElement wheelElement)
        {
            // Check if middle mouse button (wheel) is pressed
            bool isWheelPressed = (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0;

            var elementId = wheelElement.Element.Id;
            var currentState = _wheelStates.ContainsKey(elementId) ? _wheelStates[elementId] : 0;

            // Debug output
            if (isWheelPressed)
            {
                System.Diagnostics.Debug.WriteLine($"Wheel pressed detected for {elementId}");
            }

            // Check for wheel button press first
            if (isWheelPressed && currentState != 1)
            {
                _wheelStates[elementId] = 1; // Pressed state
                wheelElement.SetWheelState(1);
                _wheelScrollTimes[elementId] = DateTime.Now;
                System.Diagnostics.Debug.WriteLine($"Set wheel state to PRESSED for {elementId}");
            }
            else if (!isWheelPressed && currentState == 1)
            {
                // Released from pressed state, go back to normal
                _wheelStates[elementId] = 0;
                wheelElement.SetWheelState(0);
                if (_wheelScrollTimes.ContainsKey(elementId))
                    _wheelScrollTimes.Remove(elementId);
                System.Diagnostics.Debug.WriteLine($"Set wheel state to NORMAL for {elementId}");
            }
        }

        private void UpdateWheelStates()
        {
            var currentTime = DateTime.Now;
            var keysToReset = new List<string>();

            foreach (var kvp in _wheelScrollTimes)
            {
                if ((currentTime - kvp.Value).TotalMilliseconds > 150) // 减少到150ms，快速响应
                {
                    keysToReset.Add(kvp.Key);
                }
            }

            foreach (var key in keysToReset)
            {
                // Only reset scroll states (2=up, 3=down), not pressed state (1)
                var wheelElement = _keyElements.FirstOrDefault(e => e.Element.Id == key);
                if (wheelElement != null && _wheelStates.ContainsKey(key))
                {
                    var currentState = _wheelStates[key];
                    bool isWheelPressed = (GetAsyncKeyState(VK_MBUTTON) & 0x8000) != 0;

                    // Only reset scroll states (up/down), not press state
                    if (currentState >= 2 && !isWheelPressed) // 2=up, 3=down
                    {
                        _wheelStates[key] = 0; // Reset to normal state
                        _wheelScrollTimes.Remove(key);
                        wheelElement.SetWheelState(0);
                        System.Diagnostics.Debug.WriteLine($"Reset wheel state to NORMAL for {key}");
                    }
                    else if (currentState == 1 && !isWheelPressed) // 1=pressed
                    {
                        _wheelStates[key] = 0;
                        _wheelScrollTimes.Remove(key);
                        wheelElement.SetWheelState(0);
                        System.Diagnostics.Debug.WriteLine($"Reset wheel pressed state to NORMAL for {key}");
                    }
                }
            }
        }




        private void OverlayWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't handle mouse events if window penetration is enabled
            if (_overlayItem.WindowPenetration)
            {
                return;
            }

            // Only start dragging if not near resize areas
            var position = e.GetPosition(this);
            if (!IsNearResizeArea(position))
            {
                _isDragging = true;
                _dragStartPoint = position;
                CaptureMouse();
            }
        }

        private void OverlayWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Don't handle mouse events if window penetration is enabled
            if (_overlayItem.WindowPenetration)
            {
                return;
            }

            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
            }
        }

        private void OverlayWindow_MouseMove(object sender, MouseEventArgs e)
        {
            // Don't handle mouse events if window penetration is enabled
            if (_overlayItem.WindowPenetration)
            {
                return;
            }

            if (_isDragging)
            {
                Point currentPosition = e.GetPosition(this);
                Vector delta = currentPosition - _dragStartPoint;

                Left += delta.X;
                Top += delta.Y;
            }
        }

        private bool IsNearResizeArea(Point position)
        {
            var overlayBounds = GetOverlayContentBounds();
            if (overlayBounds == Rect.Empty) return false;

            // Check if cursor is near any resize area (corners or edges) of the overlay content
            bool nearLeft = position.X >= overlayBounds.Left - CornerSize && position.X <= overlayBounds.Left + CornerSize;
            bool nearRight = position.X >= overlayBounds.Right - CornerSize && position.X <= overlayBounds.Right + CornerSize;
            bool nearTop = position.Y >= overlayBounds.Top - CornerSize && position.Y <= overlayBounds.Top + CornerSize;
            bool nearBottom = position.Y >= overlayBounds.Bottom - CornerSize && position.Y <= overlayBounds.Bottom + CornerSize;

            // Return true if near any corner or edge
            return (nearLeft && nearTop) || (nearRight && nearTop) ||
                   (nearLeft && nearBottom) || (nearRight && nearBottom) ||
                   nearLeft || nearRight || nearTop || nearBottom;
        }

        private void OverlayItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OverlayItem.TopMost))
            {
                // Update topmost property in real-time
                Topmost = _overlayItem.TopMost;
            }
            else if (e.PropertyName == nameof(OverlayItem.WindowPenetration))
            {
                // Update window penetration in real-time
                UpdateWindowPenetration();
            }
        }

        private void UpdateWindowPenetration()
        {
            if (_hwndSource?.Handle != IntPtr.Zero && _hwndSource != null)
            {
                IntPtr hwnd = _hwndSource.Handle;

                if (_overlayItem.WindowPenetration)
                {
                    // Enable window penetration for any overlay type
                    IntPtr extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt64() | WS_EX_TRANSPARENT));
                    IsHitTestVisible = false;
                    System.Diagnostics.Debug.WriteLine($"Overlay '{_overlayItem.Name}': click-through enabled");
                }
                else
                {
                    // Disable window penetration - normal interactive mode
                    IntPtr extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(extendedStyle.ToInt64() & ~WS_EX_TRANSPARENT));
                    IsHitTestVisible = true;
                    System.Diagnostics.Debug.WriteLine($"Overlay '{_overlayItem.Name}': click-through disabled");
                }
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

            // Apply window penetration setting now that we have a window handle
            UpdateWindowPenetration();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    handled = true;
                    var result = HandleHitTest(lParam);
                    System.Diagnostics.Debug.WriteLine($"WM_NCHITTEST: WindowPenetration={_overlayItem.WindowPenetration}, Result={result.ToInt32()}");
                    return result;

                case WM_MOUSEWHEEL:
                    if (_isMouseOverlay && !_overlayItem.WindowPenetration)
                    {
                        short delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                        HandleMouseWheelMessage(delta);
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private IntPtr HandleHitTest(IntPtr lParam)
        {
            // Check if window penetration is enabled - make window completely transparent to mouse events
            if (_overlayItem.WindowPenetration)
            {
                System.Diagnostics.Debug.WriteLine($"Window penetration enabled - returning HTTRANSPARENT for overlay: {_overlayItem.Name}");
                return new IntPtr(HTTRANSPARENT);
            }

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

            // Check for corner resize areas first (priority over edges)
            if (nearLeft && nearTop) return new IntPtr(HTTOPLEFT);
            if (nearRight && nearTop) return new IntPtr(HTTOPRIGHT);
            if (nearLeft && nearBottom) return new IntPtr(HTBOTTOMLEFT);
            if (nearRight && nearBottom) return new IntPtr(HTBOTTOMRIGHT);

            // Check for edge resize areas
            if (nearLeft) return new IntPtr(HTLEFT);
            if (nearRight) return new IntPtr(HTRIGHT);
            if (nearTop) return new IntPtr(HTTOP);
            if (nearBottom) return new IntPtr(HTBOTTOM);

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

        private bool IsMouseButton(int vk)
        {
            return vk == VK_LBUTTON || vk == VK_RBUTTON || vk == VK_MBUTTON || vk == VK_XBUTTON1 || vk == VK_XBUTTON2;
        }

        private bool IsMouseElement(ElementInfo element)
        {
            // Check if element has mouse button codes
            if (element.Codes.Hid.HasValue)
            {
                var hid = element.Codes.Hid.Value;
                if (hid >= 1 && hid <= 5) // HID codes 1-5 are mouse buttons
                    return true;
            }

            if (element.Codes.WinVk.HasValue)
            {
                var vk = element.Codes.WinVk.Value;
                if (IsMouseButton(vk))
                    return true;
            }

            // Check if element is a wheel
            if (element.Wheel == true)
                return true;

            // Check if element has cursor info
            if (element.Cursor != null && !string.IsNullOrEmpty(element.Cursor.Mode))
                return true;

            // Check if element ID suggests it's mouse-related
            var id = element.Id.ToLower();
            if (id.Contains("mouse") || id.Contains("lmb") || id.Contains("rmb") ||
                id.Contains("wheel") || id.Contains("cursor") || id.Contains("xbutton"))
                return true;

            // Check if element has wheel-specific sprites (up/down states)
            if (element.Sprite.Up != null || element.Sprite.Down != null)
                return true;

            return false;
        }

        private int HidToVirtualKey(int hidCode)
        {
            return hidCode switch
            {
                1 => VK_LBUTTON,    // Left mouse button
                2 => VK_RBUTTON,    // Right mouse button
                3 => VK_MBUTTON,    // Middle mouse button
                4 => VK_XBUTTON2,   // X Button 2
                5 => VK_XBUTTON1,   // X Button 1
                6 => 0x43,          // C key
                7 => 0x44,          // D key
                22 => 0x53,         // S key
                26 => 0x57,         // W key
                // Add more keyboard mappings as needed
                _ => 0
            };
        }

        private void UpdateCursorElements(Point screenMousePos)
        {
            foreach (var cursorElement in _cursorElements)
            {
                cursorElement.UpdateForMousePosition(screenMousePos, this);
            }
        }

        private void SetupMouseHook()
        {
            _mouseHookProc = MouseHookProc;
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, GetModuleHandle(null!), 0);

            if (_mouseHook == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Failed to install mouse hook");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Global mouse hook installed successfully");
            }
        }

        private IntPtr MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam.ToInt32() == WM_MOUSEWHEEL_HOOK)
            {
                try
                {
                    // Read the MSLLHOOKSTRUCT structure
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

                    // Convert unsigned to signed
                    if (delta > 32767)
                        delta = (short)(delta - 65536);

                    System.Diagnostics.Debug.WriteLine($"Global mouse hook: wheel delta={delta} at position ({hookStruct.pt.X}, {hookStruct.pt.Y})");

                    // Use Dispatcher to update UI on main thread (non-blocking)
                    Dispatcher.BeginInvoke(new Action(() => {
                        if (_isMouseOverlay && IsVisible && WindowState != WindowState.Minimized)
                        {
                            System.Diagnostics.Debug.WriteLine($"Processing global wheel event: delta={delta}");
                            HandleMouseWheelMessage(delta);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    // Ignore errors in hook to prevent system instability
                    System.Diagnostics.Debug.WriteLine($"Mouse hook error: {ex.Message}");
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private void RemoveMouseHook()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
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

            // Remove mouse hook
            RemoveMouseHook();

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

        public void SetWheelState(int state)
        {
            if (_imageControl == null) return;

            CroppedBitmap croppedBitmap;
            int[] spriteRect;

            switch (state)
            {
                case 1: // Pressed
                    spriteRect = Element.Sprite.Pressed ?? Element.Sprite.Normal;
                    break;
                case 2: // Scroll up
                    spriteRect = Element.Sprite.Up ?? Element.Sprite.Normal;
                    break;
                case 3: // Scroll down
                    spriteRect = Element.Sprite.Down ?? Element.Sprite.Normal;
                    break;
                default: // Normal
                    spriteRect = Element.Sprite.Normal;
                    break;
            }

            croppedBitmap = new CroppedBitmap(_sourceImage, new Int32Rect(
                spriteRect[0], spriteRect[1], spriteRect[2], spriteRect[3]));

            _imageControl.Source = croppedBitmap;
            _imageControl.Width = spriteRect[2];
            _imageControl.Height = spriteRect[3];
        }
    }

    internal class CursorElement
    {
        public ElementInfo Element { get; }
        private BitmapImage _sourceImage;
        private DefaultsInfo _defaults;
        private Image? _imageControl;
        private Vector _previousMovement = new Vector(0, 0);
        private DateTime _lastMovementTime = DateTime.Now;
        private Point _lastMousePosition = new Point(0, 0);

        public CursorElement(ElementInfo element, BitmapImage sourceImage, DefaultsInfo defaults)
        {
            Element = element;
            _sourceImage = sourceImage;
            _defaults = defaults;
        }

        public Image CreateImageControl()
        {
            _imageControl = new Image();
            UpdateSprite(); // Start with normal state
            return _imageControl;
        }

        public void UpdateForMousePosition(Point screenMousePos, Window window)
        {
            if (_imageControl == null || Element.Cursor == null) return;

            // Convert screen position to window coordinates
            var windowPos = window.PointFromScreen(screenMousePos);

            if (Element.Cursor.Mode == "arrow")
            {
                // Arrow mode - rotate texture based on movement direction
                var currentTime = DateTime.Now;
                var deltaTime = (currentTime - _lastMovementTime).TotalMilliseconds;

                // Calculate movement delta
                var movementDelta = windowPos - _lastMousePosition;

                if (movementDelta.Length > 2.0) // Minimum movement threshold
                {
                    // Calculate rotation angle from movement vector
                    // Add 90 degrees to correct for visual orientation (0° = up, not right)
                    double angle = Math.Atan2(movementDelta.Y, movementDelta.X) * 180 / Math.PI + 90;

                    // Apply rotation transform to the image
                    var rotateTransform = new RotateTransform(angle);
                    rotateTransform.CenterX = _imageControl.Width / 2;
                    rotateTransform.CenterY = _imageControl.Height / 2;
                    _imageControl.RenderTransform = rotateTransform;

                    _lastMovementTime = currentTime;
                    _imageControl.Visibility = Visibility.Visible;
                }
                else if (deltaTime > 200) // Hide arrow if no movement for 200ms
                {
                    _imageControl.Visibility = Visibility.Hidden;
                }

                _lastMousePosition = windowPos;
            }
            else if (Element.Cursor.Mode == "dot")
            {
                // Dot mode - move dot relative to center position based on mouse delta
                var movementDelta = windowPos - _lastMousePosition;

                if (movementDelta.Length > 0.5) // Minimum movement threshold
                {
                    // Use configured sensitivity or default (inverse ratio - lower = more sensitive)
                    double sensitivity = Element.Cursor.Sensitivity ?? 0.3;
                    _previousMovement += movementDelta * sensitivity;

                    // Constrain movement within configured radius
                    var radius = Element.Cursor.Radius ?? 50;
                    if (_previousMovement.Length > radius)
                    {
                        _previousMovement = _previousMovement / _previousMovement.Length * radius;
                    }

                    // Update dot position relative to its center
                    var canvas = _imageControl.Parent as Canvas;
                    if (canvas != null)
                    {
                        var centerPos = new Point(Element.Position[0], Element.Position[1]);
                        var dotPos = centerPos + _previousMovement;

                        Canvas.SetLeft(_imageControl, dotPos.X - Element.Sprite.Normal[2] / 2);
                        Canvas.SetTop(_imageControl, dotPos.Y - Element.Sprite.Normal[3] / 2);
                        _imageControl.Visibility = Visibility.Visible;
                    }
                }

                _lastMousePosition = windowPos;
            }
        }

        private void UpdateSprite()
        {
            if (_imageControl == null) return;

            // Use normal sprite for now - could be enhanced to show different directions
            var croppedBitmap = new CroppedBitmap(_sourceImage, new Int32Rect(
                Element.Sprite.Normal[0], Element.Sprite.Normal[1],
                Element.Sprite.Normal[2], Element.Sprite.Normal[3]));

            _imageControl.Source = croppedBitmap;
            _imageControl.Width = Element.Sprite.Normal[2];
            _imageControl.Height = Element.Sprite.Normal[3];
        }

    }
}