using System.Windows;

namespace Geonicle.Wpf.Services
{
    /// <summary>Диалоги для моделей представления; в тестах заменяется заглушкой.</summary>
    public interface IUserDialogs
    {
        MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon);
    }
}
