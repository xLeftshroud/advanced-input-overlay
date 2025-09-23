using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using InputOverlayUI.Models;

namespace InputOverlayUI
{
    public partial class AddOverlayDialog : Window, INotifyPropertyChanged
    {
        private static string _lastImageDirectory = "";
        private static string _lastConfigDirectory = "";

        public static string LastImageDirectory => _lastImageDirectory;
        public static string LastConfigDirectory => _lastConfigDirectory;

        public static void SetLastDirectories(string imageDir, string configDir)
        {
            _lastImageDirectory = imageDir ?? "";
            _lastConfigDirectory = configDir ?? "";
        }
        private string _overlayName = "";
        private string _imagePath = "";
        private string _configPath = "";
        private bool _isEditMode = false;
        private OverlayItem? _editingItem;

        public OverlayItem? Result { get; private set; }

        public string OverlayName
        {
            get => _overlayName;
            set
            {
                _overlayName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public string ConfigPath
        {
            get => _configPath;
            set
            {
                _configPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(OverlayName) &&
                              !string.IsNullOrWhiteSpace(ImagePath) &&
                              !string.IsNullOrWhiteSpace(ConfigPath) &&
                              File.Exists(ImagePath) &&
                              File.Exists(ConfigPath);

        public AddOverlayDialog(OverlayItem? editItem = null)
        {
            InitializeComponent();
            DataContext = this;

            _isEditMode = editItem != null;
            _editingItem = editItem;

            if (_isEditMode && editItem != null)
            {
                Title = "Edit Overlay";
                OkButton.Content = "Update Overlay";

                // Load existing values
                OverlayName = editItem.Name;
                ImagePath = editItem.ImagePath;
                ConfigPath = editItem.ConfigPath;
            }
            else
            {
                Title = "Add Input Overlay";
                OkButton.Content = "Add Overlay";
            }

            // Focus on name field
            Loaded += (s, e) => NameTextBox.Focus();
        }

        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Overlay Image",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff|" +
                        "PNG Files (*.png)|*.png|" +
                        "JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                        "Bitmap Files (*.bmp)|*.bmp|" +
                        "GIF Files (*.gif)|*.gif|" +
                        "TIFF Files (*.tiff)|*.tiff|" +
                        "All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Set initial directory based on priority:
            // 1. Current image path directory
            // 2. Last browsed image directory
            // 3. Default directory
            if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(ImagePath);
                openFileDialog.FileName = Path.GetFileName(ImagePath);
            }
            else if (!string.IsNullOrEmpty(_lastImageDirectory) && Directory.Exists(_lastImageDirectory))
            {
                openFileDialog.InitialDirectory = _lastImageDirectory;
            }

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
                _lastImageDirectory = Path.GetDirectoryName(openFileDialog.FileName) ?? "";
            }
        }

        private void BrowseConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Configuration File",
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            // Set initial directory based on priority:
            // 1. Current config path directory
            // 2. Last browsed config directory
            // 3. Presets directory
            if (!string.IsNullOrEmpty(ConfigPath) && File.Exists(ConfigPath))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(ConfigPath);
                openFileDialog.FileName = Path.GetFileName(ConfigPath);
            }
            else if (!string.IsNullOrEmpty(_lastConfigDirectory) && Directory.Exists(_lastConfigDirectory))
            {
                openFileDialog.InitialDirectory = _lastConfigDirectory;
            }
            else
            {
                // Try to start in the Presets directory
                string presetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Presets");
                if (!Directory.Exists(presetsPath))
                {
                    // Fallback to solution Presets directory (relative to current directory)
                    var currentDir = Environment.CurrentDirectory;
                    var solutionRoot = Directory.GetParent(currentDir);

                    // Try multiple fallback approaches
                    string[] possiblePaths = {
                        Path.Combine(currentDir, "Presets"),
                        Path.Combine(solutionRoot?.FullName ?? currentDir, "Presets"),
                        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? currentDir, "..", "..", "..", "Presets")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (Directory.Exists(path))
                        {
                            presetsPath = path;
                            break;
                        }
                    }
                }

                if (Directory.Exists(presetsPath))
                {
                    openFileDialog.InitialDirectory = presetsPath;
                }
            }

            if (openFileDialog.ShowDialog() == true)
            {
                ConfigPath = openFileDialog.FileName;
                _lastConfigDirectory = Path.GetDirectoryName(openFileDialog.FileName) ?? "";
                ValidateConfigFile(openFileDialog.FileName);
            }
        }

        private void ValidateConfigFile(string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);

                // Basic JSON validation
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonContent);

                if (config != null)
                {
                    ConfigInfoTextBlock.Text = $"✓ Valid JSON configuration file loaded: {Path.GetFileName(filePath)}";
                    ConfigInfoTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    ConfigInfoTextBlock.Text = "⚠ Warning: Configuration file appears to be empty.";
                    ConfigInfoTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
                }
            }
            catch (Exception ex)
            {
                ConfigInfoTextBlock.Text = $"✗ Error: Invalid JSON format - {ex.Message}";
                ConfigInfoTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid)
            {
                string message = "Please ensure all fields are filled and files exist:\n";
                if (string.IsNullOrWhiteSpace(OverlayName))
                    message += "• Overlay name is required\n";
                if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath))
                    message += "• Valid image file is required\n";
                if (string.IsNullOrWhiteSpace(ConfigPath) || !File.Exists(ConfigPath))
                    message += "• Valid configuration file is required\n";

                MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isEditMode && _editingItem != null)
                {
                    // Update existing item
                    _editingItem.Name = OverlayName;
                    _editingItem.ImagePath = ImagePath;
                    _editingItem.ConfigPath = ConfigPath;
                    Result = _editingItem;
                }
                else
                {
                    // Create new item
                    Result = new OverlayItem
                    {
                        Name = OverlayName,
                        ImagePath = ImagePath,
                        ConfigPath = ConfigPath,
                        IsVisible = false,
                        TopMost = true
                    };
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating overlay: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}