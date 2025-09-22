using System.ComponentModel;
using System.Windows;
using InputOverlayUI.ViewModels;

namespace InputOverlayUI
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Ensure overlays are closed when the main window closes
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Close all overlay windows before the main application closes
            _viewModel?.Dispose();
        }
    }
}