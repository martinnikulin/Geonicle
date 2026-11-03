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

            // При запуске сразу открываем «Скважины»; выбор пункта запускает переход через AccordionControl_SelectedItemChanged
            // (к этому моменту DataContext уже задан).
            Loaded += (_, _) => menu.SelectedItem = boreholesItem;
        }

        private bool isRestoringSelection;

        private async void AccordionControl_SelectedItemChanged(object sender, AccordionSelectedItemChangedEventArgs e)
        {
            if (isRestoringSelection)
                return;

            if (e.NewItem is AccordionItem { Tag: Type viewModelType } && DataContext is MainViewModel viewModel)
            {
                if (!await viewModel.NavigateAsync(viewModelType))
                {
                    // Переход отменён: возвращаем выделение на прежний пункт меню.
                    isRestoringSelection = true;
                    ((AccordionControl)sender).SelectedItem = e.OldItem;
                    isRestoringSelection = false;
                }
            }
        }
    }
}
