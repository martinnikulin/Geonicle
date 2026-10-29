using DevExpress.Xpf.Core;
using System.Windows;

namespace Geonicle.Wpf.Services
{
    public class DialogService : IDialogService
    {
        public MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
            => DXMessageBox.Show(text, caption, buttons, icon);
    }
}
