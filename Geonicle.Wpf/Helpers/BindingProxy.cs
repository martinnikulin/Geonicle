using System.Windows;

namespace Geonicle.Wpf.Helpers
{
    /// <summary>
    /// Передаёт DataContext туда, где его нет в визуальном дереве (например, в EditSettings колонки грида).
    /// Использование: ресурс <helpers:BindingProxy x:Key="Proxy" Data="{Binding}" />,
    /// привязка {Binding Data.Свойство, Source={StaticResource Proxy}}.
    /// </summary>
    public class BindingProxy : Freezable
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));

        public object? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new BindingProxy();
    }
}
