using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using InputOverlayUI.Models;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using InputOverlayUI.Services;

namespace InputOverlayUI.ViewModels
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        private ObservableCollection<OverlayItem> _overlays;
        private OverlayItem? _selectedOverlay;
        private string _statusMessage = "Ready";
        private int _nextId = 1;
        private Dictionary<int, OverlayWindow> _openOverlays = new Dictionary<int, OverlayWindow>();
        private SettingsService _settingsService;

        public ObservableCollection<OverlayItem> Overlays
        {
            get => _overlays;
            set => SetProperty(ref _overlays, value);
        }

        public OverlayItem? SelectedOverlay
        {
            get => _selectedOverlay;
            set => SetProperty(ref _selectedOverlay, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Commands
        public ICommand DisplayAllCommand { get; }
        public ICommand CloseAllCommand { get; }
        public ICommand AddOverlayCommand { get; }
        public ICommand ShowOverlayCommand { get; }
        public ICommand CloseOverlayCommand { get; }
        public ICommand EditOverlayCommand { get; }
        public ICommand DeleteOverlayCommand { get; }

        public MainViewModel()
        {
            _overlays = new ObservableCollection<OverlayItem>();
            _settingsService = new SettingsService();

            // Initialize commands
            DisplayAllCommand = new RelayCommand(DisplayAll);
            CloseAllCommand = new RelayCommand(CloseAll);
            AddOverlayCommand = new RelayCommand(AddOverlay);
            ShowOverlayCommand = new RelayCommand<OverlayItem>(ShowOverlay);
            CloseOverlayCommand = new RelayCommand<OverlayItem>(CloseOverlay);
            EditOverlayCommand = new RelayCommand<OverlayItem>(EditOverlay);
            DeleteOverlayCommand = new RelayCommand<OverlayItem>(DeleteOverlay);

            // Load existing overlays from settings
            LoadSettings();
        }

        private void DisplayAll()
        {
            foreach (var overlay in Overlays)
            {
                if (!overlay.IsVisible)
                {
                    ShowOverlay(overlay);
                }
            }
            StatusMessage = "All overlays displayed";
        }

        private void CloseAll()
        {
            CloseAllOverlays();
        }

        private void AddOverlay()
        {
            var dialog = new AddOverlayDialog();
            if (dialog.ShowDialog() == true)
            {
                var newOverlay = dialog.Result;
                if (newOverlay != null)
                {
                    newOverlay.Id = _nextId++;
                    // Subscribe to property changes for auto-save
                    newOverlay.PropertyChanged += Overlay_PropertyChanged;
                    Overlays.Add(newOverlay);
                    SaveSettings();
                    StatusMessage = $"Added overlay: {newOverlay.Name}";
                }
            }
        }

        private void ShowOverlay(OverlayItem? overlay)
        {
            if (overlay == null) return;

            try
            {
                // Check if overlay is already open
                if (_openOverlays.ContainsKey(overlay.Id))
                {
                    StatusMessage = $"Overlay '{overlay.Name}' is already visible";
                    return;
                }

                // Validate required files
                if (string.IsNullOrEmpty(overlay.ConfigPath) || !File.Exists(overlay.ConfigPath))
                {
                    StatusMessage = $"Config file not found for overlay: {overlay.Name}";
                    MessageBox.Show($"Configuration file not found: {overlay.ConfigPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrEmpty(overlay.ImagePath) || !File.Exists(overlay.ImagePath))
                {
                    StatusMessage = $"Image file not found for overlay: {overlay.Name}";
                    MessageBox.Show($"Image file not found: {overlay.ImagePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create and show overlay window
                var overlayWindow = new OverlayWindow(overlay);
                overlayWindow.Closed += (s, e) => {
                    _openOverlays.Remove(overlay.Id);
                    overlay.IsVisible = false;
                };


                overlayWindow.Show();
                _openOverlays[overlay.Id] = overlayWindow;

                overlay.IsVisible = true;
                StatusMessage = $"Showing overlay: {overlay.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error showing overlay: {ex.Message}";
                MessageBox.Show($"Error showing overlay '{overlay.Name}': {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseOverlay(OverlayItem? overlay)
        {
            if (overlay == null) return;

            try
            {
                if (_openOverlays.ContainsKey(overlay.Id))
                {
                    _openOverlays[overlay.Id].Close();
                    _openOverlays.Remove(overlay.Id);
                }

                overlay.IsVisible = false;
                StatusMessage = $"Closed overlay: {overlay.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error closing overlay: {ex.Message}";
            }
        }

        private void EditOverlay(OverlayItem? overlay)
        {
            if (overlay == null) return;

            var dialog = new AddOverlayDialog(overlay);
            if (dialog.ShowDialog() == true)
            {
                // Overlay properties are updated via binding
                SaveSettings();
                StatusMessage = $"Updated overlay: {overlay.Name}";
            }
        }

        private void DeleteOverlay(OverlayItem? overlay)
        {
            if (overlay == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete overlay '{overlay.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                overlay.IsVisible = false; // Close overlay first
                // Unsubscribe from property changes
                overlay.PropertyChanged -= Overlay_PropertyChanged;
                Overlays.Remove(overlay);
                SaveSettings();
                StatusMessage = $"Deleted overlay: {overlay.Name}";

                // TODO: Send command to C++ core to remove overlay
            }
        }

        private void LoadSettings()
        {
            try
            {
                var settings = _settingsService.LoadSettings();

                // Load overlays
                _overlays.Clear();
                foreach (var overlay in settings.Overlays)
                {
                    // Subscribe to property changes for auto-save
                    overlay.PropertyChanged += Overlay_PropertyChanged;
                    _overlays.Add(overlay);
                }

                // Set next ID
                _nextId = settings.NextOverlayId;

                // Set folder preferences in AddOverlayDialog
                AddOverlayDialog.SetLastDirectories(settings.LastImageDirectory, settings.LastConfigDirectory);

                StatusMessage = $"Loaded {settings.Overlays.Count} overlays from settings";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    Overlays = _overlays.ToList(),
                    NextOverlayId = _nextId,
                    LastImageDirectory = AddOverlayDialog.LastImageDirectory,
                    LastConfigDirectory = AddOverlayDialog.LastConfigDirectory
                };

                _settingsService.SaveSettings(settings);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving settings: {ex.Message}";
            }
        }

        public void CloseAllOverlays()
        {
            try
            {
                // Close all open overlay windows
                var overlaysToClose = _openOverlays.ToList();
                foreach (var kvp in overlaysToClose)
                {
                    kvp.Value.Close();
                }
                _openOverlays.Clear();

                // Update all overlay visibility status
                foreach (var overlay in Overlays)
                {
                    overlay.IsVisible = false;
                }

                StatusMessage = "All overlays closed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error closing overlays: {ex.Message}";
            }
        }

        private void Overlay_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Auto-save when any overlay property changes
            SaveSettings();
        }

        public void Dispose()
        {
            SaveSettings();
            CloseAllOverlays();
        }
    }

}