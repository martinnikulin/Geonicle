using DevExpress.Xpf.Core;
using System.Windows;

namespace Geonicle.Wpf.Services
{
    public class UserDialogs : IUserDialogs
    {
        public MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon)
            => DXMessageBox.Show(text, caption, buttons, icon);
    }
}
