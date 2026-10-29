using DevExpress.Xpf.Accordion;
using Geonicle.Wpf.ViewModels;
using System;

namespace Geonicle.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void AccordionControl_SelectedItemChanged(object sender, AccordionSelectedItemChangedEventArgs e)
        {
            if (e.NewItem is AccordionItem { Tag: Type viewModelType } && DataContext is MainViewModel viewModel)
                viewModel.Navigate(viewModelType);
        }
    }
}
