using Geonicle.Wpf.ViewModels;
using System;

namespace Geonicle.Wpf.Views
{
    public partial class AssaysWindow
    {
        public AssaysWindow(AssaysViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseRequested += ViewModel_CloseRequested;
        }

        private void ViewModel_CloseRequested(object? sender, EventArgs e) => Close();
    }
}
