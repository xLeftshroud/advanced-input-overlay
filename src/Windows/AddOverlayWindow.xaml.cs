using System.IO;
using System.Windows;
using AdvancedInputOverlay.Models;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace AdvancedInputOverlay.Windows;

public partial class AddOverlayWindow : Window
{
    private bool _isEdit;

    public string HeaderText => _isEdit ? "Edit Overlay" : "Add Overlay";

    public string OverlayName => NameBox.Text.Trim();
    public string ImagePath => ImageBox.Text.Trim();
    public string LayoutPath => LayoutBox.Text.Trim();

    public AddOverlayWindow()
    {
        InitializeComponent();
    }

    public void LoadFrom(OverlayConfig config)
    {
        _isEdit = true;
        Title = "Edit Overlay";
        NameBox.Text = config.Name;
        ImageBox.Text = config.ImagePath;
        LayoutBox.Text = config.LayoutPath;
    }

    private void BrowseImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select overlay image",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) == true)
        {
            ImageBox.Text = dlg.FileName;
        }
    }

    private void BrowseLayout_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select overlay config (json)",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog(this) == true)
        {
            LayoutBox.Text = dlg.FileName;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string? err = Validate();
        if (err is not null)
        {
            ErrorText.Text = err;
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        DialogResult = true;
        Close();
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(OverlayName))
            return "Name is required.";
        if (string.IsNullOrWhiteSpace(ImagePath) || !File.Exists(ImagePath))
            return "Overlay image file not found.";
        if (string.IsNullOrWhiteSpace(LayoutPath) || !File.Exists(LayoutPath))
            return "Overlay config (json) file not found.";
        return null;
    }
}
