using DevExpress.Xpf.Bars;
using Geonicle.Wpf.ViewModels;
using System.Windows;

namespace Geonicle.Wpf.Views
{
    public partial class BoreholesView
    {
        public BoreholesView()
        {
            InitializeComponent();
        }

        // Меню грида живёт в отдельном popup, поэтому привязка к команде главной модели ненадёжна:
        // вызываем ту же RefreshCommand, что и у кнопки в ленте.
        private void RefreshMenuItem_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (Window.GetWindow(this)?.DataContext is MainViewModel main)
                main.RefreshCommand.Execute(null);
        }
    }
}
